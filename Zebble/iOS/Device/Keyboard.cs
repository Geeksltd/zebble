namespace Zebble.Device
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using CoreGraphics;
    using UIKit;

    partial class Keyboard
    {
        public static bool IsOpen;

        static UIScrollView ExpandedScrollView;
        static CGRect? OriginalRect;
        static CGRect CurrentWindowRect;

        static Keyboard()
        {
            UIKeyboard.Notifications.ObserveWillShow(OnWillShow);
            UIKeyboard.Notifications.ObserveWillHide(OnWillHide);
        }

        static async void OnWillShow(object _, UIKeyboardEventArgs args)
        {
            SoftKeyboardHeight = (float)args.FrameEnd.Height;
            await ExpandScrollerForKeyboard();
            RaiseShown();
            Screen.UpdateLayout(disposeCache: false);
        }

        static async Task ExpandScrollerForKeyboard()
        {
            ScrollView scroller = null;

            var input = View.Root.CurrentDescendants().OfType<TextInput>().FirstOrDefault(x => x.Native()?.IsFirstResponder == true);
            scroller = MainScroller ?? (input?.FindParent<ScrollView>());

            if (scroller == null || input == null) return;

            ExpandedScrollView = (UIScrollView)scroller.Native();
            CurrentWindowRect = ExpandedScrollView.Window != null ? ExpandedScrollView.Window.Frame : UIScreen.MainScreen.Bounds;

            //It makes scroll view not scroll when popup not bigger than the screen size.
            if (ExpandedScrollView.ContentSize.Height + SoftKeyboardHeight < View.Root.ActualHeight) return;

            if (OriginalRect is null) OriginalRect = ExpandedScrollView.Frame;

            nfloat newHeight;
            if (ExpandedScrollView.Window != null) newHeight = OriginalRect.Value.Size.Height - SoftKeyboardHeight + Screen.SafeAreaInsets.Bottom;
            else
                newHeight = OriginalRect.Value.Size.Height + Screen.SafeAreaInsets.Bottom;

            if (OriginalRect.Value.Height < ExpandedScrollView.ContentSize.Height)
                newHeight = CurrentWindowRect.Height - SoftKeyboardHeight;
            ExpandedScrollView.Frame = new CGRect(OriginalRect.Value.X, OriginalRect.Value.Y,
                CurrentWindowRect.Width, newHeight);

            if (FormScrollDisabled)
            {
                IsOpen = true;
                return;
            }

            if (input.CalculateAbsoluteY() >= (View.Root.ActualHeight / 2) - input.ActualHeight)
            {
                if (input.Parent is FormField)
                    await scroller.ScrollToView(input.Parent, animate: true, offset: SoftKeyboardHeight - input.ActualHeight);
                else
                    await scroller.ScrollToView(input, animate: true, offset: SoftKeyboardHeight - input.ActualHeight);
            }

            IsOpen = true;
        }

        static void OnWillHide(object _, UIKeyboardEventArgs args)
        {
            CollapseScrollerForKeyboard();
            RaiseHidden();
            Screen.UpdateLayout(disposeCache: false);
        }

        internal static void CollapseScrollerForKeyboard()
        {
            SoftKeyboardHeight = 0;
            IsOpen = false;

            if (ExpandedScrollView == null || OriginalRect is null) return;

            ExpandedScrollView.Frame = new CGRect(OriginalRect.Value.X, OriginalRect.Value.Y,
                CurrentWindowRect.Width, OriginalRect.Value.Size.Height);

            ExpandedScrollView = null;
            OriginalRect = null;
        }
    }
}