namespace Zebble.AndroidOS
{
    using Android.Views;
    using System.Runtime.CompilerServices;

    public static class AndroidContainerExtentions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConfigureLayout(this ViewGroup @this)
        {
            @this.Focusable = true; // To allow textboxes to give up focus.
            @this.FocusableInTouchMode = true;
            @this.LayoutDirection = LayoutDirection.Ltr;

            try { @this.TransitionGroup = true; } catch { /* Not found?! */ }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ViewGroup.LayoutParams FixLayoutSize(this ViewGroup.LayoutParams value, ViewGroup @this)
        {
            var rootWidth = Device.Scale.ToDevice(Zebble.View.Root.ActualWidth);
            var rootHeight = Device.Scale.ToDevice(Zebble.View.Root.ActualHeight);

            if (value.Width != ViewGroup.LayoutParams.MatchParent &&
                value.Width != ViewGroup.LayoutParams.WrapContent &&
                value.Width == rootWidth)
            {
                value.Width = ViewGroup.LayoutParams.MatchParent;
            }

            if (value.Height != ViewGroup.LayoutParams.MatchParent &&
                value.Height != ViewGroup.LayoutParams.WrapContent)
            {
                if (value.Height == rootHeight) value.Height = ViewGroup.LayoutParams.MatchParent;
                else if (@this is Android.Widget.LinearLayout) value.Height = ViewGroup.LayoutParams.WrapContent;
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FixLayoutSize(int currentSize, Zebble.View zView, bool isHeight = false)
        {
            if (currentSize == -1 || currentSize == -2)
            {
                currentSize = isHeight ? Device.Scale.ToDevice(zView.Height.CurrentValue) : Device.Scale.ToDevice(zView.Width.CurrentValue);
            }

            return currentSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FixFinalLayoutSize(int currentSize, View view, bool isHeight = false)
        {
            var rootWidth = Device.Scale.ToDevice(Zebble.View.Root.ActualWidth);
            var rootHeight = Device.Scale.ToDevice(Zebble.View.Root.ActualHeight);

            if (currentSize != ViewGroup.LayoutParams.MatchParent &&
               currentSize != ViewGroup.LayoutParams.WrapContent &&
                currentSize == rootWidth)
            {
                currentSize = ViewGroup.LayoutParams.MatchParent;
            }

            if (isHeight)
                if (currentSize != ViewGroup.LayoutParams.MatchParent &&
                    currentSize != ViewGroup.LayoutParams.WrapContent)
                {
                    if (currentSize == rootHeight) currentSize = ViewGroup.LayoutParams.MatchParent;
                    else if (view is Android.Widget.LinearLayout) currentSize = ViewGroup.LayoutParams.WrapContent;
                }

            return currentSize;
        }

    }
}
