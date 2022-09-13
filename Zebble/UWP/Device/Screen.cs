namespace Zebble.Device
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Threading.Tasks;
    using Windows.Foundation.Metadata;
    using Windows.Graphics.Display;
    using Windows.Graphics.Imaging;
    using Windows.Storage.Streams;
    using Windows.UI.ViewManagement;
    using Windows.UI.Xaml.Media.Imaging;
    using Olive;

    partial class Screen
    {
        public static partial class SafeAreaInsets
        {
            public static void DoUpdateValues() { }
        }

        public static partial class StatusBar
        {
            static void DoSetBackgroundColor()
            {
                var backgroundColor = BackgroundColor?.Render();

                // PC customization
                if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.ApplicationView"))
                {
                    var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                    if (titleBar != null)
                    {
                        if (backgroundColor != null)
                        {
                            titleBar.ButtonBackgroundColor = backgroundColor;
                            titleBar.BackgroundColor = backgroundColor;
                            titleBar.InactiveBackgroundColor = backgroundColor;
                            titleBar.ButtonInactiveBackgroundColor = backgroundColor;
                        }
                    }
                }

                // Mobile customization
                if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                {
                    var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                    if (statusBar != null)
                    {
                        if (backgroundColor != null)
                            statusBar.BackgroundColor = backgroundColor;
                    }
                }
            }

            static void DoSetForegroundColor()
            {
                Windows.UI.Color foreColor;

                if (ForegroundColor != null)
                {
                    foreColor = new Windows.UI.Color
                    {
                        A = ForegroundColor.Alpha,
                        B = ForegroundColor.Blue,
                        G = ForegroundColor.Green,
                        R = ForegroundColor.Red
                    };
                }

                // PC customization
                if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.ApplicationView"))
                {
                    var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                    if (titleBar != null)
                    {
                        if (foreColor != null)
                        {
                            titleBar.ForegroundColor = foreColor;
                            titleBar.ButtonForegroundColor = foreColor;
                            titleBar.InactiveForegroundColor = foreColor;
                            titleBar.ButtonInactiveForegroundColor = foreColor;
                        }
                    }
                }

                // Mobile customization
                if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                {
                    var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                    if (statusBar != null)
                    {
                        if (foreColor != null)
                            statusBar.ForegroundColor = foreColor;
                    }
                }
            }

            static void DoSetTransparency()
            {
                // Mobile customization
                if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                {
                    var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                    if (statusBar != null)
                    {
                        if (IsTransparent)
                            statusBar.BackgroundOpacity = 0;
                        else
                            statusBar.BackgroundOpacity = 1;
                    }
                }
            }

            static void DoSetVisibility()
            {
                // Mobile customization
                if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                {
                    var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                    if (statusBar != null)
                    {
                        if (IsVisible) statusBar.ShowAsync().GetResults();
                        else statusBar.HideAsync().GetResults();
                    }
                }
            }

            static void DoSetHasLightContent() { }
        }

        const int SAVE_IMAGE_DPI = 96;
        internal static float? density, hardwareDensity;

        static Screen()
        {
            DisplayInformation.GetForCurrentView().OrientationChanged +=
                (s, e) => OrientationChanged.SignalRaiseOn(Thread.Pool);

            DarkMode = new UISettings().GetColorValue(UIColorType.Background) == Windows.UI.Colors.Black;
        }

        public static float HardwareDensity
        {
            get
            {
                if (hardwareDensity is null)
                    hardwareDensity = (float)DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
                return hardwareDensity.Value;
            }
        }

        public static float Density
        {
            get
            {
                if (density is null)
                    density = Thread.UI.Run(() =>
                    (int)DisplayInformation.GetForCurrentView().ResolutionScale / 100f);

                return density.Value;
            }
        }

        public static DeviceOrientation Orientation
        {
            get
            {
                var orientation = Thread.UI.Run(() => ApplicationView.GetForCurrentView().Orientation);

                if (orientation == ApplicationViewOrientation.Landscape)
                    return DeviceOrientation.Landscape;
                else
                    return DeviceOrientation.Portrait;
            }
        }

        static async Task<FileInfo> DoSaveAsImage(object inputNative)
        {
            var rtb = new RenderTargetBitmap();
            await rtb.RenderAsync((Windows.UI.Xaml.UIElement)inputNative);
            var image = (await rtb.GetPixelsAsync()).ToArray();

            using (var encoded = new InMemoryRandomAccessStream())
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, encoded);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Straight,
                    (uint)rtb.PixelWidth,
                    (uint)rtb.PixelHeight,
                    SAVE_IMAGE_DPI, SAVE_IMAGE_DPI, image);
                await encoder.FlushAsync();

                var result = Device.IO.GetTempRoot().GetFile(Guid.NewGuid() + ".png");
                await result.WriteAllBytesAsync(encoded.AsStreamForRead().ReadAllBytes());

                return result;
            }
        }
    }
}
