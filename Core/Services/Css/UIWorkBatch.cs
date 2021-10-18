namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Olive;

    class UIChangeCommand
    {
        public View View; public string Property; public UIChangedEventArgs Change;

        public interface IHandler
        {
            void Apply(string property, UIChangedEventArgs change);
        }
    }

    /// <summary>
    /// Runs a batch of UI creation / change work by suspending native layout during the change process.
    /// </summary>
    public partial class UIWorkBatch
    {
        public static UIWorkBatch Current;
        static object SyncLock = new object();
        List<UIChangeCommand> Pending = new List<UIChangeCommand>();

        public static void RunSync(Action action)
        {
            var mine = TryCreate();

            try { action(); }
            finally { mine?.Release(); }
        }

        [Obsolete("Use RunSync instead.", error: true)]
        public static Task Run(Action action) => Run(() => { action(); return Task.CompletedTask; });

        static UIWorkBatch TryCreate()
        {
            UIWorkBatch me = null;

            lock (SyncLock)
                if (Current is null)
                    me = Current = new UIWorkBatch();

            return me;
        }

        void Release()
        {
            lock (SyncLock) Current = null;
            Flush();
        }

        public static async Task Run(Func<Task> action, bool awaitNative = false)
        {
            var mine = TryCreate();

            try
            {
                if (UIRuntime.IsDevMode)
                    await action().WithTimeout(5.Seconds(), timeoutAction: () => Log.For<UIWorkBatch>().Warning("UIWordBatch didn't complete within 5 seconds!"));
                else await action();
            }
            finally { mine?.Release(); }
        }

        internal void Flush()
        {
            UIChangeCommand[] items;

            lock (SyncLock)
            {
                items = Pending.ToArray();
                Pending.Clear();
            }

            if (items.Length == 0) return;
            Thread.UI.Run(() =>
            {
                foreach (var item in items)
                    item.View?.Renderer?.Apply(item.Property, item.Change);
            });
        }

        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Publish(View view, string property, UIChangedEventArgs change)
        {
            if (!view.IsRendered()) return;

            UIWorkBatch context;
            lock (SyncLock)
            {
                context = Current;

                if (context is null)
                {
                    Thread.UI.Run(() => view.Renderer?.Apply(property, change));
                }
                else
                {
                    context.Pending.RemoveWhere(x => x.Property == property && x.View == view);
                    context.Pending.Add(new UIChangeCommand { View = view, Property = property, Change = change });
                }
            }
        }
    }
}