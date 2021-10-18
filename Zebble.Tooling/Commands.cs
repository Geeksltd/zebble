namespace Zebble.Tooling
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Olive;

    static class Commands
    {
        static readonly IDictionary<string, FileInfo> _exes = new ConcurrentDictionary<string, FileInfo>();

        public static FileInfo MsBuild => VsWhereExe(@"-NoLogo -Latest -Requires Microsoft.Component.MSBuild -Find MSBuild\**\Bin\MSBuild.exe");

        public static FileInfo DotNet => WhereExe("dotnet");

        public static FileInfo Git => WhereExe("git");

        public static FileInfo Powershell => WhereExe("powershell");

        public static FileInfo VsWhereExe(string fileInPathEnv) => FindExe(VsWhere, fileInPathEnv);

        public static FileInfo WhereExe(string fileInPathEnv) => FindExe(Where, fileInPathEnv);

        static FileInfo VsWhere
        {
            get
            {
                if (Runtime.IsWindows())
                    return VsInstaller("VSWHERE.exe").ExistsOrThrow();

                throw new NotSupportedException(Runtime.OS.ToString());
            }
        }

        static FileInfo Where
        {
            get
            {
                if (Runtime.IsWindows())
                    return System32("WHERE.exe").ExistsOrThrow();

                throw new NotSupportedException(Runtime.OS.ToString());
            }
        }

        static FileInfo FindExe(FileInfo util, string args)
        {
            if (!_exes.ContainsKey(args))
            {
                var output = util.Execute(args, configuration: x => x.StartInfo.WorkingDirectory = string.Empty);
                _exes.Add(args, output.Trim().ToLines().Select(x => x.AsFile()).First(x => x.Extension.HasValue()));
            }

            return _exes[args];
        }

        static FileInfo System32(string relative)
        {
            return Environment.SpecialFolder.Windows
                .GetFile($"System32{Path.DirectorySeparatorChar}{relative}")
                .ExistsOrThrow();
        }

        static FileInfo VsInstaller(string relative)
        {
            return Environment.SpecialFolder.ProgramFilesX86
                .GetFile($"Microsoft Visual Studio{Path.DirectorySeparatorChar}Installer{Path.DirectorySeparatorChar}{relative}")
                .ExistsOrThrow();
        }
    }
}