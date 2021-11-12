namespace Zebble.Device
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Foundation;
    using MediaAccessibility;
    using UIKit;
    using Olive;

    partial class Screen
    {
        public static partial class SafeAreaInsets
        {
            public static void DoUpdateValues()
            {
                if (OS.IsAtLeastiOS(11))
                {
                    var nativeInsets = UIApplication.SharedApplication.KeyWindow.SafeAreaInsets;
                    Top = (float)nativeInsets.Top;
                    Right = (float)nativeInsets.Right;
                    Bottom = (float)nativeInsets.Bottom;
                    Left = (float)nativeInsets.Left;
                }
            }
        }

        public static partial class StatusBar
        {
            static bool? hasLightContent = null;
            public static bool HasLightContent
            {
                get => hasLightContent ?? !DarkMode;
                set
                {
                    if (hasLightContent == value) return;

                    hasLightContent = value;

                    // This will make UIViewController.PreferredStatusBarStyle to be re-evaluated.
                    CoreFoundation.DispatchQueue.MainQueue.DispatchAsync(
                        () => UIApplication.SharedApplication?.KeyWindow?
                                           .RootViewController?.SetNeedsStatusBarAppearanceUpdate()
                    );
                }
            }

            static void DoSetBackgroundColor()
            {
                var statusBar = UIApplication.SharedApplication.ValueForKey(new NSString("statusBar")) as UIView;

                if (statusBar.RespondsToSelector(new ObjCRuntime.Selector("setTintColor:")))
                    if (statusBar.RespondsToSelector(new ObjCRuntime.Selector("setBackgroundColor:")))
                    {
                        if (BackgroundColor != null)
                        {
                            statusBar.BackgroundColor = new UIColor(
                                BackgroundColor.Red,
                                BackgroundColor.Green,
                                BackgroundColor.Blue,
                                BackgroundColor.Alpha
                            );
                        }
                    }
            }

            static void DoSetForegroundColor() { }

            static void DoSetTransparency()
            {
                if (IsTransparent)
                    BackgroundColor = Colors.Transparent;
            }

            static void DoSetVisibility() => UIApplication.SharedApplication.SetStatusBarHidden(hidden: !IsVisible, animated: true);
        }

        public static readonly float HardwareDensity = (float)UIScreen.MainScreen.Scale;

        public static readonly float Density;

        static Screen()
        {
            var ignore = MACaptionAppearanceBehavior.UseValue;
            Density = (float)MACaptionAppearance.GetRelativeCharacterSize(MACaptionAppearanceDomain.User, ref ignore)
                * HardwareDensity;

            Orientation = FindCurrentOrientation();

            NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidChangeStatusBarOrientationNotification,
                n =>
                {
                    Orientation = FindCurrentOrientation();
                    OrientationChanged.SignalRaiseOn(Thread.Pool);
                });
        }

        public static DeviceOrientation Orientation { get; private set; }

        static DeviceOrientation FindCurrentOrientation()
        {
            switch (UIApplication.SharedApplication.StatusBarOrientation)
            {
                case UIInterfaceOrientation.Portrait:
                case UIInterfaceOrientation.PortraitUpsideDown:
                    return DeviceOrientation.Portrait;
                default:
                    return DeviceOrientation.Landscape;
            }
        }

        static async Task<FileInfo> DoSaveAsImage(object input)
        {
            var inputNative = (UIView)input;

            UIGraphics.BeginImageContextWithOptions(inputNative.Bounds.Size, inputNative.Opaque, 1);
            inputNative.DrawViewHierarchy(inputNative.Frame, afterScreenUpdates: true);

            var img = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();

            using (var imageData = img.AsPNG())
            {
                var myByteArray = new byte[imageData.Length];
                System.Runtime.InteropServices.Marshal.Copy(imageData.Bytes, myByteArray, 0,
                    Convert.ToInt32(imageData.Length));

                var result = Device.IO.CreateTempFile(".png");
                await result.WriteAllBytesAsync(myByteArray);

                return result;
            }
        }
    }
}