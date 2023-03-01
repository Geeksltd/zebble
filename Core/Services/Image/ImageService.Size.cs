namespace Zebble.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using Olive;

    partial class ImageService
    {
        static readonly object LoadingMetaSyncLock = new();
        static bool IsMetaLoaded;
        public static ConcurrentDictionary<string, Size> SizeCache = new();

        /// <summary>
        /// Gets the pixel size of the specified image file divided by the screen density.
        /// </summary>
        public static Size GetViewSize(string imageFile)
        {
            return GetPixelSize(imageFile).Scale(1f / Device.Screen.HardwareDensity).RoundDown();
        }

        public static Size GetPixelSize(FileInfo file) => GetPixelSize(file?.FullName);

        public static Size GetPixelSize(string path)
        {
            if (path.IsEmpty()) return new Size();

            LoadMetaSizesIntoCache();

            if (path.IsUrl()) path = GetFile(path).FullName;
            else path = Device.IO.NormalizePath(path);

            if (SizeCache.TryGetValue(path.ToLower(), out var result)) return result;

            if (path.IsUrl())
            {
                var tip = "set a specific width and height for the ImageView, with stretch of AspectFill.";
                Log.For(typeof(ImageService)).Error("As this is a remote image, the size cannot be calculated from the image file.");
                Log.For(typeof(ImageService)).Error("For remote images " + tip + " Otherwise it will be assumed as 100x100.");
                return Size.Square(100);
            }
            else
            {
                return SizeCache[path.ToLower()] = FindImageSize(Device.IO.File(path));
            }
        }

        static void LoadMetaSizesIntoCache()
        {
            lock (LoadingMetaSyncLock)
            {
                if (IsMetaLoaded) return;

                var file = Device.IO.File("Zebble-Meta.xml");

                if (!file.Exists()) return;

                foreach (var item in file.ReadAllText().To<XDocument>().Root.Elements().Where(e => e.Name == "image"))
                {
                    var imagePath = Device.IO.NormalizePath(item.GetValue<string>("@path"));
                    var size = new Size(item.GetValue<int>("@width"), item.GetValue<int>("@height"));
                    SizeCache.TryAdd(imagePath, size);

                    // Also add the full version:
                    SizeCache.TryAdd(Device.IO.File(imagePath).FullName.ToLower(), size);
                }

                IsMetaLoaded = true;
            }
        }
    }
}