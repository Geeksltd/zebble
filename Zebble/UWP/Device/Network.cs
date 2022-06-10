namespace Zebble.Device
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Olive;
    using Windows.Networking.Connectivity;
    using Windows.UI.Core;

    partial class Network
    {
        static bool hasNetworkAccess;
        static bool hasInternetAccess;
        static bool isWifi;
        static bool isMobileData;
        static ConnectionProfile netInfo;
        static ulong bandwidth;

        static void Init() 
        {
            
            NetworkStatusChanged(null);
            NetworkInformation.NetworkStatusChanged += NetworkStatusChanged;

        }
        private static void NetworkStatusChanged(object sender)
        {
            netInfo = NetworkInformation.GetInternetConnectionProfile();
            if (netInfo == null)
            {
                hasNetworkAccess = false;
                hasInternetAccess = false;
                isWifi = false;
                isMobileData = false;
                bandwidth=0;
            }
            else
            {
                var level = netInfo.GetNetworkConnectivityLevel();
                UpdateConnectionLevel(level);
                isWifi = netInfo.IsWlanConnectionProfile;
                isMobileData = netInfo.IsWwanConnectionProfile;

                var networkAdapter=netInfo.NetworkAdapter;
                if (networkAdapter != null)
                {
                    bandwidth= networkAdapter.InboundMaxBitsPerSecond;
                }
            }
        }

        private static void UpdateConnectionLevel(NetworkConnectivityLevel level)
        {
            if (netInfo == null)
            {
                NetworkStatusChanged(null);
            }

            switch (level)
            {
                case NetworkConnectivityLevel.None:
                    hasInternetAccess = false;
                    hasNetworkAccess = false;
                    break;
                case NetworkConnectivityLevel.LocalAccess:
                    hasInternetAccess = false;
                    hasNetworkAccess = true;
                    break;
                case NetworkConnectivityLevel.ConstrainedInternetAccess:
                    hasInternetAccess = true;
                    hasNetworkAccess = true;
                    break;
                case NetworkConnectivityLevel.InternetAccess:
                    hasInternetAccess = true;
                    hasNetworkAccess = true;
                    break;
                default:
                    hasInternetAccess = false;
                    hasNetworkAccess = false;
                    break;
            }

        }

        static async Task<List<ulong>> GetBandwidthsFromDevice()
        {
            if (netInfo == null)
            {
                NetworkStatusChanged(null);
            }
            return new List<ulong> { bandwidth};
        }

        static IEnumerable<DeviceConnectionType> DoGetConnectionTypes()
        {
            if (netInfo == null)
            {
                NetworkStatusChanged(null);
            }
            if (hasNetworkAccess)
            {
                if (isWifi)
                {
                    yield return DeviceConnectionType.WiFi;
                }
                if (isMobileData)
                {
                    yield return DeviceConnectionType.Cellular;
                }
                yield return DeviceConnectionType.Ethernet;
            }
            else
            {
                yield return DeviceConnectionType.Other;
            }
        }

        public static async Task<bool> IsAvailable()
        {
            if (netInfo==null)
            {
                NetworkStatusChanged(null);
            }
            return hasInternetAccess;
        }

        static async Task<bool> Responds(string host, int timeout)
        {
            foreach (var protocol in new[] { "https://", "http://" })
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