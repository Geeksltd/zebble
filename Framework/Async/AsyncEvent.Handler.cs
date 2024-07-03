namespace Zebble
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    public interface IAsyncEventHandler
    {
        object Action { get; }
        void RemoveSelf();
        bool IsDisposed();
    }

    public abstract class AsyncEventHandler : IEquatable<AsyncEventHandler>
    {
        internal BaseThread Thread;
        internal string Caller;
        protected bool Disposed;
        public bool IsDisposed() => Disposed;

        internal Delegate Handler => (Delegate)(((IAsyncEventHandler)this).Action);

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task RaiseOnCorrectThread(Func<Task> raiser)
        {
            if (Disposed) return Task.CompletedTask;

            if (raiser is null) return Task.CompletedTask;
            if (Thread is null) return raiser.Invoke();
            else return Thread.Run(raiser);
        }

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task RaiseOnCorrectThread(Action action)
        {
            if (!Disposed)
            {
                if (Thread is null) action?.Invoke();
                else Thread.RunAction(action);
            }

            return Task.CompletedTask;
        }

        public abstract bool Equals(AsyncEventHandler other);

        public abstract void RemoveSelf();

        public virtual void Dispose() => GC.SuppressFinalize(this);
    }

    public abstract class AsyncEventHandler<TActionFunction> : AsyncEventHandler, IAsyncEventHandler
        where TActionFunction : class
    {
        internal TActionFunction Action;
        internal AbstractAsyncEvent Event;
        object IAsyncEventHandler.Action => Action;

        public override bool Equals(AsyncEventHandler other) => Action == (other as AsyncEventHandler<TActionFunction>)?.Action;

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void RemoveSelf()
        {
            if (Disposed) return;
            Disposed = true;

            Event?.RemoveHandler<AsyncEventHandler>(this);
            Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Dispose()
        {
            Disposed = true;
            Event = null;
            Action = null;
            Thread = null;
            Caller = null;
			base.Dispose();
        }
    }

    public class AsyncEventActionHandler : AsyncEventHandler<Action>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Task Raise() => RaiseOnCorrectThread(Action);

        public MethodInfo HandlerMethod => Action?.GetMethodInfo();

        public object HandlerObject => Action?.Target;

        public string HandlerObjectType => Action?.Target?.GetType().FullName;

        public override string ToString() => HandlerMethod?.Name + "() of " + HandlerObject;
    }

    public class AsyncEventTaskHandler : AsyncEventHandler<Func<Task>>
    {
        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Task Raise() => RaiseOnCorrectThread(RaiseIt);

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        Task RaiseIt() => Action?.Invoke() ?? Task.CompletedTask;

        public object HandlerObject => Action?.Target;
        public MethodInfo HandlerMethod => Action?.GetMethodInfo();

        public override string ToString() => HandlerMethod?.Name + "() of " + HandlerObject;
    }

    public class AsyncEventActionHandler<T> : AsyncEventHandler<Action<T>>
    {
        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Task Raise(T arg) => RaiseOnCorrectThread(() => Action?.Invoke(arg));
    }

    public class AsyncEventTaskHandler<T> : AsyncEventHandler<Func<T, Task>>
    {
        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Task Raise(T arg) => RaiseOnCorrectThread(() => Action?.Invoke(arg) ?? Task.CompletedTask);
    }
}