namespace Zebble.AndroidOS
{
    using System;
    using System.Threading.Tasks;
    using Zebble.Device;
    using Olive;

    class PullToRefresh
    {
        public static async Task CreatePullToRefresh(IPullToRefreshScrollView scroll)
        {
            if (!scroll.View.Refresh.Enabled) return;

            scroll.Refresher.parent = scroll.View;
            scroll.Refresher.Absolute().Opacity(0);

            await scroll.View.Add(scroll.Refresher);

            if (scroll.View.Direction == RepeatDirection.Horizontal)
            {
                scroll.Refresher.Y((View.Root.ActualHeight - scroll.Refresher.ActualHeight) / 2);
                scroll.ScrollToPosition(Scale.ToDevice(scroll.Refresher.CalculateTotalWidth()), 0, animate: true);
            }
            else
            {
                scroll.Refresher.X((View.Root.ActualWidth - scroll.Refresher.ActualWidth) / 2);
                scroll.View.Padding(top: scroll.View.Effective.BorderAndPaddingTop() + scroll.Refresher.ActualHeight);

                scroll.ScrollToPosition(0, Scale.ToDevice(scroll.Refresher.CalculateTotalHeight()), animate: true);
            }

            await scroll.View.WhenShown(async () =>
            {
                await scroll.Refresher.Animate(new Animation
                {
                    Duration = 600.Milliseconds(),
                    Easing = AnimationEasing.Linear,
                    Change = () => scroll.Refresher.Rotation(359),
                    Repeats = -1
                });
            });
        }

        public static async Task InvokeRefresh(IPullToRefreshScrollView scroll)
        {
            if (scroll.Refresher.Native is null)
                scroll.CreatePullToRefresh();

            scroll.Refresher.Opacity(1);

            var waitAtLeastUntil = DateTime.UtcNow.AddSeconds(1);
            scroll.View.Refresh.Requested.SignalRaiseOn(Thread.Pool);

            var remaining = waitAtLeastUntil.Subtract(DateTime.UtcNow);
            if (remaining > TimeSpan.Zero) await Task.Delay(remaining);

            if (scroll.View.Direction == RepeatDirection.Horizontal)
                scroll.ScrollToPosition(Scale.ToDevice(scroll.Refresher.CalculateTotalWidth()), 0, animate: true);
            else scroll.ScrollToPosition(0, Scale.ToDevice(scroll.Refresher.CalculateTotalHeight()), animate: true);

            scroll.Refresher.Opacity(0);
        }

        public static Size HandleScrollPosition(IPullToRefreshScrollView scroll, float xPosition, float yPosition, bool animate = false)
        {
            if (scroll.View.Direction == RepeatDirection.Horizontal)
            {
                if (xPosition < 0) xPosition = 0;
                else if (scroll.View.Refresh.Enabled && xPosition < scroll.Refresher.ActualWidth) xPosition = 0;
                else
                {
                    var totalWidth = Scale.ToDevice(scroll.View.CalculateContentSize().Width);
                    if (xPosition > totalWidth) xPosition = totalWidth;
                }
            }
            else
            {
                if (yPosition < 0) yPosition = 0;
                else if (scroll.View.Refresh.Enabled && yPosition < scroll.Refresher.ActualHeight) yPosition = 0;
                else
                {
                    var totalHeight = Scale.ToDevice(scroll.View.CalculateContentSize().Height);
                    if (yPosition > totalHeight) yPosition = totalHeight;
                }
            }

            return new Size(xPosition, yPosition);
        }
    }
}