namespace Zebble
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Zebble.Device;
    using Olive;

    partial class StartUp
    {

        public abstract string GetResourcesVersion();

        /// <summary>
        /// Determines if the application is running in test execution mode.
        /// </summary>
        protected internal virtual bool IsTestMode() => false;

        public abstract Task Run();

        /// <summary>
        /// Installs the database and files for the first time.
        /// </summary>
        protected async Task InstallIfNeeded()
        {
            ConfigureIO();
            var version = IO.File("inst_" + GetResourcesVersion() + ".txt");

            if (!version.Exists() || Reinstall)
            {
                await Install();
                await version.WriteAllTextAsync("Done");
            }
        }

        protected virtual async Task Install()
        {
            await Thread.UI.Run(CopyResources);
            GenerateInstallationToken();
        }

        protected virtual void GenerateInstallationToken()
        {
            Device.IO.File("Installation.Token").WriteAllText(Guid.NewGuid().ToString());
        }

        protected virtual bool DeleteFilesOnUpdate() => true;

        void ConfigureIO()
        {
            if (IsTestMode())
                IO.FilesVersion = "test-" + LocalTime.UtcNow.Ticks;

            IO.Cache = new DirectoryInfo(GetIOTempRoot()).EnsureExists();

            var app = (ApplicationName + GetResourcesVersion()).Where(x => x.IsLetterOrDigit()).ToString("");
            var path = Path.Combine(GetIORoot(), app);
            if (IO.FilesVersion.HasValue()) path = Path.Combine(path, IO.FilesVersion);

            IO.Root = new DirectoryInfo(path).EnsureExists();
        }

        protected virtual async Task CopyResources()
        {
            if (DeleteFilesOnUpdate())
            {
                foreach (var d in IO.Root.GetDirectories())
                    try { d.Delete(recursive: true); }
                    catch (Exception ex) { Log.For(this).Warning("Failed to delete " + d.FullName + ": " + ex.Message); }

                foreach (var f in IO.Root.GetFiles())
                    try { f.Delete(); }
                    catch (Exception ex) { Log.For(this).Warning("Failed to delete " + f.FullName + ": " + ex.Message); }
            }

            // Move files from embedded resource into the local folder:
            foreach (var item in UIRuntime.GetEmbeddedResources())
            {
                var resource = item.Key;

                if (resource.Lacks(".Resources.")) continue;

                var parts = resource.RemoveBeforeAndIncluding(".Resources.").Split('.');
                var extension = parts.Last();
                parts = parts.ExceptLast().ToArray();
                var fileName = parts.Last();

                var folder = parts.ExceptLast().ToString("/");

                var relativePath = Path.Combine(folder, fileName + "." + extension).ToLower();
                var path = Device.IO.File(relativePath);

                path.Directory.EnsureExists();

                await path.WriteAllBytesAsync(item.Value());
            }
        }
    }
}