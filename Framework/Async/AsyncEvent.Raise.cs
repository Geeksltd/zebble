namespace Zebble
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Olive;

    partial class AbstractAsyncEvent
    {
        public event Action Event;
        bool IsRaising;

        /// <summary>
        /// Determines how concurrent attempts to raise an event should be handled.
        /// </summary>
        protected ConcurrentEventRaisePolicy ConcurrentRaisePolicy = ConcurrentEventRaisePolicy.Parallel;

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void RaiseDirectHandlers(object args) => Event?.Invoke();

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Task Raise(Func<AsyncEventHandler, Task> raiser, bool inParallel)
        {
            RaiseDirectHandlers(null);

            var handlers = this.handlers?.ToArray();
            if (handlers.None()) return Task.CompletedTask;

            if (UIRuntime.IsDebuggerAttached)
            {
                return Raise(handlers, raiser, inParallel).WithTimeout(
                        Timeout ?? 10.Seconds(),
                        timeoutAction: () =>
                        {
                            Log.For(this).Error($"Raising the event {DeclaringType}.{EventName} timed out.{Environment.NewLine}Handlers:" +
                                handlers.Select(x => x.Caller).ToLinesString());
                        });
            }

            return Raise(handlers, raiser, inParallel);
        }

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        Task Raise(AsyncEventHandler[] handlers, Func<AsyncEventHandler, Task> raiser, bool inParallel)
        {
            switch (ConcurrentRaisePolicy)
            {
                case ConcurrentEventRaisePolicy.Parallel: return RaiseOnce(handlers, raiser, inParallel);
                case ConcurrentEventRaisePolicy.Ignore: return RaiseWithIgnorePolicy(handlers, raiser, inParallel);
                case ConcurrentEventRaisePolicy.Queue: return RaiseWithQueuePolicy(handlers, raiser, inParallel);
                default: throw new NotImplementedException();
            }
        }

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        async Task RaiseWithIgnorePolicy(AsyncEventHandler[] handlers, Func<AsyncEventHandler, Task> raiser, bool inParallel)
        {
            if (IsRaising) return;

            IsRaising = true;
            try
            {
                await RaiseOnce(handlers, raiser, inParallel)
                  .WithTimeout(5.Seconds(), timeoutAction: () =>
                  {
                      IsRaising = false;
                      Log.For(this).Warning("Raising this event didn't complete within 5 seconds: " + this);
                  });
            }
            finally { IsRaising = false; }
        }

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        async Task RaiseWithQueuePolicy(AsyncEventHandler[] handlers, Func<AsyncEventHandler, Task> raiser, bool inParallel)
        {
            await RaiseOnce(handlers, raiser, inParallel);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        async Task RaiseOnce(AsyncEventHandler[] handlers, Func<AsyncEventHandler, Task> raiser, bool inParallel)
        {
            try
            {
                if (inParallel)
                {
                    await Task.WhenAll(handlers.Select(x =>
                    {
                        if (!IsDisposing) return raiser(x);
                        else return Task.CompletedTask;
                    }));
                }
                else foreach (var h in handlers)
                        if (!IsDisposing) await raiser(h);
            }
            catch (Exception ex)
            {
                Log.For(this).Error(ex, "Raising an event failed: " + this);

                throw new AsyncEventHandlingException($"Raising the event {DeclaringType}.{EventName} failed.", ex);
            }
        }
    }
}