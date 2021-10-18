namespace Zebble.Device
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Android.App;
    using Android.Content;
    using Android.Net;
    using Android.Net.Wifi;
    using Android.OS;
    using Android.Runtime;
    using Java.Net;
    using Olive;
    using Context = Android.Content.Context;

    partial class Network
    {
        static ConnectivityChangeReceiver Receiver;
        static ConnectivityManager Manager;
        static WifiManager WifiManager;

        public static void Init()
        {
            WifiManager = UIRuntime.GetService<WifiManager>(Context.WifiService);
            Manager = UIRuntime.GetService<ConnectivityManager>(Context.ConnectivityService);
            Receiver = new ConnectivityChangeReceiver();
            Application.Context.RegisterReceiver(Receiver, new IntentFilter(ConnectivityManager.ConnectivityAction));
        }

        static IEnumerable<DeviceConnectionType> DoGetConnectionTypes()
        {
            // When on API 21+ need to use getAllNetworks, else fall base to GetAllNetworkInfo
            if (Device.OS.IsAtLeast(BuildVersionCodes.Lollipop))
            {
                foreach (var network in Manager.GetAllNetworks())
                {
                    var info = Manager.GetNetworkInfo(network);

                    if (info?.Type is null)
                        yield return DeviceConnectionType.Other;

                    yield return GetConnectionType(info.Type);
                }
            }
            else
            {
                foreach (var info in Manager.GetAllNetworkInfo())
                {
                    if (info?.Type is null)
                        yield return DeviceConnectionType.Other;

                    yield return GetConnectionType(info.Type);
                }
            }
        }

        static DeviceConnectionType GetConnectionType(ConnectivityType connectivityType)
        {
            switch (connectivityType)
            {
                case ConnectivityType.Ethernet: return DeviceConnectionType.Ethernet;
                case ConnectivityType.Wifi: return DeviceConnectionType.WiFi;
                case ConnectivityType.Bluetooth: return DeviceConnectionType.Bluetooth;
                case ConnectivityType.Mobile:
                case ConnectivityType.MobileDun:
                case ConnectivityType.MobileHipri:
                    return DeviceConnectionType.Cellular;
                default:
                    return DeviceConnectionType.Other;
            }
        }

        public static bool GetIsConnected()
        {
            try
            {
                if (Device.OS.IsAtLeast(BuildVersionCodes.Lollipop))
                {
                    return Manager.GetAllNetworks().Select(Manager.GetNetworkInfo).ExceptNull().Any(x => x.IsConnected);
                }
                else
                {
                    return Manager.GetAllNetworkInfo().ExceptNull().Any(x => x.IsConnected);
                }
            }
            catch (Exception ex)
            {
                Log.For(typeof(Network)).Error(ex);
                return false;
            }
        }

        public static Task<bool> IsAvailable() => Task.FromResult(GetIsConnected());

        static Task<bool> GetAvailabilityByHost(string host, int timeout)
        {
            bool exists = false;

            try
            {
                var ip = InetAddress.GetByName(host);
                SocketAddress sockaddr = new InetSocketAddress(ip, 80);
                var socket = new Socket();

                socket.Connect(sockaddr, timeout);
                exists = true;

                socket.Close();
            }
            catch (Exception ex)
            {
                Log.For(typeof(Network)).Error(ex);
            }

            return Task.FromResult(exists);
        }

        static Task<bool> Responds(string host, int timeout)
        {
            var result = false;

            if (GetIsConnected())
                return GetAvailabilityByHost(host, timeout);

            return Task.FromResult(result);
        }

        static async Task<List<ulong>> GetBandwidthsFromDevice()
        {
            if ((await GetConnectionTypes()).Contains(DeviceConnectionType.WiFi))
                return (new[] { (ulong)WifiManager.ConnectionInfo.LinkSpeed }).ToList();
            else
                return new List<ulong>();
        }

        [BroadcastReceiver(Enabled = true), Preserve(AllMembers = true)]
        public class ConnectivityChangeReceiver : BroadcastReceiver
        {
            bool? IsConnected;

            [Preserve]
            protected ConnectivityChangeReceiver(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }
            public ConnectivityChangeReceiver() => IsConnected = GetIsConnected();

            public override void OnReceive(Context context, Intent intent)
            {
                if (intent.Action != ConnectivityManager.ConnectivityAction) return;
                var nowConnected = GetIsConnected();

                if (nowConnected != IsConnected)
                {
                    IsConnected = nowConnected;
                    ConnectivityChanged.SignalRaiseOn(Thread.Pool, nowConnected);
                }
            }
        }
    }
}