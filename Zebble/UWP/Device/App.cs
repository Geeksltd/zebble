namespace Zebble.Device
{
    using System;
    using System.Threading.Tasks;
    using Windows.System.Profile;

    partial class App
    {
        public static bool IsDesktop() => AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop";

        public static void ExitWithError(string error)
        {
            if (ExitingWithError) return;
            else ExitingWithError = true;

            UIThread.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                var dialog = new Windows.UI.Popups.MessageDialog(error, "Fatal error!");
                await dialog.ShowAsync();
                Windows.ApplicationModel.Core.CoreApplication.Exit();
            }).AsTask().ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();
        }

        static Task DoStop()
        {
            Windows.ApplicationModel.Core.CoreApplication.Exit();
            return Task.CompletedTask;
        }
    }
}