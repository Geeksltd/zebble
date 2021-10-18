namespace Zebble.Device
{
    using CoreFoundation;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using SystemConfiguration;
    using Olive;

    partial class Network
    {
        static bool IsConnected;
        static NetworkReachability NetworkReachability;

        static void Init() => IsConnected = GetConnectionStatus() != NetworkStatus.Unavailable;

        static async void OnStatusChanged()
        {
            await Task.Delay(100);
            var newStatus = GetConnectionStatus() != NetworkStatus.Unavailable;

            if (IsConnected == newStatus) return;
            IsConnected = newStatus;

            ConnectivityChanged.SignalRaiseOn(Thread.Pool, newStatus);
        }

        public static Task<bool> IsAvailable() => Task.FromResult(GetConnectionStatus() != NetworkStatus.Unavailable);

        static async Task<bool> Responds(string host, int timeout)
        {
            if (!IsConnected) return false;

            SocketAsyncEventArgs socketEventArg;
            var result = new TaskCompletionSource<bool>();

            void socketCompleted(object _, SocketAsyncEventArgs e)
            {
                result.TrySetResult(e.SocketError == SocketError.Success);
                if (socketEventArg != null) socketEventArg.Completed -= socketCompleted;
                socketEventArg = null;
            }

            using (var socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socketEventArg = new SocketAsyncEventArgs { RemoteEndPoint = new DnsEndPoint(host, 80) };
                socketEventArg.Completed += socketCompleted;
                socket.ConnectAsync(socketEventArg);

                return await result.Task.WithTimeout(timeout.Milliseconds(), timeoutAction: () => false);
            }
        }

        static Task<List<ulong>> GetBandwidthsFromDevice()
        {
            // Not supported on iOS
            return Task.FromResult(new List<ulong>());
        }

        static IEnumerable<DeviceConnectionType> DoGetConnectionTypes()
        {
            var result = new List<DeviceConnectionType>();

            switch (GetConnectionStatus())
            {
                case NetworkStatus.Mobile:
                    result.Add(DeviceConnectionType.Cellular);
                    break;
                case NetworkStatus.Wifi:
                    result.Add(DeviceConnectionType.WiFi);
                    break;
            }

            return result;
        }

        enum NetworkStatus { Unavailable, Mobile, Wifi }

        static bool IsNetworkAvailable(out NetworkReachabilityFlags flags)
        {
            if (NetworkReachability is null)
            {
                var ip = new IPAddress(0);
                NetworkReachability = new NetworkReachability(ip);
                NetworkReachability.SetNotification(x => OnStatusChanged());
                NetworkReachability.Schedule(CFRunLoop.Main, CFRunLoop.ModeDefault);
            }

            if (!NetworkReachability.TryGetFlags(out flags)) return false;

            if ((flags & NetworkReachabilityFlags.Reachable) == 0) return false;

            if ((flags & NetworkReachabilityFlags.IsWWAN) == 0) return true;

            return (flags & NetworkReachabilityFlags.ConnectionRequired) == 0;
        }

        static NetworkStatus GetConnectionStatus()
        {
            var status = NetworkStatus.Unavailable;

            if (IsNetworkAvailable(out var flags))
                status = NetworkStatus.Wifi;

            if ((flags & NetworkReachabilityFlags.InterventionRequired) == 0)
            {
                if ((flags & NetworkReachabilityFlags.ConnectionOnDemand) != 0)
                    status = NetworkStatus.Wifi;

                if ((flags & NetworkReachabilityFlags.ConnectionOnTraffic) != 0)
                    status = NetworkStatus.Wifi;
            }

            if ((flags & NetworkReachabilityFlags.IsWWAN) != 0)
                status = NetworkStatus.Mobile;

            return status;
        }
    }
}