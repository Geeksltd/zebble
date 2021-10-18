namespace Zebble
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using Foundation;
    using UIKit;

    partial class UIRuntime
    {
        internal const bool IsDevMode = false;

        /// <summary>
        /// This will be called whenever a new url opens in app
        /// </summary>
        public static AsyncEvent<NSUrl> OnOpenUrl = new();

        /// <summary>
        /// This will be called whenever a new url opens in app with more options
        /// </summary>
        public static AsyncEvent<Tuple<UIApplication, NSUrl, string, NSDictionary>> OnOpenUrlWithOptions = new();

        /// <summary>
        /// This will be called whenever application launching finished.
        /// </summary>
        public static AsyncEvent OnFinishedLaunching = new();

        /// <summary>
        /// This will be called whenever view controller motion ended.
        /// </summary>
        public static AsyncEvent<UIEventSubtype> OnViewMotionEnded = new();

        public static Func<NSDictionary, Task<bool>> DidReceiveRemoteNotification;

        public static readonly AsyncEvent<NSDictionary> OnParameterRecieved = new();

        public static readonly AsyncEvent<NSData> RegisteredForRemoteNotifications = new();

        public static readonly AsyncEvent<NSError> FailedToRegisterForRemoteNotifications = new();

        /// <summary>
        /// Gets the key window of the app.
        /// </summary>
        public static UIWindow Window => UIApplication.SharedApplication.KeyWindow;
    }
}