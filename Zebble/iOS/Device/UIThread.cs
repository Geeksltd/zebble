namespace Zebble
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    partial class UIThread
    {
        public static Foundation.NSObject Dispatcher;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Dispatch(Action action)
        {
            Dispatcher.BeginInvokeOnMainThread(action);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool IsRunning() => Foundation.NSThread.Current.IsMainThread;

        /// <summary>Schedules a specified action to be done on the UI thread, and returns immediately.
        /// If the current thread is already the UI thread, then the action will be posted 
        /// to run when the UI thread is done doing current tasks.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Post(Action action)
        {
            if (action is null) return;
            Task.Delay(Animation.OneFrame).ContinueWith(x => Thread.UI.RunAction(action)).RunInParallel();
        }
    }
}