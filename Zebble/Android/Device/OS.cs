namespace Zebble.Device
{
    using System;
    using System.Threading.Tasks;
    using Android.App;
    using Android.Content;
    using Android.OS;
    using AndroidOS;
    using Olive;

    partial class OS
    {
        static Task<bool> DoOpenBrowser(string url)
        {
            using (var browserIntent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(url)))
            {
                (UIRuntime.NativeRootScreen as BaseActivity).StartActivity(browserIntent);
                return Task.FromResult(result: true);
            }
        }

        static Task<bool> DoOpenSettings(string section)
        {
            // TODO: How to implement section?

            var intent = new Intent()
                .SetAction(Android.Provider.Settings.ActionApplicationDetailsSettings)
                .AddCategory(Intent.CategoryDefault)
                .SetData(Android.Net.Uri.Parse("package:" + UIRuntime.CurrentActivity.PackageName))
                .AddFlags(ActivityFlags.NewTask | ActivityFlags.NoHistory | ActivityFlags.ExcludeFromRecents);

            UIRuntime.CurrentActivity.StartActivity(intent);
            return Task.FromResult(result: true);
        }

        /// <summary>Gets the url to rate this app on the App store, Google store or Windows store.</summary>
        public static string GetAppRatingUrl()
        {
            return "https:" + "//play.google.com/store/apps/details?id=" + GetAppId();
        }

        static string GetAppId()
        {
            var id = Config.Get("Application.Android.ID");
            if (id.IsEmpty()) throw new Exception("'Application.Android.ID' was not found in the config file.");
            return id;
        }

        public static Task LaunchAppRating()
        {
            try
            {
                var url = Android.Net.Uri.Parse($"market://details?id={GetAppId()}");
                Application.Context.StartActivity(new Intent(Intent.ActionView, url).SetFlags(ActivityFlags.NewTask));
                return Task.CompletedTask;
            }
            catch
            {
                return OpenBrowser(GetAppRatingUrl());
            }
        }

        /// <summary>
        /// It will show the device home screen by suspending the app.
        /// </summary>
        public static void OpenHomeScreen()
        {
            UIRuntime.CurrentActivity.StartActivity(new Intent(Intent.ActionMain).AddCategory(Intent.CategoryHome)
                .SetFlags(ActivityFlags.NewTask));
        }

        static string DetectHardwareModel()
        {
            var manufacturer = Build.Manufacturer;
            var model = Build.Model;

            if (model?.StartsWith(manufacturer) == true)
                return model.ToProperCase();

            return new[] { manufacturer?.ToProperCase(), model?.ToProperCase() }.Trim().ToString(" ");
        }

        public static bool IsAtLeast(BuildVersionCodes version) => Build.VERSION.SdkInt >= version;

        public static ActivityManager.MemoryInfo GetMemoryInfo()
        {
            var result = new ActivityManager.MemoryInfo();
            UIRuntime.GetService<ActivityManager>("activity").GetMemoryInfo(result);
            return result;
        }

        public static Task NativeAlert(string title, string message)
        {
            var ok = new SimpleAwaiterDialogInterface();

            new AlertDialog.Builder(UIRuntime.CurrentActivity)
                       .SetTitle(title).SetMessage(message)
                        .SetPositiveButton("OK", ok)
                        .Show();

            return ok.Task;
        }

        class SimpleAwaiterDialogInterface : Java.Lang.Object, IDialogInterfaceOnClickListener
        {
            readonly TaskCompletionSource<bool> Source = new();
            public void OnClick(IDialogInterface dialog, int which) => Source.TrySetResult(true);

            public Task Task => Source.Task;
        }
    }
}