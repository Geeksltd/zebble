namespace Zebble.Device
{
    using System;
    using System.Threading.Tasks;
    using Windows.Security.ExchangeActiveSyncProvisioning;
    using Olive;
    using Windows.System.Profile;

    partial class OS
    {
        async static Task<bool> DoOpenBrowser(string url)
        {
            await Windows.System.Launcher.LaunchUriAsync(url.AsUri());
            return true;
        }

        static Task<bool> DoOpenSettings(string section) => DoOpenBrowser("ms-settings:" + section);

        /// <summary>Gets the url to rate this app on the App store, Google store or Windows store.</summary>
        public static string GetAppRatingUrl()
        {
            var id = Config.Get("Application.Windows.ID");
            if (id.IsEmpty()) throw new InvalidStateException("'Application.Windows.ID' was not found in the config file.");

            return "https:" + "//www.microsoft.com/en-gb/store/p/a/" + id;
        }

        public static Task LaunchAppRating() => OpenBrowser(GetAppRatingUrl());

        static string DetectHardwareModel()
        {
            var eas = new EasClientDeviceInformation();
            return new[] { eas.SystemManufacturer, eas.SystemProductName }.Trim().ToString(" ");
        }

        static string DetectOSVersion()
        {
            string sv = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
            ulong v = ulong.Parse(sv);
            ulong v1 = (v & 0xFFFF000000000000L) >> 48;
            ulong v2 = (v & 0x0000FFFF00000000L) >> 32;
            ulong v3 = (v & 0x00000000FFFF0000L) >> 16;
            ulong v4 = (v & 0x000000000000FFFFL);
            return $"{v1}.{v2}.{v3}.{v4}";
        }
    }
}