﻿namespace Zebble.Device
{
    using Android.Content.Res;
    using Android.OS;
    using Android.Views;
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

        internal static void PreLoadConfiguration()
        {
            Resources = GetResources();
            HardwareDensity = DisplayMetrics.Density;
            Density = DisplayMetrics.ScaledDensity.LimitWithin(HardwareDensity * .8f, HardwareDensity * 1.25f);

            DarkMode = (Resources.Configuration.UiMode & UiMode.NightMask) == UiMode.NightYes;
        }

        internal static void PostLoadConfiguration()
        {
            var realSize = new Android.Graphics.Point();
            Display.GetRealSize(realSize);

            DisplaySetting.WindowWidth = realSize.X;
            DisplaySetting.WindowHeight = realSize.Y - KeyboardHeight;

            DisplaySetting.HardwareWidth = realSize.X;
            DisplaySetting.HardwareHeight = realSize.Y;

            DisplaySetting.RealWidth = realSize.X;
            DisplaySetting.RealHeight = realSize.Y;

            DisplaySetting.OutOfWindowStatusBarHeight = DisplaySetting.TopInset;
            StatusBar.Height = Scale.ToZebble(DisplaySetting.OutOfWindowStatusBarHeight);

            DisplaySetting.OutOfWindowNavbarHeight = DisplaySetting.BottomInset;
            NavigationBarHeight = Scale.ToZebble(DisplaySetting.OutOfWindowNavbarHeight);

            ConfigureSize(
                widthProvider: () => Scale.ToZebble(DisplaySetting.WindowWidth),
                heightProvider: () => Scale.ToZebble(DisplaySetting.WindowHeight)
            );
        }

        static Resources GetResources() => UIRuntime.CurrentActivity?.Resources ?? Resources.System;

        public static partial class StatusBar
        {
            static StatusBar() => isVisible = true;

            static void DoSetBackgroundColor()
            {
                if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop) return;
                if (BackgroundColor == null) return;

                UIRuntime.CurrentActivity.Window.SetStatusBarColor(BackgroundColor.Render());
                UIRuntime.CurrentActivity.Window.SetNavigationBarColor(BackgroundColor.Render());
            }

            static void DoSetForegroundColor()
            {
                if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop) return;
                if (ForegroundColor == null) return;

                UIRuntime.CurrentActivity.Window.SetTitleColor(ForegroundColor.Render());
                UIRuntime.CurrentActivity.Window.SetNavigationBarColor(ForegroundColor.Render());
            }

            static void DoSetTransparency()
            {
                if (IsTransparent)
                {
                    UIRuntime.CurrentActivity.Window.AddFlags(WindowManagerFlags.TranslucentStatus);
                    UIRuntime.CurrentActivity.Window.AddFlags(WindowManagerFlags.TranslucentNavigation);
                }
                else
                {
                    UIRuntime.CurrentActivity.Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
                    UIRuntime.CurrentActivity.Window.ClearFlags(WindowManagerFlags.TranslucentStatus);
                    UIRuntime.CurrentActivity.Window.ClearFlags(WindowManagerFlags.TranslucentNavigation);
                }
            }

            static void DoSetVisibility()
            {
                if (IsVisible)
                    UIRuntime.CurrentActivity.Window.ClearFlags(WindowManagerFlags.Fullscreen);
                else
                    UIRuntime.CurrentActivity.Window.AddFlags(WindowManagerFlags.Fullscreen);
            }

            static void DoSetHasLightContent()
            {
                if (Build.VERSION.SdkInt < BuildVersionCodes.M) return;
                UIRuntime.CurrentActivity.Window.DecorView.SystemUiVisibility = HasLightContent ? (StatusBarVisibility)(SystemUiFlags.LightStatusBar ^ SystemUiFlags.LightNavigationBar) : 0;
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
                    imageBitmap.Recycle();

                    var bitmapData = stream.ToArray();
                    var result = IO.CreateTempFile(".png");
                    await result.WriteAllBytesAsync(bitmapData);

                    return result;
                }
            }
        }
    }
}