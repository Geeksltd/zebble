namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Olive;

    public partial class ThreadPool : BaseThread
    {
        static readonly List<Task> GcShield = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void RunAction(Action action) => RunAction(action, TaskCreationOptions.None);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunAction(Action action, TaskCreationOptions option)
        {
            if (action is null) return;

            if (option == TaskCreationOptions.None)
            {
                if (IsRunning()) action();
                else Task.Factory.StartNew(() => SafeInvoke(action), CancellationToken.None, option, TaskScheduler.Default);
            }
            else
            {
                var task = Task.Factory.StartNew(() => SafeInvoke(action), CancellationToken.None, option, TaskScheduler.Default);
                if (option == TaskCreationOptions.LongRunning)
                {
                    // Prevent thread GC:
                    GcShield.Add(task);
                }
            }
        }

        static void SafeInvoke(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Log.For<ThreadPool>().Error(ex);
            }
        }

        /// <summary>
        /// Ensures a new thread is created to run a specified action, and returns immediately.
        /// This is useful mainly when your code is currently running on a non-UI thread, but you don't want it to wait for running the specified action.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunActionOnNewThread(Action action) => RunAction(action, TaskCreationOptions.LongRunning);

        /// <summary>
        /// Ensures a new thread is created to run a specified task, and returns immediately.
        /// This is useful mainly when your code is currently running on a non-UI thread, but you don't want it to wait for running the specified action.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunOnNewThread(Func<Task> task) => RunActionOnNewThread(() => task().GetAwaiter());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Post(Action action) => Task.Factory.StartNew(action).GetAwaiter();

        /// <summary>Schedules a specified action to run on the background thread.
        /// If it's called on the UI thread it will await, otherwise it will return a completed task to avoid blocking the UI thread.</summary>
        public override Task Run(Func<Task> task)
        {
            if (task is null) return Task.CompletedTask;
            if (IsRunning()) return task() ?? Task.CompletedTask;

            RunAction(async () =>
            {
                try { await task(); }
                catch (Exception ex) { Log.For(this).Error(ex); }
            });

            return Task.CompletedTask;
        }

        public override Task<TResult> Run<TResult>(Func<Task<TResult>> task)
        {
            if (IsRunning()) return task();
            else if (UIRuntime.IsDebuggerAttached)
                Console.WriteLine("UI Thread should not be blocked for a result from the background thread.");
            return task();
        }

#if IOS || ANDROID || WINUI
        public override bool IsRunning() => !Thread.UI.IsRunning();
#else
public override bool IsRunning() => true;
#endif
    }
}