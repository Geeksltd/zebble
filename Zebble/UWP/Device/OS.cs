namespace Zebble.Device
{
    using System;
    using System.Threading.Tasks;
    using Windows.Security.ExchangeActiveSyncProvisioning;
    using Olive;

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
            if (id.IsEmpty()) throw new Exception("'Application.Windows.ID' was not found in the config file.");

            return "https:" + "//www.microsoft.com/en-gb/store/p/a/" + id;
        }

        public static Task LaunchAppRating() => OpenBrowser(GetAppRatingUrl());

        static string DetectHardwareModel()
        {
            var eas = new EasClientDeviceInformation();
            return new[] { eas.SystemManufacturer, eas.SystemProductName }.Trim().ToString(" ");
        }
    }
}