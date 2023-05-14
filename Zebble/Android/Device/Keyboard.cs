namespace Zebble.Device
{
    using Android.Views.InputMethods;
    using Context = Android.Content.Context;

    partial class Keyboard
    {
        public static bool IsOpen, KeepKeyboardOpen;

        public static void Show(View view)
        {
            var native = view?.Native();
            if (native is null) return;

            InputManager.ShowSoftInput(native, ShowFlags.Implicit);

            IsOpen = true;
        }

        public static void Hide()
        {
            IsOpen = KeepKeyboardOpen = false;

            var hid = InputManager.HideSoftInputFromWindow(View.Root.Native().WindowToken, HideSoftInputFlags.None);
            if (hid) DoHide();
        }

        internal static void DoHide() => RaiseHidden();

        public static InputMethodManager InputManager => UIRuntime.GetService<InputMethodManager>(Context.InputMethodService);
    }
}