namespace Zebble.Device
{
    using Zebble;

    partial class Screen
    {
        public static partial class SafeAreaInsets
        {
            public static void DoUpdateValues()
            {
                if (OS.IsAtLeast(Android.OS.BuildVersionCodes.M))
                    if (ReadFromInsets()) return;

                ReadFromResources();
            }

            static bool ReadFromInsets()
            {
                var insets = UIRuntime.CurrentActivity?.Window?.DecorView?.RootWindowInsets;
                if (insets == null) return false;

                Apply(
                    top: insets.SystemWindowInsetTop,
                    right: insets.SystemWindowInsetRight,
                    bottom: insets.SystemWindowInsetBottom,
                    left: insets.SystemWindowInsetLeft
                );

                return true;
            }

            static void ReadFromResources()
            {
                Apply(
                    top: GetStatusBarHeight(),
                    right: 0,
                    bottom: GetNavigationBarHeight(),
                    left: 0
                );
            }

            static int GetStatusBarHeight()
                => GetResourceValue("status_bar_height", "dimen", "android");

            static int GetNavigationBarHeight()
                => GetResourceValue("navigation_bar_height", "dimen", "android");

            static int GetResourceValue(string name, string defType, string defPackage)
            {
                var resourceId = Resources.GetIdentifier(name, defType, defPackage);
                if (resourceId == 0) return 0;
                return Resources.GetDimensionPixelSize(resourceId);
            }

            static void Apply(int top, int right, int bottom, int left)
            {
                DisplaySetting.TopInset = top;
                DisplaySetting.RightInset = right;
                DisplaySetting.BottomInset = bottom;
                DisplaySetting.LeftInset = left;

                Top = Scale.ToZebble(DisplaySetting.TopInset);
                Right = Scale.ToZebble(DisplaySetting.RightInset);
                Bottom = Scale.ToZebble(DisplaySetting.BottomInset);
                Left = Scale.ToZebble(DisplaySetting.LeftInset);
            }
        }
    }
}