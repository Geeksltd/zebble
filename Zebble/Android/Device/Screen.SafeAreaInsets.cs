namespace Zebble.Device
{
    using AndroidX.Core.View;
    using Zebble;

    partial class Screen
    {
        public static partial class SafeAreaInsets
        {
            public static void DoUpdateValues() => DoUpdateValues(insets: null);

            public static void DoUpdateValues(WindowInsetsCompat insets)
            {
                if (OS.IsAtLeast(Android.OS.BuildVersionCodes.M))
                {
                    insets ??= GetRootInsets();

                    if (insets != null)
                    {
                        ApplyInsets(insets);
                        return;
                    }
                }

                ReadFromResources();
            }

            static WindowInsetsCompat GetRootInsets()
            {
                var insets = UIRuntime.CurrentActivity?.Window?.DecorView?.RootWindowInsets;
                if (insets == null) return null;

                return WindowInsetsCompat.ToWindowInsetsCompat(insets);
            }

            static void ApplyInsets(WindowInsetsCompat insets)
            {
                var top = insets.GetInsets(WindowInsetsCompat.Type.StatusBars()).Top;
                var bottom = insets.GetInsets(WindowInsetsCompat.Type.NavigationBars()).Bottom;

                // Yet another hack
                if (bottom == 0) bottom = DisplaySetting.InWindowNavbarHeight;
                else DisplaySetting.InWindowNavbarHeight = bottom;

                Apply(
                    top: top,
                    right: 0,
                    bottom: bottom,
                    left: 0
                );
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