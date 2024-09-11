namespace Zebble.WinUI
{
    using System;
    using System.Threading.Tasks;
    using controls = Microsoft.UI.Xaml.Controls;
    using xaml = Microsoft.UI.Xaml;
    using Olive;

    public partial class WinUIScrollView : IRenderOrchestrator
    {
        const int GAP_UNIT = 30;
        const int REFRESHER_DURATION = 600;
        const int REFRESHER_ROTATION_DEGREE = 359;
        const int REFRESHER_MIN_HEIGHT = 50;

        float RefresherHeight;
        readonly ScrollView View;
        controls.ScrollViewer Result;
        bool IsSnapping, IsMovingByCode;

        internal controls.StackPanel Container = new() { VerticalAlignment = xaml.VerticalAlignment.Top };

        public WinUIScrollView(ScrollView view)
        {
            View = view;
            Mappings.Add(view.GetWeakReference(), this.GetWeakReference());
        }

        public Task<xaml.FrameworkElement> Render()
        {
            CreateScrollView();

            HandleEvents();

            Result.Loaded += Result_Loaded;

            Result.IsTabStop = true; // To prevent textbox auto focus

            if (SupportsChildPanning) EnableManualMode();

            return Task.FromResult<xaml.FrameworkElement>(Result);
        }

        async void Result_Loaded(object _, xaml.RoutedEventArgs __)
        {
            Result.Loaded -= Result_Loaded;
            await LoadInitialState();
            if (View.Refresh.Enabled) await ScrollToTop();
        }

        async Task ScrollToTop()
        {
            var waitAtLeastUntil = DateTime.UtcNow.AddMilliseconds(100);

            var remaining = waitAtLeastUntil.Subtract(DateTime.UtcNow);
            if (remaining > TimeSpan.Zero) await Task.Delay(remaining);

            Thread.UI.RunAction(() =>
            AutoMoveTo(vertical: RefresherHeight, animate: false));
        }

        async Task LoadInitialState()
        {
            SyncHeight();

            var verticalOffset = View.ScrollY.Round(0) + RefresherHeight;

            if (View.ScrollX == 0 && verticalOffset == 0) return;

            // For some reason it has a timing issue
            await Task.Delay(Animation.OneFrame);
            AutoMoveTo(View.ScrollX, verticalOffset, animate: false);
        }

        controls.ScrollMode IsEnabled(RepeatDirection direction)
        {
            if (!View.EnableScrolling) return controls.ScrollMode.Disabled;
            else if (View.Direction == direction) return controls.ScrollMode.Enabled;
            else return controls.ScrollMode.Disabled;
        }

        void CreateScrollView()
        {
            Result = new controls.ScrollViewer
            {
                IsScrollInertiaEnabled = true,
                HorizontalScrollBarVisibility = View.ShowHorizontalScrollBars ? controls.ScrollBarVisibility.Visible : controls.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = View.ShowVerticalScrollBars ? controls.ScrollBarVisibility.Visible : controls.ScrollBarVisibility.Auto,
                VerticalAlignment = xaml.VerticalAlignment.Top,
                Content = Container
            };

            ConfigureZooming();
            SetDirections();

            SyncHeight();

            Result.SizeChanged += Result_SizeChanged;

            CreatePullToRefresh();
        }

        void Result_SizeChanged(object _, xaml.SizeChangedEventArgs __) => SyncHeight();

        void SyncHeight()
        {
            if (!double.IsNaN(Result.ActualHeight))
                Container.MinHeight = Result.ActualHeight + RefresherHeight;
        }

        void HandleEvents()
        {
            View.ApiScrolledTo.HandleOn(Thread.UI, OnApiScrolledTo);
            View.RefreshScrollContentSize.HandleOnUI(() => {  /* TODO */ });

            Result.ViewChanged += Result_ViewChanged;
            Result.ViewChanging += Result_ViewChanging;

            View.EnableScrollingChanged.HandleOnUI(SetDirections);

            View.ZoomSettingsChanged.HandleOnUI(ConfigureZooming);
            View.ApiZoomChanged.HandleOnUI(SetZoom);
        }

        async Task OnApiScrolledTo(ScrollView.ApiMoveToEventArgs args)
        {
            // For some reason it has a timing issue
            await Task.Delay(Animation.OneFrame);
            AutoMoveTo(args.XOffset, args.YOffset, args.Animate);
        }

        void ConfigureZooming()
        {
            Result.MinZoomFactor = View.MinZoomScale;
            Result.MaxZoomFactor = View.MaxZoomScale;
            Result.ZoomMode = View.EnableZooming ? controls.ZoomMode.Enabled : controls.ZoomMode.Disabled;
            SetZoom();
        }

        void SetZoom() => Result.ChangeView(horizontalOffset: null, verticalOffset: null, zoomFactor: View.Zoom);

        async void CreatePullToRefresh()
        {
            if (!View.Refresh.Enabled) return;
            EnableManual(View);

            RefresherHeight = Refresher.Absolute().Opacity(0).ActualHeight.Round(0) + REFRESHER_MIN_HEIGHT;
            Refresher.parent = View;

            await View.WhenShown(() =>
             Refresher.Animate(new Animation
             {
                 Duration = REFRESHER_DURATION.Milliseconds(),
                 Easing = AnimationEasing.Linear,
                 Change = () => Refresher.Rotation(REFRESHER_ROTATION_DEGREE),
                 Repeats = -1
             }));

            SyncHeight();

            var refresherRendered = await Refresher.Render();
            await refresherRendered.ApplyCssToBranch();
            var nativeRefresher = refresherRendered.Native();
            if (nativeRefresher.MinHeight == 0) nativeRefresher.MinHeight = REFRESHER_MIN_HEIGHT;
            Container.Children.Add(nativeRefresher);
        }

        void SetDirections()
        {
            if (!SupportsChildPanning)
            {
                Result.HorizontalScrollMode = IsEnabled(RepeatDirection.Horizontal);
                Result.VerticalScrollMode = IsEnabled(RepeatDirection.Vertical);
            }
        }

        async void Result_ViewChanged(object sender, controls.ScrollViewerViewChangedEventArgs args)
        {
            IsMovingByCode = false;

            if (View.Refresh.Enabled && Result.VerticalOffset == 0) await InvokeRefresh();

            var newX = (float)Result.HorizontalOffset;
            var newY = (float)Result.VerticalOffset;
            var ended = !args.IsIntermediate;

            if (!Result.ZoomFactor.AlmostEquals(View.Zoom))
                View.RaiseUserZoomed(Result.ZoomFactor);

            View.SetUserScrolledX(newX);
            View.SetUserScrolledY(newY - RefresherHeight);
            if (ended) View.ScrollEnded.SignalRaiseOn(Thread.Pool);
        }

        Canvas Refresher => View.Refresh.Indicator;

        async Task InvokeRefresh()
        {
            Refresher.StartAnimation(x => x.Opacity(1));

            var waitAtLeastUntil = DateTime.UtcNow.AddSeconds(1);

            View.Refresh.Requested.SignalRaiseOn(Thread.Pool);

            var remaining = waitAtLeastUntil.Subtract(DateTime.UtcNow);
            if (remaining > TimeSpan.Zero) await Task.Delay(remaining);

            Refresher.StartAnimation(x => x.Opacity(0));

            // Why does it not animate?!
            Thread.UI.RunAction(() => AutoMoveTo(vertical: RefresherHeight, animate: true));
        }

        double GetCurrentOffset()
        {
            return View.Direction == RepeatDirection.Horizontal ? Result.HorizontalOffset.Round(0) : Result.VerticalOffset.Round(0);
        }

        async void Result_ViewChanging(object sender, controls.ScrollViewerViewChangingEventArgs args)
        {
            if (!View.EnableScrolling) return;
            if (IsMovingByCode) return;
            if (!View.PagingEnabled) return;
            if (View.PartialPagingSize <= 0) return;
            if (!args.IsInertial) return;
            if (IsSnapping) return;

            bool? forward = null;

            var current = GetCurrentOffset();
            var final = args.FinalView.Get(v => View.Direction == RepeatDirection.Horizontal ? v.HorizontalOffset : v.VerticalOffset);

            if (!Device.App.IsDesktop() && Math.Abs(current - final) < 1) forward = null;
            else if (final > current) forward = true;
            else if (final < current) forward = false;

            // Current index:
            var index = (decimal)(current / View.PartialPagingSize);

            if (forward == true) index = decimal.Floor(index) + 1;
            else if (forward == false) index = decimal.Ceiling(index) - 1;

            index = Math.Round(index, 0);

            await Snap((int)index);
        }

        async Task Snap(int index)
        {
            if (IsMovingByCode) return;
            if (IsSnapping) return;
            IsSnapping = true;
            var indexOffset = View.PartialPagingSize * index;

            var frames = Math.Abs(indexOffset - GetCurrentOffset()) / GAP_UNIT;
            if (frames < 1) frames = 1;
            if (frames > 3) frames = 3;

            while (true)
            {
                await Task.Delay(Animation.OneFrame);

                var loc = GetCurrentOffset();
                var pixelsPerFrame = (int)((indexOffset - loc) / frames);
                if (pixelsPerFrame == 0) pixelsPerFrame = (int)(indexOffset - loc);

                if (pixelsPerFrame > 0 && pixelsPerFrame < 5) pixelsPerFrame = 5;
                if (pixelsPerFrame < 0 && pixelsPerFrame > -5) pixelsPerFrame = -5;

                loc += pixelsPerFrame;
                if (pixelsPerFrame > 0 && loc > indexOffset) loc = indexOffset;
                if (pixelsPerFrame < 0 && loc < indexOffset) loc = indexOffset;

                if (View.Direction == RepeatDirection.Vertical) AutoMoveTo(vertical: loc);
                else AutoMoveTo(horizontal: loc);

                if (loc == indexOffset) break;

                // Give it some time to scroll, and then do it again.
                await Task.Delay(64);
            }

            IsSnapping = false;
        }

        void AutoMoveTo(double? horizontal = null, double? vertical = null, bool animate = true)
        {
            IsMovingByCode = true;
            Result?.ChangeView(horizontal, verticalOffset: vertical, zoomFactor: null, disableAnimation: !animate);
        }

        public void Dispose()
        {
            Result.Set(x =>
            {
                x.SizeChanged -= Result_SizeChanged;
                x.ViewChanged -= Result_ViewChanged;
                x.ViewChanging -= Result_ViewChanging;
                x.PointerWheelChanged -= Result_PointerWheelChanged;
                x.ManipulationDelta -= Result_ManipulationDelta;
                x.ManipulationStarting -= Result_ManipulationStarting;
            });

            Result = null;
			
			GC.SuppressFinalize(this);
        }
    }
}