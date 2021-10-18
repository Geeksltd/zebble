namespace Zebble.AndroidOS
{
    using System;
    using Android.Runtime;
    using Android.Widget;

    public class AndroidBaseContainer : FrameLayout
    {
        View View;
        protected bool IsDisposed;

        public AndroidBaseContainer(View view) : base(Renderer.Context)
        {
            View = view;
            this.ConfigureLayout();
        }

        [Preserve]
        protected AndroidBaseContainer(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) { }

        public override Android.Views.ViewGroup.LayoutParams LayoutParameters
        {
            get => base.LayoutParameters;
            set => base.LayoutParameters = value.FixLayoutSize(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                IsDisposed = true;
                RemoveAllViews();
                View = null;
            }

            base.Dispose(disposing);
        }
    }
}