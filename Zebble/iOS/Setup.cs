namespace Zebble.IOS
{
    using System.Threading.Tasks;
    using UIKit;
    using Zebble;

    internal class Setup
    {
        public static async Task Start(UIViewController rootScreen, UIWindow window)
        {
            UIRuntime.NativeRootScreen = rootScreen;

            Device.Screen.ConfigureSize(
                () => (float)UIScreen.MainScreen.Bounds.Width,
                () => (float)UIScreen.MainScreen.Bounds.Height
            );

            UIRuntime.RenderRoot = new Canvas
            {
                Id = "RenderRoot",
                CssClass = "ios-only",
                IsAddedToNativeParentOnce = true
            }.Size(Device.Screen.Width, Device.Screen.Height);

            await UIRuntime.RenderRoot.OnPreRender();

            var nativeRoot = (await UIRuntime.Render(UIRuntime.RenderRoot)).Native();
            nativeRoot.AddGestureRecognizer(Renderer.AddHardwareBackGesture());

            // This will resize the root to full screen size on device rotation
            nativeRoot.AutoresizingMask = UIViewAutoresizing.FlexibleBottomMargin | UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

            rootScreen.View.Add(nativeRoot);
            window.AddSubview(rootScreen.View);
            window.RootViewController = rootScreen;
            window.BackgroundColor = UIColor.White;
        }
    }
}