namespace Zebble.Tooling
{
    using System;
    using System.IO;
    using Olive;

    class DirectoryContext
    {
        internal static DirectoryInfo RootFolder;

        static DirectoryContext()
        {
            RootFolder = Environment.CurrentDirectory.AsDirectory();
            
            if (RunFolder == null) throw new IOException("The Run folder was not found.");
        }

        public static DirectoryInfo AppDomainFolder => RootFolder.GetSubDirectory("App.Domain");
        public static DirectoryInfo AppUIFolder => RootFolder.GetSubDirectory("App.UI");
        internal static DirectoryInfo AppUIResourcesFolder => AppUIFolder.GetSubDirectory("AppResources");
        public static DirectoryInfo AppUIStylesFolder => AppUIFolder.GetSubDirectory("Styles");
        public static DirectoryInfo RunFolder => RootFolder.GetSubDirectory("Run");
        public static DirectoryInfo AndroidFolder => RunFolder.GetSubDirectory("Android");
        public static DirectoryInfo WinUIFolder => RunFolder.GetSubDirectory("WinUI");
        public static DirectoryInfo WinUIBinFolder => WinUIFolder.GetSubDirectory("bin");
        public static DirectoryInfo WinUIObjFolder => WinUIFolder.GetSubDirectory("obj");
        public static DirectoryInfo ViewModelFolder => RootFolder.GetSubDirectory("ViewModel");

        public static DirectoryInfo[] Platforms => RunFolder.GetDirectories();
    }
}