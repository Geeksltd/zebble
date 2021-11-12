namespace Zebble.Device
{
    using Android.Content.Res;
    using Android.OS;
    using Android.Util;
    using Android.Views;
    using Olive;
    using System.IO;
    using System.Threading.Tasks;
    using Zebble;
    using static Android.Graphics.Bitmap;

    partial class Screen
    {
        static IWindowManager WindowManager => UIRuntime.CurrentActivity.WindowManager;
        static Display Display => WindowManager.DefaultDisplay;
        static Resources SystemtResources;
        public static int NavigationBarHeight;
        public static bool UseSystemFontSetting { get; set; } = true;

        /// <summary>
        /// Based on just the device's screen pixel density (ignoring the user's font preferences).
        /// </summary>
        public static float HardwareDensity { get; internal set; } = 1;

        /// <summary>
        /// Based on both the device's screen pixel density and the user's font preferences.
        /// </summary>
        public static float Density { get; internal set; } = 1;

        internal static async Task LoadConfiguration()
        {
            SystemtResources = GetResources();

            HardwareDensity = SystemtResources.DisplayMetrics.Density;
            Density = SystemtResources.DisplayMetrics.ScaledDensity.LimitMax(1.25f * HardwareDensity);

            DarkMode = (SystemtResources.Configuration.UiMode & UiMode.NightMask) == UiMode.NightYes;

            DetectStatusBarHeight();
            DetectSoftNavigationBar();

            ConfigureSize(
                widthProvider: () =>
                {
                    var size = new Android.Graphics.Point();
                    Display.GetRealSize(size);
                    return Scale.ToZebble(size.X);
                },
                heightProvider: () =>
                {
                    var size = new Android.Graphics.Point();
                    Display.GetRealSize(size);

                    // TODO: Shouldn't we remove NavigationBarHeight??!!
                    return Scale.ToZebble(size.Y);// - NavigationBarHeight;
                }
            );
        }

        static Resources GetResources()
        {
            if (UseSystemFontSetting) return Resources.System;

            var res = UIRuntime.CurrentActivity.Resources;
            var config = new Configuration();
            config.SetToDefaults();
            res.UpdateConfiguration(config, res.DisplayMetrics);
            return res;
        }

        static void DetectStatusBarHeight()
        {
            if (!StatusBar.IsVisible) return;
            var statusBarResourceId = SystemtResources.GetIdentifier("status_bar_height", "dimen", "android");
            if (statusBarResourceId == 0) return;

            StatusBar.Height = Scale.ToZebble(SystemtResources.GetDimensionPixelSize(statusBarResourceId));
        }

        static void DetectSoftNavigationBar()
        {
            // This is intentional. Please don't change the code structure
            var navBarResourceId = SystemtResources.GetIdentifier("navigation_bar_height", "dimen", "android");
            if (navBarResourceId == 0) return;

            var hasNavigatioBarValue = Scale.ToZebble(SystemtResources.GetDimensionPixelSize(navBarResourceId));

            var safeScreen = new Android.Graphics.Point();
            Display.GetSize(safeScreen);

            var noNavigatioBarValue = Scale.ToZebble(SystemtResources.DisplayMetrics.HeightPixels - safeScreen.Y).LimitMin(0);

            var hasSoftKeys = HasSoftKeys();

            NavigationBarHeight = hasSoftKeys ? hasNavigatioBarValue : noNavigatioBarValue;
        }

        static bool HasSoftKeys()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.JellyBeanMr1)
            {
                var realDisplayMetrics = new DisplayMetrics();
                Display.GetRealMetrics(realDisplayMetrics);

                int realHeight = realDisplayMetrics.HeightPixels;
                int realWidth = realDisplayMetrics.WidthPixels;

                var displayMetrics = new DisplayMetrics();
                Display.GetMetrics(displayMetrics);

                int displayHeight = displayMetrics.HeightPixels;
                int displayWidth = displayMetrics.WidthPixels;

                return (realWidth - displayWidth) > 0 || (realHeight - displayHeight) > 0;
            }

            var hasMenuKey = ViewConfiguration.Get(UIRuntime.CurrentActivity.ApplicationContext).HasPermanentMenuKey;
            var hasBackKey = KeyCharacterMap.DeviceHasKey(Keycode.Back);

            return !hasMenuKey && !hasBackKey;
        }

        public static partial class StatusBar
        {
            static StatusBar() => isVisible = true;

            static void DoSetBackgroundColor()
            {
                if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop) return;

                if (BackgroundColor != null)
                {
                    var bgcolor = new Android.Graphics.Color
                    {
                        A = BackgroundColor.Alpha,
                        R = BackgroundColor.Red,
                        G = BackgroundColor.Green,
                        B = BackgroundColor.Blue,
                    };

                    UIRuntime.CurrentActivity.Window.SetStatusBarColor(bgcolor);
                }
            }

            static void DoSetForegroundColor() { }

            static void DoSetTransparency()
            {
                if (IsTransparent)
                    UIRuntime.CurrentActivity.Window.AddFlags(WindowManagerFlags.TranslucentStatus);
                else
                {
                    UIRuntime.CurrentActivity.Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
                    UIRuntime.CurrentActivity.Window.ClearFlags(WindowManagerFlags.TranslucentStatus);
                }
            }

            static void DoSetVisibility()
            {
                if (IsVisible)
                    UIRuntime.CurrentActivity.Window.ClearFlags(WindowManagerFlags.Fullscreen);
                else
                    UIRuntime.CurrentActivity.Window.AddFlags(WindowManagerFlags.Fullscreen);
            }
        }

        public static DeviceOrientation Orientation
        {
            get
            {
                switch (Display.Rotation)
                {
                    case SurfaceOrientation.Rotation90:
                    case SurfaceOrientation.Rotation270:
                        return DeviceOrientation.Landscape;
                    default: return DeviceOrientation.Portrait;
                }
            }
        }

        static async Task<FileInfo> DoSaveAsImage(object input)
        {
            using (var stream = new MemoryStream())
            {
                var inputNative = (Android.Views.View)input;

                var @params = inputNative.LayoutParameters;
                var width = @params.Width;
                var height = @params.Height;

                if (width < 0) width = Scale.ToDevice(Zebble.View.Root.ActualWidth);
                if (height < 0) height = Scale.ToDevice(Zebble.View.Root.ActualHeight);

                using (var imageBitmap = CreateBitmap(width, height, Android.Graphics.Bitmap.Config.Argb8888))
                {
                    var canvasImage = new Android.Graphics.Canvas(imageBitmap);
                    inputNative.Layout(0, 0, width, height);
                    inputNative.Draw(canvasImage);

                    await imageBitmap.CompressAsync(CompressFormat.Png, 0, stream);
                    imageBitmap?.Recycle();

                    var bitmapData = stream.ToArray();
                    var result = IO.CreateTempFile(".png");
                    result.WriteAllBytes(bitmapData);

                    return result;
                }
            }
        }
    }
}