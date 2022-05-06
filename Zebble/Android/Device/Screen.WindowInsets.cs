using AndroidX.Core.View;
using Olive;
using System;

namespace Zebble.Device
{
    partial class Screen
    {
        internal static Action<int> OnKeyboardHeightChanged { get; set; }
        internal static int KeyboardHeight;

        internal class ApplyWindowInstetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
        {
            public WindowInsetsCompat OnApplyWindowInsets(Android.Views.View view, WindowInsetsCompat insets)
            {
                SafeAreaInsets.UpdateValues();

                UpdateKeyboardState(insets);
                ReadDimensions();
                UpdateLayout();

                return WindowInsetsCompat.Consumed;
            }

            void UpdateKeyboardState(WindowInsetsCompat insets)
            {
                var navigationBars = insets.GetInsets(WindowInsetsCompat.Type.NavigationBars()).Bottom;
                var ime = insets.GetInsets(WindowInsetsCompat.Type.Ime()).Bottom;

                KeyboardHeight = (ime - navigationBars).LimitMin(0);

                if (KeyboardHeight > 0)
                {
                    Keyboard.SoftKeyboardHeight = Scale.ToZebble(KeyboardHeight);
                    Keyboard.RaiseShown();
                }
                else
                {
                    Keyboard.SoftKeyboardHeight = 0;
                    Keyboard.RaiseHidden();
                }
            }
        }
    }
}
