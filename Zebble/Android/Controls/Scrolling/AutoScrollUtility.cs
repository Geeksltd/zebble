namespace Zebble.AndroidOS
{
    using Android.Graphics;
    using Android.Views;
    using Android.Widget;
    using System;
    using Zebble.Device;

    /// <summary>
    /// utility to help you to auto scroll to a view when keyboard open or focus happen
    /// </summary>
    public class AutoScrollUtility : IDisposable
    {
        readonly View _view;
        ScrollView _parentScrollView;
        public AutoScrollUtility(View view)
        {
            _view = view;
            _view.FocusChange += FocusChange;
            Screen.OnKeyboardHeightChanged += OnKeyboardHeightChanged;
        }

        bool isFocused = false;
        private void FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            isFocused = e.HasFocus;
            //FocusScroll();
        }

        void TryFindParentScroll()
        {
            ScrollView scrollView = null;
            var parent = _view.Parent;
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

            _parentScrollView = scrollView;
        }

        void FocusScroll()
        {
            if (!isFocused)
                return;
            if (_parentScrollView == null)
                TryFindParentScroll();
            if (_parentScrollView != null)
            {
                ScrollToView(_parentScrollView);
            }
        }

        int _keyboardHeight;

        public void OnKeyboardHeightChanged(int keyboardHeight)
        {
            //ShowText($"OnKeyboardHeightChanged {keyboardHeight}");

            _keyboardHeight = keyboardHeight;
            FocusScroll();
        }

        private bool IsViewFullyVisible(ScrollView scrollViewParent)
        {
            Point pointOfView = new Point();
            GetDeepChildRealPosition(_parentScrollView, _view.Parent, _view, pointOfView);
            Rect scrollBounds = new Rect();
            scrollViewParent.GetDrawingRect(scrollBounds);

            float top = pointOfView.Y;
            float bottom = top + _view.Height;
            var scrollBottom = scrollBounds.Bottom - _keyboardHeight;
            if (scrollBounds.Top <= top && scrollBottom >= bottom)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        private bool IsPartOfViewVisible(ScrollView scrollViewParent, View view)
        {
            Rect scrollBounds = new Rect();
            scrollViewParent.GetHitRect(scrollBounds);
            // Any portion of the imageView, even a single pixel, is within the visible window
            return view.GetLocalVisibleRect(scrollBounds);
        }

        /**
         * Used to scroll to the given view.
         *
         * @param scrollViewParent Parent ScrollView
         * @param view View to which we need to scroll.
         */
        private void ScrollToView(ScrollView scrollViewParent)
        {
            try
            {
                // Any portion of the imageView, even a single pixel, is within the visible window
                if (IsViewFullyVisible(scrollViewParent))
                {
                    return;
                }

                Point childOffset = new Point();
                GetDeepChildOffset(_parentScrollView, _view.Parent, _view, childOffset);
                // Scroll to child.
                scrollViewParent.SmoothScrollTo(0, childOffset.Y);
            }
            catch (ObjectDisposedException)
            {
                Dispose();
            }
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
        private void GetDeepChildOffset(ViewGroup mainParent, IViewParent parent, View child, Point accumulatedOffset)
        {
            ViewGroup parentGroup = (ViewGroup)parent;
            accumulatedOffset.X += child.Left;
            accumulatedOffset.Y += child.Top;
            if (parentGroup.Equals(mainParent))
            {
                return;
            }
            GetDeepChildOffset(mainParent, parentGroup.Parent, parentGroup, accumulatedOffset);
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
        private void GetDeepChildRealPosition(ViewGroup mainParent, IViewParent parent, View child, Point accumulatedOffset)
        {
            ViewGroup parentGroup = (ViewGroup)parent;
            accumulatedOffset.X += (int)child.GetX();
            accumulatedOffset.Y += (int)child.GetY();
            if (parentGroup.Equals(mainParent))
            {
                return;
            }
            GetDeepChildRealPosition(mainParent, parentGroup.Parent, parentGroup, accumulatedOffset);
        }

        public void Dispose()
        {
            Screen.OnKeyboardHeightChanged -= OnKeyboardHeightChanged;
            _view.FocusChange -= FocusChange;
        }
    }
}
