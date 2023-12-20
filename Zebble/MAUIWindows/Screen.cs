namespace Zebble.Device
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Windows.Graphics.Display;
    using Windows.UI.ViewManagement;

    partial class Screen
    {
        public static partial class SafeAreaInsets
        {
            public static void DoUpdateValues() { }
        }

        public static partial class StatusBar
        {
            static void DoSetBackgroundColor() => throw new NotSupportedException();

            static void DoSetForegroundColor() => throw new NotSupportedException();

            static void DoSetTransparency() => throw new NotSupportedException();

            static void DoSetVisibility() => throw new NotSupportedException();

            static void DoSetHasLightContent() => throw new NotSupportedException();
        }

        internal static float? density, hardwareDensity;


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

        static async Task<FileInfo> DoSaveAsImage(object inputNative) => throw new NotSupportedException();
    }
}
