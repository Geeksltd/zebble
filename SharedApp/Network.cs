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
        static ConcurrentDictionary<string, HttpClient> Clients = new ConcurrentDictionary<string, HttpClient>();
        const int DEFAULT_TIME_OUT = 5000;
        public static readonly AsyncEvent<bool> ConnectivityChanged = new AsyncEvent<bool>(ConcurrentEventRaisePolicy.Queue);

        // static SemaphoreSlim DownloadThrottler;
        static List<Task> AllDownloads;
        // static int THROTTLING_LIMIT => Config.Get("Max.Concurrent.Download.Per.Domain", 5);

        static Network()
        {
#if ANDROID || IOS
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;
#endif
            Init();
            // DownloadThrottler = new SemaphoreSlim(THROTTLING_LIMIT);
            AllDownloads = new List<Task>();
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
            if (hostName.ContainsAny(new[] { "/", ":" })) throw new Exception($"Invalid host name format: {hostName}");
            return Clients.GetOrAdd(
                $"{scheme}:{hostName}:{port}|{timeout.TotalMilliseconds}",
                _ =>
                {
                    var client = HttpClient();
                    client.BaseAddress = new Uri($"{scheme}://{hostName}:{port}");
                    client.Timeout = timeout;
                    return client;
                }
            );
        }

        public static HttpClient HttpClient() => new(CreateHttpHandler());

        static HttpMessageHandler CreateHttpHandler()
        {
#if ANDROID
            return new Xamarin.Android.Net.AndroidClientHandler();
#elif IOS
            return new NSUrlSessionHandler();
#else
            return new HttpClientHandler();
#endif
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
                        var data = d.Result;
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
                    if (Config.Get<bool>("Offline.Mode")) throw new Exception("Simulated as offline");

                    try
                    {
                        var fetchTask = HttpClient(url, timeoutPerAttempt.Seconds()).GetAsync(url);

                        if (await Task.WhenAny(Task.Delay(timeoutPerAttempt.Seconds()), fetchTask) != fetchTask || fetchTask.IsCanceled)
                        {
                            Log.For(typeof(Network)).Warning("Attempt #" + retry + " timed out for downloading " + url);
                        }
                        else
                        {
                            using var httpResponse = fetchTask.GetAlreadyCompletedResult();
                            if (httpResponse.StatusCode == HttpStatusCode.OK)
                            {
                                data = await httpResponse.Content.ReadAsByteArrayAsync();
                                if (saveOutput != null)
                                    await saveOutput.WriteAllBytesAsync(data);
                            }
                            else
                            {
                                Log.For(typeof(Network)).Warning("Attempt #" + retry + " failed for " + url + " >> " + httpResponse.StatusCode);
                            }
                        }
                    }
                    catch (TimeoutException)
                    {
                        Log.For(typeof(Network)).Warning("Attempt #" + retry + " timed out for downloading " + url);
                    }
                    catch (OperationCanceledException)
                    {
                        Log.For(typeof(Network)).Warning("Attempt #" + retry + " timed out for downloading " + url);
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                    Log.For(typeof(Network)).Warning($"Attempt #{retry} failed for downloading {url}\n{ex.Message}");
                }

                if (data != null) break;
            }

            if (data is null)
            {
                error ??= new Exception(retries + " attempts to download a URL all failed: " + url);

                source.TrySetException(error);
            }

            source.TrySetResult(data);
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