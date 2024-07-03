namespace Zebble.AndroidOS
{
    using Android.Graphics;
    using Android.Views;
    using Android.Widget;
    using System;
    using System.Threading.Tasks;
    using Zebble.Device;

    /// <summary>
    /// utility to help you to auto scroll to a view when keyboard open or focus happen
    /// </summary>
    public class AutoScrollUtility : IDisposable
    {
        readonly View View;
        ScrollView ParentScrollView;
        int KeyboardHeight;
        bool IsFocused = false;

        public AutoScrollUtility(View view)
        {
            View = view;
            View.FocusChange += FocusChange;
            Screen.OnKeyboardHeightChanged += OnKeyboardHeightChanged;
        }

        void FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            IsFocused = e.HasFocus;
            FocusScroll();
        }

        void TryFindParentScroll()
        {
            ScrollView scrollView = null;
            var parent = View.Parent;
            while (true)
            {
                if (parent == null)
                    break;
                else if (parent is ScrollView scroll)
                {
                    scrollView = scroll;
                    break;
                }
                parent = parent.Parent;
            }

            ParentScrollView = scrollView;
        }

        void FocusScroll()
        {
            if (!IsFocused)
                return;
            if (ParentScrollView == null)
                TryFindParentScroll();
            if (ParentScrollView != null)
            {
                ScrollToView(ParentScrollView);
            }
        }

        public void OnKeyboardHeightChanged(int keyboardHeight)
        {
            KeyboardHeight = keyboardHeight;
            FocusScroll();
        }

        bool IsViewFullyVisible(ScrollView scrollViewParent)
        {
            var pointOfView = GetDeepChildRealPosition(ParentScrollView, View.Parent, View);
            Rect scrollBounds = new();
            scrollViewParent.GetDrawingRect(scrollBounds);

            float top = pointOfView.Y;
            float bottom = top + (View.Height * 2);
            var scrollBottom = scrollBounds.Bottom - KeyboardHeight;
            if (scrollBounds.Top <= top && scrollBottom >= bottom)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /**
         * Used to scroll to the given view.
         *
         * @param scrollViewParent Parent ScrollView
         * @param view View to which we need to scroll.
         */
        void ScrollToView(ScrollView scrollViewParent)
        {
            // Thread.UI.Post Fixed in Motorola_Moto_G8 some times not working as good scrolling
            Thread.UI.Post(() => Thread.Pool.Run(async () =>
            {
                try
                {
                    await Task.Delay(Animation.OneFrame * 3);
                    // Any portion of the imageView, even a single pixel, is within the visible window
                    if (IsViewFullyVisible(scrollViewParent))
                    {
                        return;
                    }

                    var childOffset = GetDeepChildOffset(ParentScrollView, View.Parent, View);
                    childOffset.Y += View.Height * 2;
                    // Scroll to child.
                    scrollViewParent.SmoothScrollTo(0, childOffset.Y);
                }
                catch (ObjectDisposedException)
                {
                    Dispose();
                }
            }));
        }

        /**
         * Used to get deep child offset.
         * <p/>
         * 1. We need to scroll to child in scrollview, but the child may not the direct child to scrollview.
         * 2. So to get correct child position to scroll, we need to iterate through all of its parent views till the main parent.
         *
         * @param mainParent        Main Top parent.
         * @param parent            Parent.
         * @param child             Child.
         * @param accumulatedOffset Accumulated Offset.
         */
        Point GetDeepChildOffset(ViewGroup mainParent, IViewParent parent, View child, Point accumulatedOffset = null)
        {
            if (accumulatedOffset == null)
                accumulatedOffset = new Point();

            var parentGroup = (ViewGroup)parent;
            accumulatedOffset.X += child.Left;
            accumulatedOffset.Y += child.Top;

            if (parentGroup.Equals(mainParent)) return accumulatedOffset;

            return GetDeepChildOffset(mainParent, parentGroup.Parent, parentGroup, accumulatedOffset);
        }

        /**
         * Used to get real child offset even its not visible.
         * <p/>
         * 1. We need to scroll to child in scrollview, but the child may not the direct child to scrollview.
         * 2. So to get correct child position to scroll, we need to iterate through all of its parent views till the main parent.
         *
         * @param mainParent        Main Top parent.
         * @param parent            Parent.
         * @param child             Child.
         * @param accumulatedOffset Accumulated Offset.
         */
        Point GetDeepChildRealPosition(ViewGroup mainParent, IViewParent parent, View child, Point accumulatedOffset = null)
        {
            if (accumulatedOffset == null)
                accumulatedOffset = new Point();

            var parentGroup = (ViewGroup)parent;
            accumulatedOffset.X += (int)child.GetX();
            accumulatedOffset.Y += (int)child.GetY();

            if (parentGroup.Equals(mainParent)) return accumulatedOffset;

            return GetDeepChildRealPosition(mainParent, parentGroup.Parent, parentGroup, accumulatedOffset);
        }

        public void Dispose()
        {
            Screen.OnKeyboardHeightChanged -= OnKeyboardHeightChanged;
            View.FocusChange -= FocusChange;
			GC.SuppressFinalize(this);
        }
    }
}
