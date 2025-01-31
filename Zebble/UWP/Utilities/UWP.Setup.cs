namespace Zebble.UWP
{
    using Olive;

    internal class Setup
    {
        public static void Start()
        {
            Animation.DefaultDuration = 400.Milliseconds();
            Animation.FadeDuration = 300.Milliseconds();

            UIRuntime.RenderRoot = new Canvas
            {
                IsAddedToNativeParentOnce = true,
                Id = "RenderRoot",
                CssClass = "uwp-only"
            }.Background(color: Colors.White);
        }
    }
}