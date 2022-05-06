namespace Zebble.Device
{
    using Zebble;

    partial class Screen
    {
        public static partial class SafeAreaInsets
        {
            public static void DoUpdateValues()
            {
                if (!OS.IsAtLeast((Android.OS.BuildVersionCodes)28)) return;

                var insets = UIRuntime.CurrentActivity?.Window?.DecorView?.RootWindowInsets;

                if (insets == null) return;

                DisplaySetting.TopInset = insets.SystemWindowInsetTop;
                DisplaySetting.RightInset = insets.SystemWindowInsetRight;
                DisplaySetting.BottomInset = insets.SystemWindowInsetBottom;
                DisplaySetting.LeftInset = insets.SystemWindowInsetLeft;

                Top = Scale.ToZebble(DisplaySetting.TopInset);
                Right = Scale.ToZebble(DisplaySetting.RightInset);
                Bottom = Scale.ToZebble(DisplaySetting.BottomInset);
                Left = Scale.ToZebble(DisplaySetting.LeftInset);
            }
        }
    }
}