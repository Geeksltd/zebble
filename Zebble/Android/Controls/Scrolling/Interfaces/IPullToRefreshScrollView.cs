namespace Zebble.AndroidOS
{
    using Android.Widget;

    interface IPullToRefreshScrollView : IScrollView
    {
        Canvas Refresher { get; set; }
        FrameLayout RefreshContent { get; set; }
        void CreatePullToRefresh();
        void InvokeRefresh();
    }
}