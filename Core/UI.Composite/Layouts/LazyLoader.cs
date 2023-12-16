namespace Zebble
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Olive;

    public class LazyLoader : Stack
    {
        readonly ConcurrentList<View> ChildrenToLoad = new();
        readonly AsyncLock ChildrenSyncLock = new();
        public readonly AsyncEvent StartingContentLoad = new();
        bool IsFirstTime = true;
        protected Func<Task> Loaded;

        public LazyLoader() : base(RepeatDirection.Vertical) { }

        public override async Task OnInitializing()
        {
            await base.OnInitializing();

            await WhenShown(Loaded += async () =>
            {
                using (await ChildrenSyncLock.Lock())
                    await LoadActualContent();
            });
        }

        public override async Task<TView> Add<TView>(TView child, bool awaitNative = false)
        {
            if (IsShown || !IsFirstTime) return await base.Add(child, awaitNative);

            using (await ChildrenSyncLock.Lock()) ChildrenToLoad.Add(child);

            return child;
        }

        async Task LoadActualContent()
        {
            if (ChildrenToLoad.None()) return;

            if (!IsFirstTime) return;// Invoked again in a cached page.
            else IsFirstTime = false;

            await StartingContentLoad.Raise();
            await Dialogs.Current.ShowWaiting(false);

            foreach (var c in ChildrenToLoad) await base.Add(c);

            var first = ChildrenToLoad.FirstOrDefault();
            if (first != null) await first.WhenShown(() => Dialogs.Current.HideWaiting());
            else await Dialogs.Current.HideWaiting();

            ChildrenToLoad.Clear();
        }
    }
}