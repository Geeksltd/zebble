namespace Zebble.Device
{
    using System;
    using System.IO;
    using Olive;

    public static class IO
    {
        /// <summary>
        /// When set, it will redefine Root as a subfolder with this name.
        /// </summary>
        public static string FilesVersion = null;

        /// <summary>
        /// Return the documents directory for the current user. 
        /// </summary>
        public static string DocumentsFolder
        {
            get
            {
#if WINUI
                return Windows.Storage.KnownFolders.DocumentsLibrary.Path;
#else
                return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
#endif
            }
        }

        /// <summary>
        /// Return the personal or home directory for the current user. 
        /// On non-Windows operating systems, this is the user's home directory.
        /// </summary>
        public static string PersonalFolder
        {
            get
            {
#if WINUI
                return Windows.Storage.ApplicationData.Current.LocalFolder.Path;
#else
                return Environment.GetFolderPath(Environment.SpecialFolder.Personal);
#endif
            }
        }

        public static string ExternalStorageFolder
        {
            get
            {
#if ANDROID
                if (Android.OS.Environment.ExternalStorageState == Android.OS.Environment.MediaMounted)
                    return Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
                else return "";
#else
                return PersonalFolder;
#endif
            }
        }

        public static FileInfo File(string relative)
        {
            try { return new FileInfo(AbsolutePath(relative)); }
            catch (Exception ex) { throw new FormatException($"Failed to convert '{relative}' to FileInfo.", ex); }
        }

        public static DirectoryInfo Directory(string relative) => new(AbsolutePath(relative));

        /// <summary>
        ///  Will convert a specified path to a cross-platform compatible relative path.
        ///  It will be lowercase, uses underscore instead of hyphen, uses / instead of \ and doesn't start with /.
        ///  However if it's an absolute path, it will be returned intact.
        /// </summary>
        internal static string NormalizePath(string relative)
        {
            if (IsAbsolute(relative)) return relative;
            return relative.ToLowerOrEmpty().Replace("-", "_").Replace("\\", "/").KeepReplacing("//", "/").TrimStart("/");
        }

        public static DirectoryInfo Root { get; internal set; }

        /// <summary>
        /// It takes a relative path as input, and returns the physical path on the device. 
        /// It assumes that the file / folder is inside the AppResources directory. 
        /// It's case insensitive as all files are turned into lowercase.
        /// Example: For Images/Abc.png it returns (...)/AppResources/Images/Abc.png.
        /// </summary>
        public static string AbsolutePath(string relative)
        {
            if (Root == null) throw new InvalidStateException("Device.IO.Root is null!");
            var root = Root.FullName;

            if (relative.IsEmpty()) return root;
            else
            {
                relative = NormalizePath(relative);
#if WINUI
                relative = relative.Replace("/", "\\");
#endif
            }

            if (IsAbsolute(relative)) return relative;

            return Path.Combine(root, relative);
        }

        public static bool IsAbsolute(string path)
        {
            if (path.IsEmpty()) return false;
            if (path.StartsWith(Root.FullName, caseSensitive: false)) return true;
            if (path.StartsWith(Cache.FullName, caseSensitive: false)) return true;
            return false;
        }

        /// <summary>
        /// Creates a temporary directory in either the local app storage folder under .\temp\ or in the OS global cache directory.
        /// If global cache is used (default), it can be cleaned by the OS when needed.
        /// </summary>
        public static DirectoryInfo CreateTempDirectory(bool globalCache = true)
        {
            return GetTempRoot(globalCache).GetOrCreateSubDirectory(Guid.NewGuid().ToString());
        }

        /// <summary>
        /// Returns a temp file name, as a new guid, in the temp directory.
        /// </summary>
        /// <param name="extension">The file extension needed, e.g. .png</param>
        /// <param name="globalCache">If global cache is used (default), it can be cleaned by the OS when needed.</param>
        public static FileInfo CreateTempFile(string extension, bool globalCache = true)
        {
            return GetTempRoot(globalCache).GetFile(Guid.NewGuid() + extension.EnsureStartsWith("."));
        }

        /// <summary>
        /// Returns the root temp folder in which to create temp files and folders.
        /// </summary>
        /// <param name="globalCache">If global cache is used (default), it can be cleaned by the OS when needed.</param>
        public static DirectoryInfo GetTempRoot(bool globalCache = true)
        {
            if (globalCache)
                return Cache.GetOrCreateSubDirectory("Temp");
            return Directory("temp").EnsureExists();
        }

        public static DirectoryInfo Cache { get; internal set; }
    }
}
