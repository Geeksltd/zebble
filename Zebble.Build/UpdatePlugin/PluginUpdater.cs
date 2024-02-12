namespace Zebble.Build
{
    using System;
    using System.IO;
    using System.Xml.Linq;
    using System.Linq;
    using Zebble.Tooling;
    using Olive;

    class PluginUpdater : Builder
    {
        readonly bool _increaseVersion;
        readonly string _configuration;
        readonly bool _publish;
        readonly string _source;
        readonly string _apiKey;
        readonly bool _commit;
        readonly DirectoryInfo _root;
        readonly FileInfo _csProjFile;

        public PluginUpdater(bool increaseVersion, string configuration, bool publish, string source, string apiKey, bool commit)
        {
            _increaseVersion = increaseVersion;
            _configuration = configuration;
            _publish = publish;
            _source = source;
            _apiKey = apiKey;
            _commit = commit;

            _root = DirectoryContext.RootFolder;

            _csProjFile = _root.GetFiles("*.csproj").ElementAtOrDefault(0);
            if (_csProjFile == null)
                throw new IOException(
                    "Couldn't find .csproj file. Ensure you're calling this tool in the root folder of your plugin."
                );

            Log($"Trying to build your dotnet tool (nuget package) under {_root.FullName}");

            if (_publish)
            {
                Log($"It will be published automatically to {source}");

                if (_increaseVersion && _commit) Log("Changes will be committed automatically");
            }
        }

        protected override void AddTasks()
        {
            if (_increaseVersion)
                Add(() => IncreasePackageVersion());

            Add(() => Clean());
            Add(() => RemoveExistingNugetPackages());
            Add(() => RestorePackages());
            Add(() => Build());
            Add(() => GenerateNugetPackage());

            if (_publish)
            {
                Add(() => PushCreatedNugetPackage());

                if (_increaseVersion && _commit)
                    Add(() => CommitChanges());
            }
        }

        void IncreasePackageVersion()
        {
            var pluginDocument = XDocument.Parse(_csProjFile.ReadAllText());

            var projectNode = pluginDocument.GetElement("Project");
            if (projectNode == null)
                throw new FormatException("Couldn't find the Project node. Is this a .NET Core project?");

            var versionAttr = projectNode.GetElement("PropertyGroup/Version") ??
                              projectNode.GetElement("PropertyGroup/PackageVersion");
            if (versionAttr == null)
                throw new FormatException("Couldn't determine the plugin version");

            var currentVersion = Version.Parse(versionAttr.Value);
            var updatedVersion = new Version(currentVersion.Major, currentVersion.Minor, currentVersion.Build + 1, 0);

            Log($"Current version is {currentVersion} and new version will be {updatedVersion}.");

            versionAttr.SetValue(updatedVersion.ToString());

            _csProjFile.WriteAllText(pluginDocument.ToString());
        }

        void Clean()
        {
            Execute(Commands.MsBuild, $"/t:Clean /p:Configuration={_configuration}");
        }

        void RemoveExistingNugetPackages()
        {
            Execute(Commands.Powershell, @$" Try {{ Get-ChildItem .\bin\{_configuration}\*.nupkg -Recurse | ForEach {{ Remove-Item -Path $_.FullName }} }} Catch {{ }} Exit 0");
        }

        void RestorePackages()
        {
            Execute(Commands.MsBuild, $"/t:Restore /p:Configuration={_configuration}");
        }

        void Build()
        {
            Execute(Commands.MsBuild, $"/p:Configuration={_configuration}");
        }

        void GenerateNugetPackage()
        {
            Execute(Commands.MsBuild, $"/t:pack /p:Configuration={_configuration}");
        }

        void PushCreatedNugetPackage()
        {
            Execute(Commands.DotNet, $@"nuget push .\bin\{_configuration}\*.nupkg --source ""{_source}"" --api-key ""{_apiKey}""");
        }

        void CommitChanges()
        {
            Execute(Commands.Git, $@"add ""{_csProjFile.Name}""");
            Execute(Commands.Git, $@"commit -m ""Update {_csProjFile.Name}""");
            Execute(Commands.Git, @"push");
        }

        void Execute(FileInfo executor, string args)
        {
            executor.Execute(args, configuration: x => x.StartInfo.WorkingDirectory = _root.FullName);
        }
    }
}