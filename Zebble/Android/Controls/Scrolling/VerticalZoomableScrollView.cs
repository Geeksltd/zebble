namespace Zebble.AndroidOS
{
    using System;
    using System.Threading.Tasks;
    using System.Timers;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using Zebble.Device;
    using Olive;

    public class VerticalZoomableScrollView : ScrollView, IScrollView, IZoomableScrollView, IPartialPagingScrollView, IPullToRefreshScrollView
    {
        DateTime LatestChange;
        bool IsDisposed;

        public ZoomView ZoomContainer { get; set; }
        public Zebble.ScrollView View { get; set; }
        public AndroidScrollContainer Container { get; set; }

        public VerticalZoomableScrollView(Zebble.ScrollView view) : base(Renderer.Context)
        {
            View = view;

            ZoomContainer = new ZoomView(View);

            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            Container = new AndroidScrollContainer(view)
            {
                LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
            };

            Configure();

            HandleEvents();

            Post(() => new Java.Lang.Runnable(() => ScrollTo(Scale.ToDevice(View.ScrollX), Scale.ToDevice(View.ScrollY))).Run());
        }

        [Preserve]
        public VerticalZoomableScrollView(IntPtr ptr, JniHandleOwnership handle) : base(ptr, handle) { }

        public AndroidScrollContainer GetContainer() => Container;

        public void ScrollToPosition(float xOffset, float yOffset, bool animate = false)
        {
            var offset = PullToRefresh.HandleScrollPosition(this, xOffset, yOffset, animate);

            if (animate) SmoothScrollTo((int)offset.Width, (int)offset.Height);
            else ScrollTo((int)offset.Width, (int)offset.Height);
        }

        #region Partial Paging

        public int PartialPagingInterval { get; set; } = 50;
        public int MaxScrollSpeed { get; set; } = 2000;
        public bool PartialPagingEnabled { get; set; }
        public float PartialPagingSize { get; set; }
        public Timer PartialPagingTimer { get; set; }

        public void ConfigurePartialPaging() => PartialPaging.Configure(this);

        public void OnPartialPagingEnded()
        {
            if (View.EnableZooming && !ZoomContainer.GetZoom().AlmostEquals(1)) return;
            PartialPaging.OnEnd(this);
        }

        #endregion

        #region Pull to refresh

        public Canvas Refresher { get => View.Refresh.Indicator; set { } }

        public FrameLayout RefreshContent { get; set; }
        Timer IPartialPagingScrollView.PartialPagingTimer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public async void CreatePullToRefresh() => await PullToRefresh.CreatePullToRefresh(this);

        public async void InvokeRefresh() => await PullToRefresh.InvokeRefresh(this);

        #endregion

        void Configure()
        {
            SetBackgroundColor(Colors.Transparent.Render());

            PartialPagingEnabled = View.PartialPagingEnabled || View.PagingEnabled;
            PartialPagingSize = View.PartialPagingSize;
            if (Device.OS.IsAtLeast(Android.OS.BuildVersionCodes.Lollipop)) NestedScrollingEnabled = true;
            SmoothScrollingEnabled = true;
            DescendantFocusability = DescendantFocusability.BeforeDescendants;
            FillViewport = false;

            VerticalScrollBarEnabled = View.ShowVerticalScrollBars;

            ZoomContainer.AddView(Container);

            AddView(ZoomContainer);

            if (PartialPagingEnabled) ConfigurePartialPaging();
        }

        void HandleEvents()
        {
            View.ApiScrolledTo.HandleOnUI(OnApiScrolledTo);
            View.ScrollEnded.HandleOnUI(OnScrollEnded);
            View.ContentSizeChanged.HandleOnUI(() => Scrolling.SyncContainerSize(this));
            View.ApiZoomChanged.HandleOnUI(() => ZoomContainer?.OnApiZoomChanged());
        }

        void OnScrollEnded()
        {
            if (View.Refresh.Enabled && ScrollX == 0)
                InvokeRefresh();
        }

        void OnApiScrolledTo(Zebble.ScrollView.ApiMoveToEventArgs args)
        {
            ScrollToPosition(Scale.ToDevice(args.XOffset), Scale.ToDevice(args.YOffset), args.Animate);
        }

        public override bool OnTouchEvent(MotionEvent eventArgs)
        {
            if (!View.EnableScrolling) return false;
            return base.OnTouchEvent(eventArgs);
        }

        public override bool DispatchTouchEvent(MotionEvent eventArgs)
        {
            Parent?.RequestDisallowInterceptTouchEvent(disallowIntercept: true);
            return base.DispatchTouchEvent(eventArgs);
        }

        public override bool OnInterceptTouchEvent(MotionEvent ev)
        {
            if (!View.EnableScrolling) return false;
            return base.OnInterceptTouchEvent(ev);
        }

        protected override async void OnScrollChanged(int horizontal, int vertical, int oldHorizontal, int oldVertical)
        {
            base.OnScrollChanged(horizontal, vertical, oldHorizontal, oldVertical);

            var timestamp = LatestChange = DateTime.UtcNow;

            await Task.Delay(50).ContinueWith(v =>
            {
                if (LatestChange == timestamp) View.ScrollEnded.SignalRaiseOn(Thread.Pool);
            });
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
                    catch { /* No loging is needed */ }
                }

                RefreshContent?.Dispose();
                Container?.Dispose();
                ZoomContainer?.Dispose();
                PartialPagingTimer?.Stop();
                PartialPagingTimer?.Dispose();
                View.ApiScrolledTo.RemoveHandler(OnApiScrolledTo);
                View.ScrollEnded.Event -= OnScrollEnded;

                RefreshContent = null;
                Container = null;
                ZoomContainer = null;
                PartialPagingTimer = null;
                View = null;
            }

            base.Dispose(disposing);
        }
    }
}