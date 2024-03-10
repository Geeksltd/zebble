namespace Zebble.Device
{
    using System;
    using System.Threading.Tasks;
    using Olive;

    public static partial class OS
    {
#if ANDROID
        public static readonly DevicePlatform Platform = DevicePlatform.Android;
#elif IOS
        public static readonly DevicePlatform Platform = DevicePlatform.IOS;
#else
        public static readonly DevicePlatform Platform = DevicePlatform.Windows;
#endif  

        public static async Task<bool> OpenBrowser(string url, OnError errorAction = OnError.Toast)
        {
            try
            {
                return await Thread.UI.Run(() => DoOpenBrowser(url));
            }
            catch (Exception ex)
            {
                await errorAction.Apply(ex, "Failed to launch the browser to show a URL.");
                return false;
            }
        }

        public static async Task<bool> OpenSettings(string section = null, OnError errorAction = OnError.Toast)
        {
            try
            {
                return await Thread.UI.Run(() => DoOpenSettings(section));
            }
            catch (Exception ex)
            {
                await errorAction.Apply(ex, "Failed to open the settings app.");
                return false;
            }
        }

        static string hardwareModel;
        public static string HardwareModel
        {
            get
            {
                if (hardwareModel.HasValue()) return hardwareModel;

                try
                {
                    hardwareModel = DetectHardwareModel();
                    return hardwareModel.Or("UNKNOWN");
                }
                catch (Exception ex)
                {
                    Log.For(typeof(OS)).Error(ex, "Failed to detect device model.");
                    return "UNKNOWN";
                }
            }
        }

        static string version;
        public static string Version
        {
            get
            {
                if (version.HasValue()) return version;

                try
                {
                    version = DetectOSVersion();
                    return version.Or("UNKNOWN");
                }
                catch (Exception ex)
                {
                    Log.For(typeof(OS)).Error(ex, "Failed to detect device model.");
                    return "UNKNOWN";
                }
            }
        }
    }
}