using System;
using System.Threading.Tasks;
using Olive;

namespace Zebble.Mvvm
{
    public class TestRender<TPage> : TestRender where TPage : FullScreen
    {
        TPage Page => ViewModel.The<TPage>();
        bool ShouldFireEvents;

        public TestRender<TPage> Configure(Action<TPage> configure)
        {
            configure?.Invoke(Page);
            return this;
        }

        public TestRender<TPage> FireEvents() => this.Set(x => x.ShouldFireEvents = true);

        public Task Run()
        {
            ViewModel.Page = Page;
            ViewModel.NavAnimationStarted.Set(LocalTime.UtcNow);

            if (ShouldFireEvents)
                Page.NavigationStarted();

            var startedTask = ShouldFireEvents ? Task.Factory.StartNew(Page.NavigationStartedAsync) : Task.CompletedTask;
            var page = (Page)Templates.GetOrCreate(Page);

            Task.Delay(1000 / 60).ContinueWith(async t =>
            {
                await Nav.Go(page, PageTransition.None);

                if (ShouldFireEvents)
                {
                    await startedTask;
                    Page.NavigationCompleted();
                    await Page.NavigationCompletedAsync();
                }
            });

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Allows you to instantly run the app on a specific page (view model) while skipping the page lifecycle.
    /// </summary>
    public class TestRender
    {
        public static TestRender<TPage> Page<TPage>() where TPage : FullScreen
        {
            return new TestRender<TPage>();
        }
    }
}