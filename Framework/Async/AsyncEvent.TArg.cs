namespace Zebble
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    public class AsyncEvent<TArg> : AbstractAsyncEvent
    {
        public event Action<TArg> FullEvent;

        public AsyncEvent([CallerMemberName] string eventName = "", [CallerFilePath] string declaringFile = "")
            : base(eventName, declaringFile) { }

        public AsyncEvent(ConcurrentEventRaisePolicy raisePolicy, [CallerMemberName] string eventName = "", [CallerFilePath] string declaringFile = "") : base(eventName, declaringFile)
        {
            ConcurrentRaisePolicy = raisePolicy;
        }

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void RaiseDirectHandlers(object args)
        {
            base.RaiseDirectHandlers(args);
            if (args is TArg a) FullEvent?.Invoke(a);
        }

        public override bool HasDirectHandler() => FullEvent != null || base.HasDirectHandler();

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AsyncEvent<TArg> Handle(Func<TArg, Task> handler,
            [CallerFilePath] string callerFile = null, [CallerLineNumber] int line = 0)
        {
            return DoHandleOn<TArg, AsyncEvent<TArg>>(null, handler, null, callerFile, line, index: null);
        }

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AsyncEvent<TArg> Handle(Action<TArg> handler,
            [CallerFilePath] string callerFile = null, [CallerLineNumber] int line = 0)
        {
            return DoHandleOn<TArg, AsyncEvent<TArg>>(null, null, handler, callerFile, line, index: null);
        }

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AsyncEvent<TArg> HandleOn(BaseThread thread, Action<TArg> handler,
            [CallerFilePath] string callerFile = null, [CallerLineNumber] int line = 0)
        {
            return DoHandleOn<TArg, AsyncEvent<TArg>>(thread, null, handler, callerFile, line, index: null);
        }

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AsyncEvent<TArg> HandleOn(BaseThread thread, Func<TArg, Task> handler,
            [CallerFilePath] string callerFile = null, [CallerLineNumber] int line = 0)
        {
            return DoHandleOn<TArg, AsyncEvent<TArg>>(thread, handler, null, callerFile, line, index: null);
        }

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AsyncEvent<TArg> InsertHandler(Func<TArg, Task> handler, int index,
            [CallerFilePath] string callerFile = null, [CallerLineNumber] int line = 0)
        {
            return DoHandleOn<TArg, AsyncEvent<TArg>>(null, handler, null, callerFile, line, index);
        }

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AsyncEvent<TArg> InsertHandlerOn(BaseThread thread, Func<TArg, Task> handler, int index,
            [CallerFilePath] string callerFile = null, [CallerLineNumber] int line = 0)
        {
            return DoHandleOn<TArg, AsyncEvent<TArg>>(thread, handler, null, callerFile, line, index);
        }

        protected AsyncEvent<TArg> DoHandleOn(BaseThread thread, Func<TArg, Task> handlerTask, Action<TArg> handlerAction,
            string callerFile, int line)
        {
            return DoHandleOn<TArg, AsyncEvent<TArg>>(thread, handlerTask, handlerAction, callerFile, line, index: null);
        }

        [DebuggerStepThrough]
        public IAsyncEventHandler CreateHandler(Func<TArg, Task> handler, BaseThread onThread = null,
            [CallerFilePath] string callerFile = null, [CallerLineNumber] int line = 0)
        {
            if (handler is null) throw new ArgumentNullException(nameof(handler));

            var handlers = GetOrCreateHandlers();
            lock (handlers)
            {
                var result = handlers.FirstOrDefault(x => x.Handler == (Delegate)handler) as AsyncEventTaskHandler<TArg>;
                if (result == null)
                {
                    result = new AsyncEventTaskHandler<TArg>
                    {
                        Action = handler,
                        Event = this,
                        Thread = onThread,
                        Caller = UIRuntime.IsDebuggerAttached ? $"{callerFile}:{line}" : string.Empty
                    };

                    handlers.Add(result);
                }

                return result;
            }
        }

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        Task RaiseHandler(AsyncEventHandler handler, TArg arg)
        {
            if (handler is AsyncEventActionHandler h) return h.Raise();
            else if (handler is AsyncEventTaskHandler t) return t.Raise();
            else if (handler is AsyncEventActionHandler<TArg> ha) return ha.Raise(arg);
            else if (handler is AsyncEventTaskHandler<TArg> aa) return aa.Raise(arg);
            else return Task.CompletedTask;
        }

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task Raise(TArg arg) => Raise(arg, inParallel: false);

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task Raise(TArg arg, bool inParallel)
        {
            RaiseDirectHandlers(arg);
            return Raise(h => RaiseHandler(h, arg), inParallel);
        }

        /// <summary>Will run its event handlers on the UI or ThreadPool as specified.</summary>
        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task RaiseOn(BaseThread thread, TArg arg) => thread.Run(() => Raise(arg));

        /// <summary>Will run its event handlers on the UI or ThreadPool as specified.</summary>
        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SignalRaiseOn(BaseThread thread, TArg arg) => thread.RunAction(() => Raise(arg).GetAwaiter());

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AsyncEvent<TArg> RemoveHandler(Action<TArg> handler) => this.DoRemoveHandler(handler);

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AsyncEvent<TArg> RemoveHandler(Func<TArg, Task> handler) => this.DoRemoveHandler(handler);

        public override void ClearHandlers()
        {
            FullEvent = null;
            base.ClearHandlers();
        }
    }
}