namespace Zebble.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Olive;

    public static partial class ImageService
    {
        public const int DEFAULT_JPEG_QUALITY = 90;

        static readonly Dictionary<string, ImageSource> Providers = new();
        const string NOT_FOUND = "Images/Icons/not-found.png";
        static string FailedPlaceholderImagePath;
        static string[] MemoryCacheFolders = new string[0];
        static readonly List<string> MemoryCacheFiles = new();
        static readonly object ProviderSyncLock = new();

        static ImageService()
        {
            MemoryCacheFolder("images/icons");
            SetFailedPlaceholderImagePath(null);
        }

        internal static bool IsWorthResizing(Size source, Size window)
        {
            return source.IsLargerThan(window.Scale(1.2f));
        }

        static string Clean(string path) => path.ToLowerOrEmpty().Replace("\\", "/").KeepReplacing("//", "/").TrimStart("/");

        public static void SetFailedPlaceholderImagePath(string path) => FailedPlaceholderImagePath = path.Or(NOT_FOUND);

        public static void MemoryCacheFolder(string folder)
        {
            folder = Clean(folder);
            if (folder.IsEmpty()) return;

            folder = folder.EnsureEndsWith("/");
            var absolute = Clean(Device.IO.AbsolutePath(folder)).EnsureEndsWith("/");

            MemoryCacheFolders = MemoryCacheFolders.Concat(folder).Concat(absolute).Distinct().ToArray();
        }

        public static void MemoryCacheBackground(View view) => MemoryCacheFile(view?.BackgroundImagePath);

        public static void MemoryCacheFile(string path)
        {
            path = Clean(path);
            if (path.IsEmpty()) return;

            if (MemoryCacheFiles.Lacks(path))
                MemoryCacheFiles.Add(path);

            path = Device.IO.AbsolutePath(path);
            if (MemoryCacheFiles.Lacks(path)) MemoryCacheFiles.Add(path);
        }

        public static bool ShouldMemoryCache(string localPath)
        {
            localPath = Clean(localPath);
            if (localPath.IsEmpty()) return false;

            if (localPath.StartsWithAny(MemoryCacheFolders)) return true;

            if (MemoryCacheFiles.Contains(localPath)) return true;

            return false;
        }

        public static bool ShouldLazyLoad(this View image)
        {
            return (image as ImageView)?.IsLazyLoaded ?? !ShouldMemoryCache(image.BackgroundImagePath);
        }

        public static Task<object> GetNativeImage(string path, Size size, Stretch stretch)
        {
            return GetSource(path, size, stretch).Result();
        }

        public static ImageSource GetSource(string path, Size size, Stretch stretch)
        {
            path = path.Or(FailedPlaceholderImagePath);
            var key = path.ToLowerOrEmpty() + stretch + size;

            lock (ProviderSyncLock)
            {
                if (Providers.TryGetValue(key, out var result)) return result;

                result = new ImageSource(key, path, size, stretch);
                if (ShouldMemoryCache(path)) Providers[key] = result;
                return result;
            }
        }

        /// <summary>
        /// Gets the correct pixel size to encode or decode an image based on a specified display frame and stretch.
        /// </summary>
        /// <param name="imageSize">Image size in pixels.</param>
        /// <param name="displayFrame">Target display size in Zebble UI (logical point).</param>
        internal static Size GetPixelSize(Size imageSize, Size displayFrame, Stretch stretch)
        {
            if (imageSize.Area() == 0 || displayFrame.Area() == 0) return imageSize;

            displayFrame = displayFrame.Scale(Device.Screen.HardwareDensity);

            switch (stretch)
            {
                case Stretch.Fill:
                    var usefulWidth = Math.Min(imageSize.Width, displayFrame.Width);
                    var usefulHeight = Math.Min(imageSize.Height, displayFrame.Height);
                    return new Size(usefulWidth, usefulHeight);

                case Stretch.Fit:
#if ANDROID
                    if (displayFrame.Width > displayFrame.Height)
                        return new Size(displayFrame.Width / 2, displayFrame.Height);
                    else
                        return new Size(displayFrame.Width, displayFrame.Height / 2);
#else
                    return imageSize.LimitTo(displayFrame);

#endif
                case Stretch.AspectFill:

                    var scaleWidth = (displayFrame.Width / imageSize.Width).LimitMax(1);
                    var scaleHeight = (displayFrame.Height / imageSize.Height).LimitMax(1);
                    var scale = Math.Max(scaleWidth, scaleHeight);
                    return imageSize.Scale(scale);
                case Stretch.Default:
                    return imageSize;
                default: throw new NotSupportedException();
            }
        }

        public static ImageSource GetSource(View viewer)
        {
            var path = viewer.BackgroundImagePath;
            if (path.IsEmpty()) return null;

            var imageSize = new Size
            {
                Width = (viewer.ActualWidth - viewer.HorizontalPaddingAndBorder()).LimitMin(0),
                Height = (viewer.ActualHeight - viewer.VerticalPaddingAndBorder()).LimitMin(0)
            };

            return GetSource(path, imageSize, viewer.BackgroundImageStretch);
        }

        internal static async void Draw(View view, Action<object> drawer)
        {
            if (view is null) return; // Concurrency?

            if (view.BackgroundImageData?.Length > 0)
            {
                async void apply()
                {
                    try
                    {
                        var result = await DecodeImage(view.BackgroundImageData);
                        await Thread.UI.Run(() => drawer(result));
                    }
                    catch (Exception ex)
                    {
                        Log.For(typeof(ImageService)).Error(ex, "IMAGE ERROR!!!!!!!");
                    }
                }

                if (Device.OS.Platform == DevicePlatform.Windows) apply(); // can only decode on ui thread.
                else Thread.Pool.RunAction(apply);

                return;
            }

            var provider = GetSource(view);
            if (provider == null) return;
            provider.RegisterViewer();

            async Task doLoad()
            {
                try
                {
                    var img = await provider.GetImageResult();
                    Thread.UI.RunAction(() =>
                    {
                        try { drawer(img); }
                        catch (Exception ex) { Log.For(typeof(ImageService)).Error(ex); }
                    });
                }
                catch (Exception ex) { Log.For(typeof(ImageService)).Error(ex); }
            }

            if (provider.IsFast()) await doLoad();
            else Thread.Pool.Run(doLoad).GetAwaiter();
        }
    }
}