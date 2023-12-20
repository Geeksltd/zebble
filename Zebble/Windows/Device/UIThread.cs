namespace Zebble
{
    using System;
    using System.Runtime.CompilerServices;

    partial class UIThread
    {
        public static Windows.UI.Core.CoreDispatcher Dispatcher;
        internal static int UIThreadID;

        /// <summary>Schedules a specified action to be done on the UI thread, and returns immediately.
        /// If the current thread is already the UI thread, then the action will be posted to run when the UI thread is done doing current tasks.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Post(Action action)
        {
            if (action is null) return;
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => RunAction(action)).AsTask();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool IsRunning() => Environment.CurrentManagedThreadId == UIThreadID;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void DoDispatch(Action action)
        {
#pragma warning disable CS4014 
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => action());
#pragma warning restore CS4014
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Dispatch(Action action) => DoDispatch(action);
    }
}