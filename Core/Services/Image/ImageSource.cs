namespace Zebble.Services
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Olive;

    partial class ImageService
    {
        const int SmallImageFileSize = 100000;

        /// <summary>
        /// It will disposed images which are not currently being viewed.
        /// This should be invoked in case of meomry pressure.
        /// </summary>
        public static void DisposeCache()
        {
            Thread.Pool.RunAction(() =>
            {
                ImageSource[] toRemove;
                lock (ProviderSyncLock)
                {
                    toRemove = Providers.Values.Where(x => x.Viewers == 0).ToArray();
                    toRemove.Do(x => Providers.Remove(x.CacheKey));
                }

                Parallel.ForEach(toRemove, x => x.Dispose());
            });
        }

        public static void Dispose(ImageSource provider)
        {
            if (provider is null) return;

            lock (ProviderSyncLock)
                Providers.Remove(provider.CacheKey);

            try { provider.Dispose(); }
            catch (Exception ex)
            {
                Log.For<ImageSource>().Error(ex, "Disposing the Image failed. Source: " + provider.Source);
            }
        }

        public partial class ImageSource
        {
            internal string CacheKey, Url;
            internal int Viewers;
            public string Source;
            public readonly FileInfo File;
            public Size Size;
            public Stretch Stretch;
            public bool IsDownloading { get; private set; }
            public bool IsMemoryCached { get; private set; }
            internal object Image;

            /// <summary>
            /// Fired when Image is loaded, decoded and ready for use.
            /// </summary>
            public readonly AsyncEvent<object> Ready = new AsyncEvent<object>();
            TaskCompletionSource<bool> LoadingFromFileTask;

            public ImageSource(string cacheKey, string source, Size size, Stretch stretch)
            {
                CacheKey = cacheKey;
                Stretch = stretch;
                Size = size;
                IsMemoryCached = ShouldMemoryCache(source);

                if (source.IsUrl()) Url = source;

                File = GetFile(source);
                Source = source;
            }

            public override string ToString()
            {
                return $"{Source} ({Size}) {Stretch}" + " Downloading...".OnlyWhen(IsDownloading);
            }

            public async Task<object> Result()
            {
                if (Image != null) return Image;

                if (Url.HasValue())
                {
                    var shouldDownload = !await IsDownloaded(Url);
                    if (shouldDownload)
                    {
                        // Schedule a download. When it's done, it will fire the ImageDownloaded event.
                        IsDownloading = true;
                        await Download();
                        // For now, just return null
                        return null;
                    }
                }

                await LoadFromFile();

                if (Image is null) throw new Exception("Image is null for " + Url);
                return Image;
            }

            public async Task<object> GetImageResult()
            {
                await Result().OrCompleted();
                return Image;
            }

            async Task LoadFromFile()
            {
                var source = LoadingFromFileTask;

                if (source != null)
                {
                    try { await source.Task; }
                    catch { /* No logging is needed. Already handled */ }

                    return;
                }

                source = LoadingFromFileTask = new TaskCompletionSource<bool>();

                try
                {
                    // Create a version for the specifically requested size
                    await LoadPhysicalImageFromDisk();

                    if (Image is null)
                    {
                        Log.For(this).Error("Image is null?! What the hell? " + ShouldMemoryCache(File.FullName));
                    }
                }
                catch (Exception ex)
                {
                    ex = new Exception($"Failed to load an image from disk: {GetPhysicalFileToLoad().FullName}{Environment.NewLine}{ex.Message}", ex);
                    Log.For(this).Error(ex);
                    Image = await GetSource(FailedPlaceholderImagePath, Size, Stretch).Result();
                }
                finally
                {
                    source.TrySetResult(result: true);
                    LoadingFromFileTask = null;
                }

                if (Image != null) Ready?.Raise(Image);
            }

            public async Task<FileInfo> GetExactSizedFile()
            {
                var file = GetPhysicalFileToLoad();
                var sizedFile = GetSizedFilePath();
                if (sizedFile is null) return file;

                if (!await sizedFile.ExistsAsync() && await file.ExistsAsync())
                    await Thread.Pool.Run(() => SaveSpecificSizedCache(file, sizedFile, Stretch));

                return sizedFile;
            }

            FileInfo GetSizedFilePath()
            {
                if (Size.Width == 0 && Size.Height == 0) return null;

                var file = GetPhysicalFileToLoad();

                if (file.Length < SmallImageFileSize) return null;

                var root = Device.IO.Cache;

                return root.GetFile(file.FullName.ToIOSafeHash() + "___" +
                    Size.Width + "x" + Size.Height + file.Extension);
            }

            async Task LoadPhysicalImageFromDisk()
            {
                var file = GetPhysicalFileToLoad();

                if (IsMemoryCached)
                {
                    await DecodeImageFile(file);
                    return;
                }

                var sizedFile = GetSizedFilePath();

                if (sizedFile != null && await sizedFile.ExistsAsync() && sizedFile.Length > 0)
                    await DecodeImageFile(sizedFile);
                else
                {
                    await DecodeImageFile(file);

                    // Create a snapshot
                    if (sizedFile != null)
                        GetExactSizedFile().RunInParallel();
                }
            }

            async Task SaveSpecificSizedCache(FileInfo source, FileInfo cache, Stretch stretch)
            {
                while (!IdleUITasks.SeemsIdle()) await Task.Delay(500.Milliseconds());
                await Task.Delay(500.Milliseconds());

                var sourceSize = GetPixelSize(source);
                if (sourceSize.Width == 0 || sourceSize.Height == 0) return;

                var wantedSize = GetPixelSize(sourceSize, Size, stretch);

                var isNeeded = IsWorthResizing(sourceSize, wantedSize);
#if ANDROID
                if (FindInSampleSize(sourceSize, wantedSize) < 2) isNeeded = false;
#endif
                if (isNeeded) await Resize(source, cache, wantedSize);
                else await source.CopyToAsync(cache);
            }

            async Task DecodeImageFile(FileInfo toLoad)
            {
                try { Image = await DecodeImage(toLoad, Size, Stretch); }
                catch (Exception ex)
                {
                    var wrappedEx = new Exception($"Failed to load the image: {toLoad.FullName}{Environment.NewLine}Error: {ex.Message}");
                    Log.For(this).Error(wrappedEx);
                    await RemoveImageFileIfText(toLoad);
                    throw wrappedEx;
                }
            }

            static async Task RemoveImageFileIfText(FileInfo file)
            {
                // Is this image file in fact text (error message)?
                if (!file.Exists()) return;

                try
                {
                    var text = (await file.ReadAllTextAsync()).OrEmpty();
                    if (text.Contains("<html>", caseSensitive: false) || text.Contains("error", caseSensitive: false))
                    {
                        Log.For<ImageSource>().Warning("This file seems to be a text file (perhaps containing error messages). Attempting to delete it: " + file.FullName);
                        await file.DeleteAsync(false);
                    }
                }
                catch (Exception ex)
                {
                    Log.For<ImageSource>().Error(ex, "Corrupted image file clean up error.");
                    // No logging needed
                    return;
                }
            }

            FileInfo GetPhysicalFileToLoad()
            {
                if (!File.Exists())
                {
                    Log.For(this).Error("Image file not found: " + Source);
                    var result = Device.IO.File(FailedPlaceholderImagePath);
                    if (!result.Exists()) throw new Exception(FailedPlaceholderImagePath + " file must exist.");
                    return result;
                }

                return File;
            }

            public void RegisterViewer()
            {
                lock (ProviderSyncLock) Viewers++;
            }

            public void UnregisterViewer()
            {
                Thread.Pool.RunAction(() =>
                {
                    lock (ProviderSyncLock) Viewers--;

                    if (IsMemoryCached) return;

                    IdleUITasks.Run("Dispose unused image provider", () =>
                    {
                        lock (ProviderSyncLock)
                            if (Viewers == 0) ImageService.Dispose(this);
                    });
                });
            }

            public override int GetHashCode() => ToString().GetHashCode();

            public static bool operator !=(ImageSource @this, ImageSource another) => !(@this == another);

            public static bool operator ==(ImageSource @this, ImageSource another) => @this?.Equals(another) ?? another is null;

            public override bool Equals(object obj)
            {
                var another = obj as ImageSource;
                if (another is null) return false;

                return CacheKey == another.CacheKey &&
                    Stretch == another.Stretch &&
                    Size.AlmostEquals(another.Size) &&
                    Source == another.Source;
            }
        }
    }
}