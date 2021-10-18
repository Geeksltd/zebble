namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Olive;

    public class AssemblyInfo
    {
        public static Assembly[] GetAssemblies()
        {
#if UWP
            var assemblies = new List<Assembly>();
            var files = Windows.ApplicationModel.Package.Current.InstalledLocation.GetFilesAsync().AsTask().AwaitResultWithoutContext();
            if (files is null) return assemblies.ToArray();

            foreach (var file in files.Where(file => file.FileType == ".dll" || file.FileType == ".exe"))
            {
                try
                {
                    assemblies.Add(Assembly.Load(new AssemblyName(file.DisplayName)));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }

            return assemblies.ToArray();
#else
            return AppDomain.CurrentDomain.GetAssemblies();
#endif
        }
    }
}
