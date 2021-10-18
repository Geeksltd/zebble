namespace Zebble
{
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    public class AsyncEvent : AbstractAsyncEvent
    {
        public AsyncEvent([CallerMemberName] string eventName = "", [CallerFilePath] string declaringFile = "") : base(eventName, declaringFile) { }

        public AsyncEvent(ConcurrentEventRaisePolicy raisePolicy, [CallerMemberName] string eventName = "", [CallerFilePath] string declaringFile = "") : base(eventName, declaringFile)
        {
            ConcurrentRaisePolicy = raisePolicy;
        }

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task RaiseHandler(AsyncEventHandler handler)
        {
            if (handler is AsyncEventActionHandler h) return h.Raise();
            else if (handler is AsyncEventTaskHandler t) return t.Raise();
            else return Task.CompletedTask;
        }

        /// <summary>Will run its event handlers on the UI or ThreadPool as specified.</summary>
        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task RaiseOn(BaseThread thread)
        {
            if (!IsHandled()) return Task.CompletedTask;

            var runner = thread.Run(() => Raise());

#if IOS || ANDROID || UWP
            if (Thread.UI.IsRunning() && thread == Thread.Pool)
                return Task.CompletedTask;
#endif

            return runner;
        }

        /// <summary>Will run its event handlers on the UI or ThreadPool as specified but does not wait for the handlers to complete.</summary>
        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SignalRaiseOn(BaseThread thread)
        {
            if (!IsHandled()) return;
            thread.Run(() => Raise());
        }

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task Raise() => Raise(inParallel: false);

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task Raise(bool inParallel)
        {
            return Raise(RaiseHandler, inParallel);
        }
    }
}