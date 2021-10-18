namespace Zebble
{
    using System;
    using System.Runtime.CompilerServices;

    partial class UIThread
    {
        Android.OS.Looper MainLooper; // = Android.OS.Looper.MainLooper;

        /// <summary>Schedules a specified action to be done on the UI thread, and returns immediately.
        /// If the current thread is already the UI thread, then the action will be posted to run when the UI thread is done doing current tasks.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Post(Action action)
        {
            if (action is null) return;

            using (var handler = new Android.OS.Handler(Android.OS.Looper.MainLooper))
                handler.Post(action);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool IsRunning()
        {
            if (MainLooper is null) MainLooper = Android.OS.Looper.MainLooper;

            return Android.OS.Looper.MyLooper() == MainLooper;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Dispatch(Action action)
        {
            ((Android.App.Activity)UIRuntime.NativeRootScreen).RunOnUiThread(action);
        }
    }
}