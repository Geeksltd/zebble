namespace Zebble.Device
{
    using Android.Content.Res;
    using Android.OS;
    using Android.Views;
    using AndroidX.Core.View;
    using Olive;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Zebble;
    using static Android.Graphics.Bitmap;

    partial class Screen
    {
        static IWindowManager WindowManager => UIRuntime.CurrentActivity.WindowManager;
        static Display Display => WindowManager.DefaultDisplay;
        static Resources Resources;
        static Android.Util.DisplayMetrics DisplayMetrics => Resources.DisplayMetrics;
        public static int NavigationBarHeight;

        /// <summary>
        /// Based on just the device's screen pixel density (ignoring the user's font preferences).
        /// </summary>
        public static float HardwareDensity { get; internal set; } = 1;

        /// <summary>
        /// Based on both the device's screen pixel density and the user's font preferences.
        /// </summary>
        public static float Density { get; internal set; } = 1;

        public static readonly DisplaySettings DisplaySetting = new();

        internal static void LoadConfiguration(ViewGroup rootScreen)
        {
            rootScreen.SetFitsSystemWindows(true);
            ViewCompat.SetOnApplyWindowInsetsListener(rootScreen, new ApplyWindowInstetsListener());

            Resources = GetResources();

            HardwareDensity = DisplayMetrics.Density;
            Density = DisplayMetrics.ScaledDensity;

            DarkMode = (Resources.Configuration.UiMode & UiMode.NightMask) == UiMode.NightYes;

            var realSize = new Android.Graphics.Point();
            Display.GetRealSize(realSize);

            DisplaySetting.WindowWidth = DisplayMetrics.WidthPixels;
            DisplaySetting.WindowHeight = DisplayMetrics.HeightPixels;

            DisplaySetting.HardwareWidth = realSize.X;
            DisplaySetting.HardwareHeight = realSize.Y;

            DisplaySetting.RealWidth = realSize.X;
            DisplaySetting.RealHeight = realSize.Y;

            DisplaySetting.OutOfWindowStatusBarHeight = GetStatusBarHeight();
            StatusBar.Height = Scale.ToZebble(DisplaySetting.OutOfWindowStatusBarHeight);

            DisplaySetting.OutOfWindowNavbarHeight = GetNavigationBarHeight();
            NavigationBarHeight = Scale.ToZebble(DisplaySetting.OutOfWindowNavbarHeight);

            ConfigureSize(
                widthProvider: () => Scale.ToZebble(DisplaySetting.WindowWidth),
                heightProvider: () => Scale.ToZebble(DisplaySetting.WindowHeight)
            );
        }

        static Resources GetResources() => UIRuntime.CurrentActivity?.Resources ?? Resources.System;

        static int GetStatusBarHeight()
        {
            if (!StatusBar.IsVisible) return 0;

            return GetResourceValue("status_bar_height", "dimen", "android");
        }

        static int GetNavigationBarHeight()
        {
            if (HasHardKeys()) return 0;

            return GetResourceValue("navigation_bar_height", "dimen", "android");
        }

        static int GetResourceValue(string name, string defType, string defPackage)
        {
            var resourceId = Resources.GetIdentifier(name, defType, defPackage);
            if (resourceId == 0) return 0;

            return Resources.GetDimensionPixelSize(resourceId);
        }

        static bool HasHardKeys() => ViewConfiguration.Get(UIRuntime.CurrentActivity.ApplicationContext).HasPermanentMenuKey;

        public static partial class StatusBar
        {
            static StatusBar() => isVisible = true;

            static void DoSetBackgroundColor()
            {
                if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop) return;
                if (BackgroundColor == null) return;

                UIRuntime.CurrentActivity.Window.SetStatusBarColor(BackgroundColor.Render());
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