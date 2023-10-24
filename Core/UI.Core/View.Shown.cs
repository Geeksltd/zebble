namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Olive;

    partial class View
    {
        readonly object ShownSyncLock = new();

        /// <summary>
        /// This is fired every time the view is shown.
        /// If the view is within a cached page, then every time it's shown again, this event is fired.
        /// </summary>
        internal void RaiseShown()
        {
            if (IsDisposing) return;
            if (IsShown) return;

            lock (ShownSyncLock)
            {
                IsShown = true;
                InitializeAutoFlash();
                Shown.SignalRaiseOn(Thread.Pool);
            }
        }

        /// <summary>
        /// Runs a specified action when this view is shown. If it's already shown, then it runs it immediately.
        /// </summary>
        public async Task WhenShown(Func<Task> action,
            [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            if (IsShown)
            {
                await action();
                return;
            }

            Task raiseTask;

            lock (ShownSyncLock)
            {
                if (IsShown) raiseTask = action();
                else
                {
                    Shown.Handle(action, callerFile, callerLine);
                    raiseTask = Task.CompletedTask;
                }
            }

            await raiseTask;
        }

        /// <summary>
        /// Runs a specified action when this view is shown. If it's already shown, then it runs it immediately.
        /// </summary>
        public async Task WhenShown(Action action)
        {
            if (action is null) return;
            await WhenShown(() => { action(); return Task.CompletedTask; });
        }

        /// <summary>
        /// Runs the specified action now, and also registers it to be registered when the page is being loaded for revisiting.
        /// This method is best called in the OnInitializing or OnInitialized event of a module that will be hosted on a cacheable page.
        /// </summary>
        public Task NowAndOnPageRevisiting(Func<Task> action,
        [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            return NowAndOnPageRevisiting(x => action(), callerFile, callerLine);
        }

        /// <summary>
        /// Runs the specified action now, and also registers it to be registered when the page is being loaded for revisiting.
        /// This method is best called in the OnInitializing or OnInitialized event of a module that will be hosted on a cacheable page.
        /// </summary>
        public async Task NowAndOnPageRevisiting(Func<IDictionary<string, object>, Task> action,
        [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            await action(Nav.CurrentPage?.NavParams ?? new Dictionary<string, object>());

            await WhenShown(() =>
            {
                var page = Page;
                if (page is null) return Task.CompletedTask;

                if (!CacheViewAttribute.IsCacheable(page))
                    Log.For(this).Warning("Page " + page.GetType().Name + " is not cacheable. Revisiting event will never be fired.");

                page.OnRevisiting.Handle(x => action(x.NavParams ?? new Dictionary<string, object>()), callerFile, callerLine);

                return Task.CompletedTask;
            }, callerFile, callerLine);
        }

        /// <summary>
        /// Runs the specified action now, and also registers it to be registered when the page is being loaded for revisiting.
        /// This method is best called in the OnInitializing or OnInitialized event of a module that will be hosted on a cacheable page.
        /// </summary>
        public Task NowAndOnPageRevisiting(Action action,
        [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            return NowAndOnPageRevisiting(() => { action(); return Task.CompletedTask; }, callerFile, callerLine);
        }

        /// <summary>
        /// Runs the specified action when this page's containing page is shown, and every time it's revisited.
        /// This method is best called in the OnInitializing or OnInitialized event of a module that will be hosted on a cacheable page.
        /// </summary>
        public Task WhenShownOrPageRevisited(Action action,
        [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            return WhenShownOrPageRevisited(() => { action(); return Task.CompletedTask; }, callerFile, callerLine);
        }

        /// <summary>
        /// Runs the specified action when this page's containing page is shown, and every time it's revisited.
        /// This method is best called in the OnInitializing or OnInitialized event of a module that will be hosted on a cacheable page.
        /// </summary>
        public Task WhenShownOrPageRevisited(Func<Task> action,
        [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            return WhenShownOrPageRevisited(x => action(), callerFile, callerLine);
        }

        /// <summary>
        /// Runs the specified action when this page's containing page is shown, and every time it's revisited.
        /// This method is best called in the OnInitializing or OnInitialized event of a module that will be hosted on a cacheable page.
        /// </summary>
        public async Task WhenShownOrPageRevisited(Func<IDictionary<string, object>, Task> action,
        [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            await WhenShown(async () =>
            {
                await action(Nav.CurrentPage?.NavParams ?? new Dictionary<string, object>());
                var page = Page;
                if (page is null) return;

                if (!CacheViewAttribute.IsCacheable(page))
                    Log.For(this).Warning("Page " + page.GetType().Name + " is not cacheable. Revisited event will never be fired.");

                page.OnRevisited.Handle(x => action(x.NavParams ?? new Dictionary<string, object>()), callerFile, callerLine);
            }, callerFile, callerLine);
        }

        internal string GetBackgroundImageKey()
        {
            return ActualWidth + BackgroundImagePath + BackgroundImageAlignment
                + ActualHeight + BackgroundImageStretch + Padding + BorderRadius;
        }
    }
}