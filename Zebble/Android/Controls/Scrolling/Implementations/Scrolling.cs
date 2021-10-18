namespace Zebble.AndroidOS
{
    using System;
    using Android.Widget;
    using Zebble.Device;

    class Scrolling
    {
        public static void UpdateSize(IScrollView scroll, int width, int height)
        {
            if (scroll.View.Direction == RepeatDirection.Horizontal) 
                scroll.GetContainer().GetEffectiveLayoutContainer().LayoutParameters.Height = height;
            else 
                scroll.GetContainer().GetEffectiveLayoutContainer().LayoutParameters.Width = width;

            SyncContainerSize(scroll);
        }

        public static void SyncContainerSize(IScrollView scroll)
        {
            var layoutParams = (scroll as AndroidScrollContainer)?.GetEffectiveLayoutContainer().LayoutParameters;
            if (layoutParams is null) return;

            void setLayout(LinearLayout.LayoutParams layout)
            {
                if (scroll is IZoomableScrollView zoom)
                    scroll.GetContainer().GetEffectiveLayoutContainer().LayoutParameters = layout;
            }

            if (scroll.View.Direction == RepeatDirection.Horizontal)
            {
                var newLayout = new LinearLayout.LayoutParams(GetEffectiveContainerWidth(scroll), layoutParams.Height);
                scroll.Container.GetEffectiveLayoutContainer().LayoutParameters = newLayout;

                setLayout(newLayout);
            }
            else
            {
                var newLayout = new LinearLayout.LayoutParams(layoutParams.Width, GetEffectiveContainerHeight(scroll));
                scroll.Container.GetEffectiveLayoutContainer().LayoutParameters = newLayout;

                setLayout(layout: newLayout);
            }
        }

        static int GetEffectiveContainerWidth(IScrollView scroll)
        {
            var frame = scroll as AndroidScrollContainer;
            if (frame.GetEffectiveLayoutContainer().LayoutParameters is null) 
                frame.GetEffectiveLayoutContainer().LayoutParameters = scroll.View.GetFrame();

            var contentWidth = scroll.View.CalculateContentSize().Width;

            if (scroll is IPullToRefreshScrollView pull)
                if (pull.Refresher != null && pull.View.Refresh.Enabled)
                    contentWidth += pull.Refresher.CalculateTotalWidth();

            if (scroll is IZoomableScrollView zoom)
                contentWidth = Scale.ToDevice(contentWidth) * zoom.ZoomContainer.GetZoom();

            return (int)Math.Max(frame.GetEffectiveLayoutContainer().LayoutParameters.Width, contentWidth);
        }

        static int GetEffectiveContainerHeight(IScrollView scroll)
        {
            var frame = scroll as AndroidScrollContainer;
            if (frame.GetEffectiveLayoutContainer().LayoutParameters is null) 
                frame.GetEffectiveLayoutContainer().LayoutParameters = scroll.View.GetFrame();

            var contentHeight = scroll.View.CalculateContentSize().Height;

            if (scroll is IPullToRefreshScrollView pull)
                if (pull.Refresher != null && pull.View.Refresh.Enabled)
                {
                    contentHeight += pull.Refresher.CalculateTotalWidth();
                    var rootHeight = View.Root.Height.CurrentValue;
                    if (contentHeight <= rootHeight) contentHeight += rootHeight;
                }

            if (scroll is IZoomableScrollView zoom)
                contentHeight = Scale.ToDevice(contentHeight) * zoom.ZoomContainer.GetZoom();

            return (int)Math.Max(frame.GetEffectiveLayoutContainer().LayoutParameters.Height, contentHeight);
        }
    }
}