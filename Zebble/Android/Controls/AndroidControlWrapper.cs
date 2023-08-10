namespace Zebble.AndroidOS
{
    using System;
    using System.Threading.Tasks;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using Zebble.Device;
    using zbl = Zebble;
    using Olive;

    public interface IZebbleAndroidControl { Task<View> Render(); }

    public class AndroidControlWrapper<TControl> : FrameLayout, IPaddableControl
        where TControl : View, IZebbleAndroidControl
    {
        zbl.View View;
        bool IsDisposed;
        public TControl Control;

        [Preserve]
        public AndroidControlWrapper(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) { }

        public AndroidControlWrapper(zbl.View view, TControl control) : base(Renderer.Context)
        {
            View = view;
            Control = control;
            LayoutDirection = LayoutDirection.Ltr;
            AddView(Control);
        }

        internal View Render()
        {
            Control.Render().GetAlreadyCompletedResult();
            return this;
        }

        public override void SetPadding(int left, int top, int right, int bottom)
        {
            if (View == null) return;
            if (Control?.LayoutParameters is not LayoutParams frame) return;

            if (View is zbl.TextView textView && textView.ShouldIgnoreHorizontalPadding())
            {
                left = right = 0;
            }

            frame.LeftMargin = left;
            frame.TopMargin = top;
            frame.Width = (Scale.ToDevice(View.ActualWidth) - left - right).LimitMin(0);
            frame.Height = (Scale.ToDevice(View.ActualHeight) - top - bottom).LimitMin(0);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                IsDisposed = true;
                Control?.Dispose();
                RemoveAllViews();
                View = null;
                Control = null;
            }

            base.Dispose(disposing);
        }
    }
}