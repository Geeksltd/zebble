namespace Zebble.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Threading.Tasks;
    using Olive;

    partial class ImageService
    {
        /// <summary>
        /// Fired every time an image is downloaded from a remote URL.
        /// </summary>
        public static AsyncEvent<string> ImageDownloaded = new AsyncEvent<string>();

        /// <summary>
        /// Determines a specified remote image has already been downloaded.
        /// </summary> 
        public static Task<bool> IsDownloaded(string url) => GetFile(url).ExistsAsync();

        internal static FileInfo GetFile(string path)
        {
            if (path.IsUrl())
            {
                var ext = "png";
                if (path.ToLower().EndsWith(".webp")) ext = "webp";
                if (path.ToLower().EndsWithAny(".jpg", "jpeg")) ext = "jpg";

                path = path.ToIOSafeHash();
                path = Device.IO.Cache.GetFile($"{path}.{ext}").FullName;
            }

            return Device.IO.File(path);
        }

        partial class ImageSource
        {
            static ConcurrentDictionary<string, TaskCompletionSource<bool>> DownloadQueue =
                new ConcurrentDictionary<string, TaskCompletionSource<bool>>();

            async Task Download()
            {
                var key = Url.ToLower();

                TaskCompletionSource<bool> item = null;
                var alreadyDownloading = false;

                lock (DownloadQueue)
                {
                    if (DownloadQueue.TryGetValue(key, out item)) alreadyDownloading = true;
                    else DownloadQueue.TryAdd(key, item = new TaskCompletionSource<bool>());
                }

                if (alreadyDownloading) await item.Task;
                else
                {
                    try
                    {
                        var succeeded = await Device.Network.Download(Url.AsUri(), File.FullName);
                        if (!succeeded) Log.For(this).Warning("Failed to download: " + Url);
                        item.TrySetResult(result: succeeded);
                        if (succeeded) ImageDownloaded.SignalRaiseOn(Thread.Pool, Url);
                    }
                    catch (Exception ex)
                    {
                        var error = new Exception("Failed to download the image " + Url + "\n" + ex.Message +
                            "\n" + ex.StackTrace, ex);

                        item.TrySetException(error);
                        throw error;
                    }
                    finally
                    {
                        DownloadQueue.TryRemove(key);
                    }
                }

                LoadingFromFileTask?.TrySetResult(result: false);
                LoadingFromFileTask = null;
                IsDownloading = false;

                await LoadFromFile();
            }
        }
    }
}