namespace Zebble
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Olive;

    public partial class UIThread : BaseThread
    {
        internal override bool IsUI => true;

        /// <summary>Schedules a specified action to be done on the UI thread, and returns immediately.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void RunAction(Action action)
        {
            if (action is null) return;

            if (IsRunning()) action();
            else Dispatch(action);
        }

        internal void ThrowException(Exception error) => RunAction(() => { throw error; });

        /// <summary>Schedules a specified action to be done on the UI thread, but allows you to await it before continuing.</summary>
        public override Task Run(Func<Task> task)
        {
            if (task is null) return Task.CompletedTask;
            if (IsRunning()) return task() ?? Task.CompletedTask;

            var source = new TaskCompletionSource<bool>();

            RunAction(async () =>
            {
                try
                {
                    await task();
                    source.SetResult(result: true);
                }
                catch (Exception ex)
                {
                    Log.For(this).Error(ex);
                    source.SetException(ex);
                }
            });

            return source.Task;
        }

        /// <summary>Runs a specified expression run on the UI thread.</summary>
        public override Task<TResult> Run<TResult>(Func<Task<TResult>> task)
        {
            if (task is null) return Task.FromResult(default(TResult));

            if (IsRunning()) return task().OrCompleted();

            var source = new TaskCompletionSource<TResult>();

            RunAction(async () =>
            {
                try
                {
                    var theTask = task().OrCompleted();

                    var result = await theTask.ConfigureAwait(continueOnCapturedContext: false);
                    source.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    Log.For(this).Error(ex);
                    source.SetException(ex);
                }
            });

            return source.Task;
        }
    }
}