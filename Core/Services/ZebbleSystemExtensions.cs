namespace System
{
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Linq;
    using Zebble;
    using Olive;
    using System.IO;

    public static class ZebbleSystemExtensions
    {
        /// <summary>
        /// Runs this task in parallel without waiting for it.
        /// In case of an exception, in development mode, 
        /// it will be either thrown on the UI thread to stop the application, or 
        /// </summary>
        public static void RunInParallel(this Task task, [CallerMemberName] string caller = "",
            [CallerLineNumber] int line = -1,
            [CallerFilePath] string callingFile = "")
        {
            if (task is null) return;

            task.ContinueWith(x =>
            {
                if (!x.IsFaulted) return;

                var ex = x.Exception.InnerException;
                if (ex.Message.StartsWith("No installed components were detected.")) return;

                var file = callingFile.Split(Path.DirectorySeparatorChar).Trim().Reverse().Take(3).Reverse().ToString("/");

                var error = new Exception("An error occurred when running an async method in parallel ➤ " +
                     file + ":" + line + " → " + caller + "()", ex);

                Log.For(typeof(ZebbleSystemExtensions)).Error(error);
                if (UIRuntime.IsDevMode) Diagnostics.Debugger.Break();
            }).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void HandleOnUI<TArg>(this AsyncEvent<TArg> @event, Action<TArg> handler)
        {
            @event.FullEvent += x => Thread.UI.Run(() => handler(x));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void HandleOnUI(this AbstractAsyncEvent @event, Action handler)
        {
            @event.Event += () => Thread.UI.Run(handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void HandleChangedBySourceOnUI<T>(this TwoWayBindable<T> @event, Action handler)
        {
            @event.ChangedBySource += () => Thread.UI.RunAction(handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void HandleChangedBySourceOnUI<T>(this TwoWayBindable<T> @event, Action<T> handler)
        {
            @event.ChangedBySource += () => Thread.UI.RunAction(() => handler(@event.Value));
        }

    }
}