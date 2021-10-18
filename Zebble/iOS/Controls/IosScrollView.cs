namespace Zebble.IOS
{
    using CoreGraphics;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using UIKit;
    using Olive;

    class IosScrollView : UIScrollView
    {
        bool IsDisposing, IsContentOffsetSetManually;
        ScrollView View;
        UIRefreshControl Refresh;
        DateTime AssumeScrollingHeightIsValidUntil;

        public IosScrollView(ScrollView view) : base(view.GetFrame())
        {
            View = view;
            CreateScrollView();
            HandleEvents();
        }

        internal async Task<IosScrollView> GetResult()
        {
            await CreatePullToRefresh();
            ScrollTo(View.ScrollX, View.ScrollY, animated: false);
            return this;
        }

        void CreateScrollView()
        {
            ScrollEnabled = View.EnableScrolling;

            SetHorizontalScrolling();
            SetVerticalScrolling();
            SetContentSize();

            BackgroundColor = UIColor.Clear;
            SetPaging();
            DelaysContentTouches = true;
            CanCancelContentTouches = true;
            ShowsVerticalScrollIndicator = ShowsHorizontalScrollIndicator = View.ShowScroll;
            AlwaysBounceVertical = View.Direction == RepeatDirection.Horizontal;
            UserInteractionEnabled = true;

            if (View.OnePagePartialPagingEnabled) DecelerationRate = DecelerationRateFast;

            SetZooming();
        }

        void SetPaging()
        {
            PagingEnabled = ZoomScale <= 1 && View.PagingEnabled;
            DirectionalLockEnabled = PagingEnabled;
        }

        void SetHorizontalScrolling()
        {
            var allowed = View.Direction == RepeatDirection.Horizontal || ZoomScale > 1;
            ShowsHorizontalScrollIndicator = allowed;
            AlwaysBounceHorizontal = false;
        }

        void SetVerticalScrolling()
        {
            var allowed = View.Direction == RepeatDirection.Vertical || ZoomScale > 1;
            ShowsVerticalScrollIndicator = allowed;
            AlwaysBounceVertical = false;
        }

        void SetZooming()
        {
            if (View.EnableZooming)
            {
                MinimumZoomScale = View.MinZoomScale;
                MaximumZoomScale = View.MaxZoomScale;
            }
            else MinimumZoomScale = MaximumZoomScale = 1;
        }

        void SetContentSize()
        {
            var size = View.CalculateContentSize();

            if (View.Refresh.Enabled && (View.Direction == RepeatDirection.Vertical))
                // Content size should be large enough to necessiate actual scrolling:
                size.Height = size.Height.LimitMin((float)Frame.Size.Height + 2);

            ContentSize = new CGSize(size.Width * (float)ZoomScale, size.Height * (float)ZoomScale);
        }

        void HandleEvents()
        {
            View.RefreshScrollContentSize.HandleOnUI(SetContentSize);
            View.ApiScrolledTo.HandleOnUI(e => ScrollTo(e.XOffset, e.YOffset, e.Animate));
            View.ContentSizeChanged.HandleOnUI(SetContentSize);
            View.EnableScrollingChanged.HandleOnUI(() => ScrollEnabled = View.EnableScrolling);
            View.ZoomSettingsChanged.HandleOnUI(SetZooming);
            View.ApiZoomChanged.HandleOnUI(() => ZoomScale = View.Zoom);

            Scrolled += IosScrollView_Scrolled;
            DraggingEnded += IosScrollView_DraggingEnded;
            DecelerationEnded += IosScrollView_DecelerationEnded;
            DidZoom += IosScrollView_DidZoom;
            ViewForZoomingInScrollView += x => Subviews.FirstOrDefault();
            ZoomingEnded += IosScrollView_ZoomingEnded;
        }

        void IosScrollView_DidZoom(object _, EventArgs __)
        {
            SetHorizontalScrolling();
            SetVerticalScrolling();

            var scale = (float)ZoomScale;

            View.RaiseUserZoomed(scale);
        }

        void IosScrollView_ZoomingEnded(object _, ZoomingEndedEventArgs __)
        {
            SetContentSize();
            SetPaging();
        }

        void IosScrollView_DecelerationEnded(object _, EventArgs __)
        {
            OnPartilaPagingEnded();
            View.ScrollEnded.SignalRaiseOn(Thread.Pool);
        }

        void IosScrollView_DraggingEnded(object _, DraggingEventArgs args)
        {
            if (!args.Decelerate) OnPartilaPagingEnded();
        }

        void IosScrollView_Scrolled(object _, EventArgs __)
        {
            if (View.Direction == RepeatDirection.Vertical)
            {
                if (DateTime.UtcNow > AssumeScrollingHeightIsValidUntil)
                {
                    AssumeScrollingHeightIsValidUntil = DateTime.UtcNow.AddSeconds(1);
                    SetContentSize();
                }
            }

            var xOffset = (float)ContentOffset.X;
            var yOffset = (float)ContentOffset.Y;

            View.SetUserScrolledX(xOffset);
            View.SetUserScrolledY(yOffset);
        }

        async Task CreatePullToRefresh()
        {
            if (!View.Refresh.Enabled) return;

            Refresh = new UIRefreshControl();

            var refreshContent = (await View.Refresh.Indicator.Render()).Native();

            refreshContent.Frame = Refresh.Bounds;
            refreshContent.AutoresizingMask = UIViewAutoresizing.All;
            refreshContent.TranslatesAutoresizingMaskIntoConstraints = true;

            Refresh.AddSubview(refreshContent);
            Refresh.PrimaryActionTriggered += async (s, e) =>
            {
                if (Refresh.Refreshing)
                {
                    View.Refresh.Requested.SignalRaiseOn(Thread.Pool);
                    await Task.Delay(100);
                    Refresh.EndRefreshing();
                }
            };

            AddSubview(Refresh);
        }

        bool IsPartialPagingEnabled() => ZoomScale <= 1 && View.PartialPagingEnabled;

        void OnPartilaPagingEnded()
        {
            if (!IsPartialPagingEnabled()) return;

            var pageSize = View.PartialPagingSize;

            var addingOffset = 0f;
            CGPoint correctOffset;

            if (View.Direction == RepeatDirection.Vertical)
            {
                if (ContentOffset.Y % pageSize >= pageSize / 2)
                    addingOffset = pageSize;
                correctOffset = new CGPoint(0, (((int)(ContentOffset.Y / pageSize)) * pageSize) + addingOffset);
            }
            else
            {
                if (ContentOffset.X % pageSize >= pageSize / 2) addingOffset = pageSize;
                correctOffset = new CGPoint((((int)(ContentOffset.X / pageSize)) * pageSize) + addingOffset, 0);
            }

            if (correctOffset != null) ScrollTo((float)correctOffset.X, (float)correctOffset.Y, animated: true);
        }

        void ScrollTo(float xOffset, float yOffset, bool animated)
        {
            IsContentOffsetSetManually = false;
            var scrollViewHeight = (float)Frame.Size.Height;
            var scrollContentSizeHeight = (float)ContentSize.Height;

            if ((scrollViewHeight + Device.Keyboard.SoftKeyboardHeight).AlmostEquals(scrollContentSizeHeight))
                yOffset -= yOffset - Device.Keyboard.SoftKeyboardHeight;

            SetContentOffset(new CGPoint(xOffset, yOffset), animated);
            IsContentOffsetSetManually = true;
        }

        public override void SetContentOffset(CGPoint contentOffset, bool animated)
        {
            if (Device.Keyboard.IsOpen && IsContentOffsetSetManually) return;
            base.SetContentOffset(contentOffset, animated);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposing)
            {
                IsDisposing = true;
                Scrolled -= IosScrollView_Scrolled;
                DraggingEnded -= IosScrollView_DraggingEnded;
                DecelerationEnded -= IosScrollView_DecelerationEnded;
                DidZoom -= IosScrollView_DidZoom;
                ZoomingEnded -= IosScrollView_ZoomingEnded;

                Subviews.Do(s => s.Dispose());
                Refresh?.Dispose();
                View = null;
            }

            base.Dispose(disposing);
        }
    }
}