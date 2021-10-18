namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Olive;

    public abstract partial class BaseThread
    {
        List<Func<Task>> NextFrameTasks = new List<Func<Task>>();
        Task NextFrameSignal = Task.CompletedTask;
        object NextFrameSyncLock = new object();

        static TimeSpan FrameDelay = TimeSpan.FromMilliseconds(1000 / 60.0);

        public void OnNextFrame(Action action)
        {
            if (action != null) OnNextFrame(() => Task.Run(action));
        }

        public void OnNextFrame(Func<Task> task)
        {
            if (task is null) return;

            lock (NextFrameSyncLock)
            {
                if (NextFrameSignal.IsCompleted)
                    NextFrameSignal = Task.Delay(FrameDelay).ContinueWith(x => DispatchNextFrameActions());

                if (NextFrameTasks.Lacks(task)) NextFrameTasks.Add(task);
            }
        }

        void DispatchNextFrameActions()
        {
            List<Func<Task>> toRun;

            lock (NextFrameSyncLock)
            {
                toRun = NextFrameTasks.Clone();
                NextFrameTasks.Clear();
            }

            if (toRun.None()) return;

            Run(async () =>
            {
                foreach (var t in toRun) await t();
            });
        }

        public abstract Task<TResult> Run<TResult>(Func<Task<TResult>> task);

        internal virtual bool IsUI => false;

        [DebuggerStepThrough]
        public abstract void RunAction(Action action);

        /// <summary>Schedules a specified action to be done on this thread, but allows you to await it before continuing.</summary>
        public Task Run(Action action)
        {
            if (action is null) return Task.CompletedTask;

            if (IsRunning())
            {
                action();
                return Task.CompletedTask;
            }

            var source = new TaskCompletionSource<bool>();

            RunAction(() =>
            {
                try
                {
                    action();
                    source.TrySetResult(result: true);
                }
                catch (Exception ex)
                {
                    Log.For(this).Error(ex);
                    source.SetException(ex);
                }
            });

            return source.Task;
        }

        public abstract bool IsRunning();

        /// <summary>Schedules a specified action to be done on this thread, but allows you to await it before continuing.</summary>
        public abstract Task Run(Func<Task> task);

        /// <summary>Runs a specified expression run on this thread but returns the result on the calling context.</summary>
        public T Run<T>(Func<T> expression)
        {
            if (expression is null) return default(T);

            if (IsRunning()) return expression();
            else
            {
                var source = new TaskCompletionSource<T>();

                RunAction(() =>
                {
                    try { source.TrySetResult(result: expression()); }
                    catch (Exception ex)
                    {
                        Log.For(this).Error(ex);
                        source.SetException(ex);
                    }
                });

                return source.Task.AwaitResultWithoutContext();
            }
        }

        public abstract void Post(Action action);
    }
}