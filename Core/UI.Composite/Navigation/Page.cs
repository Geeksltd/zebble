namespace Zebble
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Olive;

    public abstract partial class Page : Canvas
    {
        public IDictionary<string, object> NavParams;
        public PageTransition Transition;

        protected Page() => this.Size(Length.AutoStrategy.Container);

        public readonly AsyncEvent PulledToRefresh = new();

        /// <summary>
        /// Will be called when redirecting to a previously loaded cached page.
        /// This can be used in order to update the page partially.
        /// </summary>
        public readonly AsyncEvent<RevisitingEventArgs> OnRevisiting = new();

        /// <summary>
        /// Will be called when redirecting to a previously loaded cached page finished.
        /// </summary>
        public readonly AsyncEvent<RevisitingEventArgs> OnRevisited = new();

        /// <summary>
        /// Will be called when redirecting away from this page.
        /// </summary>
        public readonly AsyncEvent<NavigationEventArgs> OnExiting = new();

        /// <summary>
        /// Will be called when after redirected away from this page.
        /// </summary>
        public readonly AsyncEvent<NavigationEventArgs> OnExited = new();

        public bool EnablePullToRefresh { get; set; }

        protected override bool ShouldDisposeWhenRemoved() => false; // It will be disposed in Nav if not cached.

        public virtual Task NavigateTo(Page page)
        {
            return new Nav.Transitor(Root, this, page, page.Transition).Run();
        }

        public override async Task OnInitialized()
        {
            await base.OnInitialized();

            if (EnablePullToRefresh)
            {
                var scroller = AllDescendents().OfType<ScrollView>().FirstOrDefault();
                if (scroller is null)
                {
                    Log.For(this).Error("Failed to enable pull to refresh as no ScrollView was found on the page.");
                    return;
                }

                scroller.Refresh.Enabled = true;
                scroller.Refresh.Requested.Handle(PulledToRefresh.Raise);
            }
        }
    }
}