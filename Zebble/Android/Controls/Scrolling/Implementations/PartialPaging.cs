namespace Zebble.AndroidOS
{
    using System;
    using Zebble.Device;

    class PartialPaging
    {
        public static void Configure(IPartialPagingScrollView scroll)
        {
            if (scroll.PartialPagingTimer is null) scroll.PartialPagingTimer = new System.Timers.Timer();

            scroll.PartialPagingTimer.Interval = scroll.PartialPagingInterval;
            scroll.PartialPagingTimer.Elapsed += async (s, e) =>
            {
                scroll.PartialPagingTimer.Stop();
                Thread.UI.RunAction(() => scroll.OnPartialPagingEnded());
            };
        }

        public static void OnEnd(IPartialPagingScrollView scroll)
        {
            if (!scroll.PartialPagingEnabled) return;

            var index = Math.Round(scroll.View.ScrollX / scroll.PartialPagingSize);
            var addingOffset = index * scroll.PartialPagingSize;
            scroll.ScrollToPosition(Scale.ToDevice((float)addingOffset), 0, animate: true);
        }
    }
}