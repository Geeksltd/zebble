namespace Zebble.Device
{
    using System;

    public static partial class Keyboard
    {
        public static event Action Shown, Hidden;
        public static float SoftKeyboardHeight;
        public static ScrollView MainScroller;
        public static bool FormScrollDisabled = false;

        internal static void RaiseShown() => Shown?.Invoke();

        internal static void RaiseHidden() => Hidden?.Invoke();
    }
}