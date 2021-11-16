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

                Top = Scale.ToZebble(insets.SystemWindowInsetTop) - StatusBar.Height;
                Bottom = Scale.ToZebble(insets.SystemWindowInsetBottom);
                Left = Scale.ToZebble(insets.SystemWindowInsetLeft);
                Right = Scale.ToZebble(insets.SystemWindowInsetRight);

                DisplaySetting.TopInset = insets.SystemWindowInsetTop;
                DisplaySetting.BottomInset = insets.SystemWindowInsetBottom;
            }
        }
    }
}