namespace Zebble.Device
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Olive;

    partial class Network
    {
        static void Init() { }
        static Task<List<ulong>> GetBandwidthsFromDevice() => throw new NotImplementedException();
        static IEnumerable<DeviceConnectionType> DoGetConnectionTypes() => new[] { DeviceConnectionType.WiFi };
        public static Task<bool> IsAvailable() => Task.FromResult(Mvvm.SimulatedEnvironment.IsNetworkAvailable);

        static async Task<bool> Responds(string host, int timeout)
        {
            foreach (var protocol in new[] { "http://", "https://" })
            {
                try
                {
                    using var client = HttpClient(host, timeout.Milliseconds());
                    using var stream = await client.GetStreamAsync(protocol + host);
                    if (stream.CanRead) return true;
                }
                catch
                {
                    // No logging is needed.
                }
            }

            return false;
        }
    }
}