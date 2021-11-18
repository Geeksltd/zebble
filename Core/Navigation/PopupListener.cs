namespace Zebble
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    internal static class PopupListener
    {
        static readonly ConcurrentQueue<IAsyncPopupInstruction> ShowHideInstructions = new();

        static PopupListener() => Task.Factory.StartNew(() => ProcessInstructions());

        static async Task ProcessInstructions()
        {
            while (true)
            {
                if (ShowHideInstructions.TryDequeue(out var nextInstruction))
                    nextInstruction.Process();
                else await Task.Delay(50);
            }
        }

        public static Task<TResult> Show<TResult>(Func<Task<TResult>> showFunc)
        {
            var item = new AsyncPopupInstruction<TResult> { Func = showFunc, CompletionSource = new() };
            ShowHideInstructions.Enqueue(item);

            return item.CompletionSource.Task;
        }

        public static Task Hide(Func<Task<bool>> hideFunc)
        {
            var item = new AsyncPopupInstruction<bool> { Func = hideFunc, CompletionSource = new() };
            ShowHideInstructions.Enqueue(item);

            return item.CompletionSource.Task;
        }
    }

    internal interface IAsyncPopupInstruction
    {
        void Process();
    };

    internal class AsyncPopupInstruction<TResult> : IAsyncPopupInstruction
    {
        public Func<Task<TResult>> Func { get; set; }

        public TaskCompletionSource<TResult> CompletionSource { get; set; }

        public void Process()
        {
            Thread.UI.Run(async () =>
            {
                var result = await Func.Invoke();
                CompletionSource.SetResult(result);
            });
        }
    }
}
