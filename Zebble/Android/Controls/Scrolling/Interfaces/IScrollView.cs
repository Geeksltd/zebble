namespace Zebble.AndroidOS
{
    using Android.Views;

    public interface IScrollView
    {
        ScrollView View { get; set; }
        AndroidScrollContainer Container { get; set; }
        AndroidScrollContainer GetContainer();
        void ScrollToPosition(float xOffset, float yOffset, bool animate);
    }

    public interface IZoomableScrollView : IScrollView
    {
        ZoomView ZoomContainer { get; set; }
    }
}