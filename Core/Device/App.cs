namespace Zebble.Device
{
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Olive;

    public partial class App
    {
        internal static bool ExitingWithError;

        public static bool IsActive { get; private set; } = true;

        public static bool IsStopping { get; private set; } = true;

        public static event Action Started;

        public static event Action ReceivedMemoryWarning;

        /// <summary>
        /// Use this method to release shared resources, save user data, invalidate timers and store the application state.
        /// If your application supports background execution this method is called instead of WillTerminate when the user quits.
        /// Warning: This is invoked on the UI thread. Your code must also run on the UI thread.
        /// </summary>
        public static event Action WentIntoBackground;

        public static event Action CameToForeground;

        /// <summary>
        /// Raised (on the UI thread) when the application is closing.
        /// In iOS this is never invoked unless UIApplicationExitsOnSuspend is set in the info.plist, in which case WentIntoBackground is never invoked.
        /// It's recommended that you only use WentIntoBackground for a consistent experience.
        /// </summary>
        public static event Action Stopping;

        /// <summary>
        /// Will shut down the app.
        /// </summary> 
        public static async Task Stop()
        {
            IsStopping = true;
            await DoStop();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static void RaiseStarted()
        {
            IsActive = true;
            Started?.Invoke();
            IsStopping = false;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static void RaiseStopping() => Stopping?.Invoke();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void RaiseWentIntoBackground() => WentIntoBackground?.Invoke();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void RaiseCameToForeground() => CameToForeground?.Invoke();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void RaiseReceivedMemoryWarning()
        {
            Log.For<App>().Warning("Received memory warning!");
            Nav.DisposeCache();
            Services.ImageService.DisposeCache();

            GC.Collect();
            ReceivedMemoryWarning?.Invoke();

            // Force GC again after user's custom clean up code.
            if (ReceivedMemoryWarning != null) GC.Collect();
        }
    }
}