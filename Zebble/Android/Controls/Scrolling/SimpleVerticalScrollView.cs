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

    public class SimpleVerticalScrollView : ScrollView, IScrollView, IPartialPagingScrollView, IPullToRefreshScrollView
    {
        float AutoScrollX, AutoScrollY;
        bool IsDisposed, IsFirstDraw = true;
        DateTime LatestChange;

        public zbl.ScrollView View { get; set; }
        public AndroidScrollContainer Container { get; set; }

        public SimpleVerticalScrollView(zbl.ScrollView view) : base(Renderer.Context)
        {
            View = view;

            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            Container = new AndroidScrollContainer(view)
            {
                LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
            };

            Configure();

            HandleEvents();

            if (View.ScrollX != 0 || View.ScrollY != 0)
                OnApiScrolledTo(new zbl.ScrollView.ApiMoveToEventArgs { XOffset = View.ScrollX, YOffset = View.ScrollY });
        }

        [Preserve]
        public SimpleVerticalScrollView(IntPtr ptr, JniHandleOwnership handle) : base(ptr, handle) { }

        public AndroidScrollContainer GetContainer() => Container;

        public void ScrollToPosition(float xOffset, float yOffset, bool animate)
        {
            AutoScrollX = xOffset;
            AutoScrollY = yOffset;

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

            PartialPagingSize = View.PartialPagingSize;
            if (OS.IsAtLeast(Android.OS.BuildVersionCodes.Lollipop)) NestedScrollingEnabled = true;
            SmoothScrollingEnabled = true;
            DescendantFocusability = DescendantFocusability.BeforeDescendants;
            FillViewport = false;
            VerticalScrollBarEnabled = View.ShowVerticalScrollBars;

            AddView(Container);

            if (View.PartialPagingEnabled) ConfigurePartialPaging();
        }

        Task OnApiZoomChanged()
        {
            Log.For(this).Error("ApiZoomChanged is not implemented for Android yet !");
            return Task.CompletedTask;
        }

        void HandleEvents()
        {
            View.ApiScrolledTo.HandleOnUI(OnApiScrolledTo);
            View.ScrollEnded.HandleOnUI(OnScrollEnded);
            View.ContentSizeChanged.HandleOnUI(() => Scrolling.SyncContainerSize(this));
        }

        void OnScrollEnded()
        {
            if (View.Refresh.Enabled && ScrollY == 0)
                InvokeRefresh();
        }

        void OnApiScrolledTo(zbl.ScrollView.ApiMoveToEventArgs args)
        {
            LatestChange = DateTime.UtcNow;
            ScrollToPosition(Scale.ToDevice(args.XOffset), Scale.ToDevice(args.YOffset), args.Animate);
        }

        public override bool OnTouchEvent(MotionEvent eventArgs)
        {
            if (IsDisposed) return false;
            if (View?.EnableScrolling != true) return false;
            return base.OnTouchEvent(eventArgs);
        }

        public override bool DispatchTouchEvent(MotionEvent eventArgs)
        {
            Parent?.RequestDisallowInterceptTouchEvent(disallowIntercept: true);
            return base.DispatchTouchEvent(eventArgs);
        }

        public override bool OnInterceptTouchEvent(MotionEvent ev)
        {
            if (IsDisposed) return false;
            if (View?.EnableScrolling != true) return false;
            return base.OnInterceptTouchEvent(ev);
        }

        protected override async void OnScrollChanged(int horizontal, int vertical, int oldHorizontal, int oldVertical)
        {
            base.OnScrollChanged(horizontal, vertical, oldHorizontal, oldVertical);

            var myVersion = LatestChange = DateTime.UtcNow;

            var left = Scale.ToZebble(ScrollX);
            var top = Scale.ToZebble(ScrollY);

            await Thread.Pool.Run(async () =>
             {
                 if (!IsDisposed) await ProcessScrollChanged(left, top, myVersion);
             });
        }

        async Task ProcessScrollChanged(int scrollX, int scrollY, DateTime timestamp)
        {
            View.SetUserScrolledX(scrollX);
            View.SetUserScrolledY(scrollY);

            await Task.Delay(100);

            if (LatestChange != timestamp) return;

            if (View.PartialPagingEnabled || View.PagingEnabled)
            {
                var index = (float)Math.Round(scrollY / View.PartialPagingSize);

                var stepScrollY = Scale.ToDevice(View.PartialPagingSize * index);
                if (stepScrollY != scrollY)
                    Thread.UI.Post(() => ScrollToPosition(scrollX, stepScrollY, animate: true));
            }

            View.ScrollEnded.SignalRaiseOn(Thread.Pool);
        }

        public override void Draw(Android.Graphics.Canvas canvas)
        {
            try
            {
                base.Draw(canvas);
            }
            catch
            {
                // Ignore.
                return;
            }

            if (!IsFirstDraw) return;
            else IsFirstDraw = false;

            Thread.UI.Post(() =>
            {
                if (IsDisposed) return;
                if (!canvas.IsAlive()) return;
                if (!this.IsAlive()) return;

                try { ScrollTo((int)AutoScrollX, (int)AutoScrollY); }
                catch (Exception ex) { Log.For(this).Error(ex); }
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
                    catch { /* No logging is needed. Continue. */ }
                }

                RefreshContent?.Dispose();
                Container?.Dispose();
                PartialPagingTimer?.Stop();
                PartialPagingTimer?.Dispose();
                View.ApiScrolledTo.FullEvent -= OnApiScrolledTo;
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