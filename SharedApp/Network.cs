#if !FX46
namespace Zebble.Device
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Olive;

    public static partial class Network
    {
        const int DEFAULT_TIME_OUT = 5000;
        public static readonly AsyncEvent<bool> ConnectivityChanged = new(ConcurrentEventRaisePolicy.Queue);
        static readonly List<Task> AllDownloads = new();
        static ConcurrentDictionary<string, HttpClient> HttpClients = new();

        static Network()
        {
#if ANDROID || IOS
            ServicePointManager.ServerCertificateValidationCallback += (_, _, _, _) => true;
            ServicePointManager.DefaultConnectionLimit = 256;
#endif
            Init();
        }

        public static HttpClient HttpClient(string domainIpOrUrl, TimeSpan timeout)
        {
            domainIpOrUrl = domainIpOrUrl.ToLowerOrEmpty().Trim();

            if (domainIpOrUrl.Contains("://") || domainIpOrUrl.StartsWith("//"))
                return HttpClient(domainIpOrUrl.AsUri(), timeout);
            else
                return CreateHttpClient(domainIpOrUrl, timeout);
        }

        public static HttpClient HttpClient(Uri url, TimeSpan timeout) => CreateHttpClient(url.Host, timeout, url.Scheme, url.Port);

        static HttpClient CreateHttpClient(string hostName, TimeSpan timeout, string scheme = "http", int port = 80)
        {
            if (hostName.ContainsAny(["/", ":"])) throw new HttpRequestException($"Invalid host name format: {hostName}");
            var baseAddress = $"{scheme}://{hostName}:{port}";

            return HttpClients.GetOrAdd(baseAddress, CreateClient);

            HttpClient CreateClient()
            {
#if IOS
                var handler = new NSUrlSessionHandler();
#elif MAUI_ANDROID
                var handler = new Xamarin.Android.Net.AndroidMessageHandler();
#elif ANDROID
                var handler = new Xamarin.Android.Net.AndroidClientHandler();
#else
                var handler = new HttpClientHandler();
#endif

                return new HttpClient(handler) { BaseAddress = baseAddress.AsUri(), Timeout = timeout };
            }
        }

        /// <summary>
        /// Downloads a file from a url to a specific path on device
        /// </summary>
        /// <param name="url">file url that image will be downloaded from</param>
        /// <param name="path">Destination path that image will be saved</param>
        /// <returns>true if download is completed and the file is saved on device successfully</returns>
        public static Task<bool> Download(Uri url, string path, OnError errorAction = OnError.Ignore, int attempts = 1)
        {
            var result = new TaskCompletionSource<bool>();

            AllDownloads.Add(Task.Factory.StartNew(() =>
            {
                var file = Device.IO.File(path);
                file.Directory.EnsureExists();

                Download(url, attempts, DEFAULT_TIME_OUT / 1000).ContinueWith(d =>
                {
                    if (d.IsFaulted)
                    {
                        errorAction.Apply(d.Exception.InnerException, "Failed to download the file: " + url)
                        .ContinueWith(v => result.TrySetResult(result: false));
                    }
                    else
                    {
                        var data = d.GetAlreadyCompletedResult();
                        if (data == null) result.TrySetResult(result: true);
                        else file.WriteAllBytesAsync(data).ContinueWith(v => result.TrySetResult(result: true));
                    }
                });
            }));

            return result.Task;
        }

        /// <summary>
        /// Downloads the data from a specified URL.
        /// </summary>
        /// <param name="timeoutPerAttempt">The number of seconds to wait for each attempt.</param>
        public static Task<byte[]> FromCacheOrDownload(Uri url, int retries = 2, int timeoutPerAttempt = 30)
        {
            var inCache = IO.Cache.GetFile(url.ToString().ToSafeFileName());
            if (inCache.Exists()) return inCache.ReadAllBytesAsync();

            var source = new TaskCompletionSource<byte[]>();
            Zebble.Thread.Pool.RunAction(() => TryDownload(url, retries, timeoutPerAttempt, source, inCache));
            return source.Task;
        }

        /// <summary>
        /// Downloads the data from a specified URL.
        /// </summary>
        /// <param name="timeoutPerAttempt">The number of seconds to wait for each attempt.</param>
        public static Task<byte[]> Download(Uri url, int retries = 2, int timeoutPerAttempt = 30)
        {
            var source = new TaskCompletionSource<byte[]>();
            Zebble.Thread.Pool.RunAction(() => TryDownload(url, retries, timeoutPerAttempt, source));
            return source.Task;
        }

        static async void TryDownload(Uri url, int retries, int timeoutPerAttempt, TaskCompletionSource<byte[]> source, FileInfo saveOutput = null)
        {
            byte[] data = null;
            Exception error = null;

            for (var retry = 1; retry <= retries; retry++)
            {
                if (retry > 1)
                    await Task.Delay(100.Milliseconds());

                try
                {
                    var client = HttpClient(url, timeoutPerAttempt.Seconds());
                    var fetchTask = client.GetAsync(url);

                    var completedFirst = await Task.WhenAny(Task.Delay(timeoutPerAttempt.Seconds()), fetchTask);

                    if (completedFirst != fetchTask)
                    {
                        // Mark the exception as observed so it does not crash the whole thing.
                        fetchTask.ContinueWith(x => { }).GetAwaiter();
                        Log.For(typeof(Network)).Warning("Attempt #" + retry + " timed out for downloading " + url);
                    }
                    else if (fetchTask.Status == TaskStatus.Faulted || fetchTask.Status == TaskStatus.Canceled)
                        Log.For(typeof(Network)).Warning("Attempt #" + retry + " failed for " + url);
                    else
                    {
                        // Do not dispose the response and let the GC do it
                        // The reason is in some scenarios, iOS may reuse the instances
                        // https://github.com/xamarin/xamarin-macios/blob/c32c925eb52239e1cef8b6e708bcbff7f015ec9f/src/Foundation/NSUrlSessionHandler.cs#L876
                        // https://github.com/xamarin/xamarin-macios/blob/c32c925eb52239e1cef8b6e708bcbff7f015ec9f/src/Foundation/NSUrlSessionHandler.cs#L1398
                        var httpResponse = fetchTask.GetAlreadyCompletedResult();

                        if (httpResponse.StatusCode.IsAnyOf(HttpStatusCode.OK, HttpStatusCode.Created))
                        {
                            data = await httpResponse.Content.ReadAsByteArrayAsync();

                            if (saveOutput != null)
                                await saveOutput.WriteAllBytesAsync(data);

                            break;
                        }
                        else
                        {
                            Log.For(typeof(Network)).Warning("Attempt #" + retry + " failed for " + url + " >> " + httpResponse.StatusCode);
                        }
                    }
                }
                catch (Exception ex) when (ex is TimeoutException || ex is TaskCanceledException || ex is OperationCanceledException)
                {
                    Log.For(typeof(Network)).Warning("Attempt #" + retry + " timed out for downloading " + url);
                }
                catch (Exception ex)
                {
                    error = ex;
                    Log.For(typeof(Network)).Warning($"Attempt #{retry} failed for downloading {url}\n{ex.Message}");
                }
            }

            if (data is null)
            {
                error ??= new HttpRequestException(retries + " attempts to download a URL all failed: " + url);
                source.TrySetException(error);
            }
            else source.TrySetResult(data);
        }

        public static async Task<IEnumerable<ulong>> GetBandwidths(OnError errorAction = OnError.Ignore)
        {
            try
            {
                return await GetBandwidthsFromDevice();
            }
            catch (Exception ex)
            {
                await errorAction.Apply($"Unable to get connected state - error: {ex}");
                return new List<ulong>();
            }
        }

        /// <summary>
        /// Tests if a host name is accessible (i.e. there is internet connection available on the device, and the address itself is valid and responding to network requests.
        /// </summary>
        /// <param name="destination">The host name can machine name, domain name, IP address, etc.</param>
        public static async Task<bool> IsHostReachable(string destination, int timeout = DEFAULT_TIME_OUT)
        {
            destination = destination.ToLowerOrEmpty().RemoveBefore("//").TrimEnd('/').Trim();

            if (destination.IsEmpty()) throw new ArgumentNullException(nameof(destination));

            try
            {
                return await Responds(destination, timeout);
            }
            catch { /* No logging is needed. */ return false; }
        }

        public static async Task<DeviceConnectionType[]> GetConnectionTypes(OnError errorAction = OnError.Throw)
        {
            try
            {
                return DoGetConnectionTypes().ToArray();
            }
            catch (Exception ex)
            {
                await errorAction.Apply($"Unable to get connected state - error: {ex}");
                return new DeviceConnectionType[0];
            }
        }
    }
}
#endif