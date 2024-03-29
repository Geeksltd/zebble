namespace Zebble
{
    using Olive;
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;

    public partial class ScrollView : View
    {
        float scrollX, scrollY, zoom = 1;
        bool enableScrolling = true;

        public bool ShowVerticalScrollBars { get; set; }
        public bool ShowHorizontalScrollBars { get; set; }
        public bool PagingEnabled { get; set; }

        public bool PartialPagingEnabled { get; set; }
        public bool OnePagePartialPagingEnabled { get; set; }
        public RepeatDirection Direction { get; set; } = RepeatDirection.Vertical;

        public readonly AsyncEvent ScrollEnded = new();
        public readonly AsyncEvent UserScrolledHorizontally = new();
        public readonly AsyncEvent UserScrolledVertically = new();
        public readonly AsyncEvent EnableScrollingChanged = new();
        public readonly AsyncEvent ContentSizeChanged = new();
        public readonly AsyncEvent<int> ScrolledToNewPage = new();
        public readonly AsyncEvent RefreshScrollContentSize = new();

        internal readonly AsyncEvent ApiZoomChanged = new();
        public readonly AsyncEvent UserZoomChanged = new(ConcurrentEventRaisePolicy.Queue);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public readonly AsyncEvent<ApiMoveToEventArgs> ApiScrolledTo = new();

        public readonly Refresher Refresh = new();

        public bool EnableScrolling
        {
            get => enableScrolling;
            set { SetEnableScrolling(value); }
        }

        public Task SetEnableScrolling(bool value)
        {
            if (enableScrolling == value) return Task.CompletedTask;
            enableScrolling = value;
            return EnableScrollingChanged.Raise();
        }

        public override async Task OnPreRender()
        {
            await base.OnPreRender();
            if (UIRuntime.IsDevMode && Padding.HasValue())
                Log.For(this).Error("Do not set padding on ScrollView directly. Instead its immediate child should be a container stack with the padding you want to apply." + GetFullPath());
        }

        public float PartialPagingSize { get; set; }

        public float ScrollX
        {
            get => scrollX;
            set => _ = ScrollTo(xOffset: value.LimitMin(0));
        }

        internal void SetUserScrolledX(float newX)
        {
            if (!scrollX.AlmostEquals(newX))
            {
                scrollX = newX;
                UserScrolledHorizontally.SignalRaiseOn(Thread.Pool);
            }
        }

        internal void SetUserScrolledY(float newY)
        {
            if (!scrollY.AlmostEquals(newY))
            {
                scrollY = newY;
                UserScrolledVertically.SignalRaiseOn(Thread.Pool);
            }
        }

        internal void RaiseUserZoomed(float zoom)
        {
            if (this.zoom.AlmostEquals(zoom)) return;
            this.zoom = zoom;

            UserZoomChanged.SignalRaiseOn(Thread.Pool);
        }

        public float Zoom
        {
            get => zoom;
            set
            {
                if (value.AlmostEquals(zoom)) return;
                zoom = value;
                ApiZoomChanged.Raise();
            }
        }

        public float ScrollY
        {
            get => scrollY;
            set => _ = ScrollTo(yOffset: value.LimitMin(0));
        }

        public async Task ScrollTo(float? yOffset = null, float? xOffset = null, bool animate = false)
        {
            var changed = false;

            if (yOffset.HasValue && !yOffset.Value.AlmostEquals(ScrollY))
            {
                scrollY = yOffset.Value;
                changed = true;
            }

            if (xOffset.HasValue && !xOffset.Value.AlmostEquals(ScrollX))
            {
                scrollX = xOffset.Value;
                changed = true;
            }

            if (!changed) return;

            // Scroll views have a problem with run-time setting of the scroll value.
            await WhenShown(() =>
            Thread.UI.Post(() => Thread.Pool.Run(async () =>
            {
                await Task.Delay(Animation.OneFrame);
                var renderArgs = new ApiMoveToEventArgs { XOffset = scrollX, YOffset = scrollY, Animate = animate };
                await ApiScrolledTo.Raise(renderArgs);
            })));
        }

        public Size CalculateContentSize()
        {
            if (Direction == RepeatDirection.Horizontal)
                return new Size(CalculateContentWidth(), ActualHeight);
            else
                return new Size(ActualWidth, CalculateContentHeight());
        }

        internal async Task RaiseContentSizeChanged()
        {
            await ContentSizeChanged.Raise();
            if (IsContentHeightShorterThanActualHeight())
                ScrollY = 0;
            else if (ScrollY > 0 && IsShown)
                ScrollY = Math.Min(ScrollY, CalculateContentHeight() - ActualHeight);
        }

        public bool IsContentHeightShorterThanActualHeight()
        {
            return CalculateContentHeight() - ActualHeight <= 0;
        }

        protected override async Task OnChildAdded(View view)
        {
            await base.OnChildAdded(view);
            await RaiseContentSizeChanged();

            view.Height.Changed.Handle(RaiseContentSizeChanged);
            view.Width.Changed.Handle(RaiseContentSizeChanged);
        }

        protected override void OnChildRemoved(View view)
        {
            base.OnChildRemoved(view);

            view.Height.Changed.RemoveHandler(RaiseContentSizeChanged);
            view.Width.Changed.RemoveHandler(RaiseContentSizeChanged);

            RaiseContentSizeChanged().GetAwaiter();
        }

        float CalculateContentWidth()
        {
            var relevantChildren = CurrentChildren.Except(x => x.absolute).ToList();

            if (UIRuntime.IsDevMode && relevantChildren.Any(x => x.Width.PercentageValue.HasValue))
                throw new RenderException("Children of a horizontal scroll view should not have percentage based width.");

            return relevantChildren.Sum(x => x.CalculateTotalWidth());
        }

        float CalculateContentHeight()
        {
            var relevantChildren = CurrentChildren.Except(x => x.absolute).ToList();

            if (UIRuntime.IsDevMode && relevantChildren.Any(x => x.Height.PercentageValue.HasValue))
                throw new RenderException("Children of a vertical scroll view should not have percentage based height.");

            return relevantChildren.Sum(x => x.CalculateTotalHeight());
        }

        public Task MovedToPage(int page) => ScrolledToNewPage.Raise(page);

        public Task ScrollToView(View child, bool animate = true, float offset = 0)
        {
            var parents = child.WithAllParents().TakeWhile(x => x != this).ToArray();

            var yOffset = ScrollY;
            if (Direction == RepeatDirection.Vertical) yOffset = parents.Sum(x => x.ActualY) - offset;

            var xOffset = ScrollX;
            if (Direction == RepeatDirection.Horizontal) xOffset = parents.Sum(x => x.ActualX) - offset;

            return ApiScrolledTo.Raise(new ApiMoveToEventArgs { XOffset = xOffset, YOffset = yOffset, Animate = animate });
        }

        public class Refresher
        {
            public Canvas Indicator = new Canvas().Id("RefreshingIndicator");
            public readonly AsyncEvent Requested = new();
            public bool Enabled;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public class ApiMoveToEventArgs
        {
            public float XOffset, YOffset;
            public bool Animate;
        }
    }
}