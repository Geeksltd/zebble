using AndroidX.Core.View;
using Olive;
using System;
using System.Threading.Tasks;

namespace Zebble.Device
{
    partial class Screen
    {
        internal static Action<int> OnKeyboardHeightChanged { get; set; }
        internal static int KeyboardHeight;

        internal abstract class AwaitableListener : Java.Lang.Object
        {
            protected readonly TaskCompletionSource<bool> CompletionSource = new();
        }

        internal class WindowInsetsApplierListener : AwaitableListener, IOnApplyWindowInsetsListener
        {
            public WindowInsetsCompat OnApplyWindowInsets(Android.Views.View view, WindowInsetsCompat insets)
            {
                SafeAreaInsets.DoUpdateValues(insets);
                UpdateKeyboardState(insets);
                PostLoadConfiguration();

                CompletionSource.TrySetResult(true);

                return WindowInsetsCompat.Consumed;
            }

            public async Task WaitForCompletion(Android.Views.View view)
            {
                ViewCompat.SetOnApplyWindowInsetsListener(view, this);
                ViewCompat.RequestApplyInsets(view);
                await CompletionSource.Task;
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
