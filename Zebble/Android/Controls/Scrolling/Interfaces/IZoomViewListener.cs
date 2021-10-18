namespace Zebble.AndroidOS
{
    public interface IZoomViewListener
    {
        void OnZoomStarted(float zoom, float zoomx, float zoomy);
        void OnZooming(float zoom, float zoomx, float zoomy);
        void OnZoomEnded(float zoom, float zoomx, float zoomy);
    }
}