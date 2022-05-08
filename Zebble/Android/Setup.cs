namespace Zebble.AndroidOS
{
    using System.Threading.Tasks;
    using Android.Views;
    using AndroidX.Core.View;
    using Zebble;

    public class Setup
    {
        const double ANIMATION_SLOW_DOWN = 0;
        static ViewGroup RootView;

        public static async Task Start(ViewGroup rootScreen, object rootViewController)
        {
            ConfigureAnimationDurations();

            UIRuntime.NativeRootScreen = rootViewController;

            Device.Screen.PreLoadConfiguration();

            var view = UIRuntime.CurrentActivity.Window?.DecorView ?? rootScreen;
            ViewCompat.SetFitsSystemWindows(view, true);
            ViewCompat.SetOnApplyWindowInsetsListener(view, new Device.Screen.ApplyWindowInstetsListener());

            await Device.Screen.ApplyWindowInstetsListener.OnInsetsConsumed.Task;

            await AddRootView();

            rootScreen.AddView(RootView);
            UIRuntime.CurrentActivity.Window.SetSoftInputMode(SoftInput.AdjustResize);
        }

        public static void SwitchActivity(ViewGroup rootScreen, object rootViewController)
        {
            UIRuntime.NativeRootScreen = rootViewController;

            (RootView.Parent as ViewGroup)?.RemoveView(RootView);
            rootScreen.AddView(RootView);
        }

        static void ConfigureAnimationDurations()
        {
            Animation.DropDuration = Animation.DropDuration.Multiply(1 + ANIMATION_SLOW_DOWN);
            Animation.DefaultDuration = Animation.DefaultDuration.Multiply(1 + ANIMATION_SLOW_DOWN);
            Animation.FadeDuration = Animation.FadeDuration.Multiply(1 + ANIMATION_SLOW_DOWN);
        }

        static async Task AddRootView()
        {
            UIRuntime.RenderRoot = new Canvas
            {
                Id = "RenderRoot",
                CssClass = "android-only",
                IsAddedToNativeParentOnce = true,
                ClipChildren = false
            }.Background(color: Colors.Transparent)
            .Size(Device.Screen.Width, Device.Screen.Height);

            await UIRuntime.RenderRoot.OnPreRender();

            RootView = (ViewGroup)(await UIRuntime.Render(UIRuntime.RenderRoot)).Native;

            RootView.SetClipToPadding(clipToPadding: false);
        }
    }
}