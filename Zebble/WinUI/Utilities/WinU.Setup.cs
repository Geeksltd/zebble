namespace Zebble.WinUI
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
                CssClass = "winui-only"
            }.Background(color: Colors.White);
        }
    }
}