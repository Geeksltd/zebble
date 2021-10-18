namespace Zebble.Device
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Olive;

    partial class Network
    {
        const string PING_URL = "https://www.google.com";
        static Task<bool> CheckingTask;
        static void Init() { }

        static async Task<bool> Responds(string host, int timeout)
        {
            foreach (var protocol in new[] { "https://", "http://" })
            {
                try
                {
                    using (var stream = await HttpClient(host, timeout.Milliseconds()).GetStreamAsync(protocol + host))
                        if (stream.CanRead) return true;
                }
                catch
                {
                    // No logging is needed.
                }
            }

            return false;
        }

        static Task<List<ulong>> GetBandwidthsFromDevice()
        {
            // TODO
            throw new NotImplementedException();
        }

        static IEnumerable<DeviceConnectionType> DoGetConnectionTypes()
        {
            // TODO
            yield return DeviceConnectionType.WiFi;
        }

        public static async Task<bool> IsAvailable()
        {
            var checking = CheckingTask;
            if (checking != null) return await checking;

            CheckingTask = checking = Task.Run(async () =>
            {
                try
                {
                    using (var stream = await HttpClient(PING_URL, 3.Seconds()).GetStreamAsync(PING_URL))
                        return stream.CanRead;
                }
                catch
                {
                    // No logging is needed.
                    return false;
                }
                finally
                {
                    Task.Delay(1.Minutes()).ContinueWith(t => CheckingTask = null).GetAwaiter();
                }
            });

            return await checking;
        }
    }
}