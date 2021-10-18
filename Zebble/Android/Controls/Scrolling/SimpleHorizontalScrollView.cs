namespace Zebble.AndroidOS
{
    using System;
    using System.Threading.Tasks;
    using System.Timers;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using Zebble.Device;
    using zbl = Zebble;
    using Olive;

    public class SimpleHorizontalScrollView : HorizontalScrollView, IScrollView, IPartialPagingScrollView, IPullToRefreshScrollView
    {
        bool IsDisposed;
        DateTime LatestChange;
        public zbl.ScrollView View { get; set; }
        public AndroidScrollContainer Container { get; set; }

        public SimpleHorizontalScrollView(zbl.ScrollView view) : base(Renderer.Context)
        {
            View = view;

            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            Container = new AndroidScrollContainer(view)
            {
                LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
            };

            Configure();
            HandleEvents();
            Post(() => new Java.Lang.Runnable(() => { ScrollTo(Scale.ToDevice(View.ScrollX), Scale.ToDevice(View.ScrollY)); }).Run());
        }

        [Preserve]
        public SimpleHorizontalScrollView(IntPtr ptr, JniHandleOwnership handle) : base(ptr, handle) { }

        public AndroidScrollContainer GetContainer() => Container;

        public void ScrollToPosition(float xOffset, float yOffset, bool animate)
        {
            var offset = PullToRefresh.HandleScrollPosition(this, xOffset, yOffset, animate);

            if (animate) SmoothScrollTo((int)offset.Width, (int)offset.Height);
            else ScrollTo((int)offset.Width, (int)offset.Height);
        }

        protected override void OnSizeChanged(int width, int height, int oldw, int oldh)
        {
            base.OnSizeChanged(width, height, oldw, oldh);
            Scrolling.UpdateSize(this, width, height);
        }

        #region Pull to refresh
        public Canvas Refresher { get => View.Refresh.Indicator; set { } }

        public FrameLayout RefreshContent { get; set; }

        public async void CreatePullToRefresh() => await PullToRefresh.CreatePullToRefresh(this);

        public async void InvokeRefresh() => await PullToRefresh.InvokeRefresh(this);
        #endregion

        #region Partial Paging

        public int PartialPagingInterval { get; set; } = 50;
        public int MaxScrollSpeed { get; set; } = 2000;
        public bool PartialPagingEnabled { get; set; }
        public float PartialPagingSize { get; set; }
        public Timer PartialPagingTimer { get; set; }

        public void ConfigurePartialPaging() => PartialPaging.Configure(this);

        public void OnPartialPagingEnded() => PartialPaging.OnEnd(this);

        #endregion

        void Configure()
        {
            SetBackgroundColor(Colors.Transparent.Render());

            PartialPagingEnabled = View.PartialPagingEnabled || View.PagingEnabled;
            PartialPagingSize = View.PartialPagingSize;
            SmoothScrollingEnabled = true;
            if (Device.OS.IsAtLeast(Android.OS.BuildVersionCodes.Lollipop)) NestedScrollingEnabled = true;
            DescendantFocusability = DescendantFocusability.BeforeDescendants;
            FillViewport = false;
            HorizontalScrollBarEnabled = View.ShowHorizontalScrollBars;

            AddView(Container);

            if (View.PartialPagingEnabled) ConfigurePartialPaging();
        }

        void OnApiZoomChanged()
        {
            Log.For(this).Error("ApiZoomChanged is not implemented for Android yet !");
        }

        void HandleEvents()
        {
            View.ApiScrolledTo.HandleOnUI(OnApiScrolledTo);
            View.ScrollEnded.HandleOnUI(OnScrollEnded);
            View.ContentSizeChanged.HandleOnUI(() => Scrolling.SyncContainerSize(this));
            View.ApiZoomChanged.HandleOnUI(OnApiZoomChanged);
        }

        void OnScrollEnded()
        {
            if (View.Refresh.Enabled && ScrollX == 0)
                InvokeRefresh();
        }

        void OnApiScrolledTo(zbl.ScrollView.ApiMoveToEventArgs args)
        {
            LatestChange = DateTime.UtcNow;
            ScrollToPosition(Scale.ToDevice(args.XOffset), Scale.ToDevice(args.YOffset), args.Animate);
        }

        public override bool OnTouchEvent(MotionEvent eventArgs)
        {
            if (!View.EnableScrolling) return false;
            return base.OnTouchEvent(eventArgs);
        }

        public override bool OnInterceptTouchEvent(MotionEvent ev)
        {
            if (!View.EnableScrolling) return false;
            return base.OnInterceptTouchEvent(ev);
        }

        protected override async void OnScrollChanged(int horizontal, int vertical, int oldHorizontal, int oldVertical)
        {
            base.OnScrollChanged(horizontal, vertical, oldHorizontal, oldVertical);

            var left = Scale.ToZebble(ScrollX);
            var top = Scale.ToZebble(ScrollY);

            ProcessScrollChanged(left, top);

            var timestamp = LatestChange = DateTime.UtcNow;

            await Task.Delay(50).ContinueWith(x =>
            {
                if (LatestChange == timestamp) View.ScrollEnded.SignalRaiseOn(Thread.Pool);
            });
        }

        void ProcessScrollChanged(int scrollX, int scrollY)
        {
            View.SetUserScrolledX(scrollX);
            View.SetUserScrolledY(scrollY);

            if (!View.PartialPagingEnabled) return;

            if (PartialPagingTimer.Enabled) PartialPagingTimer.Stop();
            PartialPagingTimer.Start();
        }

        public override void Fling(int velocityX)
        {
            var halfpage = PartialPagingSize / 2;
            if (Math.Abs(velocityX) > MaxScrollSpeed)
            {
                if (velocityX > 0 && Math.Abs(View.ScrollX % PartialPagingSize) < halfpage)
                {
                    View.ScrollX += halfpage;
                }
                else if (velocityX < 0 && Math.Abs(View.ScrollX % PartialPagingSize) > halfpage)
                {
                    View.ScrollX -= halfpage;
                }
            }

            Thread.UI.RunAction(() => OnPartialPagingEnded());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                IsDisposed = true;
                while (ChildCount > 0)
                {
                    try
                    {
                        var child = GetChildAt(0);
                        RemoveViewAt(0);
                        child.Dispose();
                    }
                    catch { /*No logging is needed. Continue.*/ }
                }

                RefreshContent?.Dispose();
                Container?.Dispose();
                PartialPagingTimer?.Stop();
                PartialPagingTimer?.Dispose();
                View.ApiScrolledTo.RemoveHandler(OnApiScrolledTo);
                View.ScrollEnded.Event -= OnScrollEnded;

                RefreshContent = null;
                Container = null;
                PartialPagingTimer = null;
                View = null;
            }

            base.Dispose(disposing);
        }
    }
}