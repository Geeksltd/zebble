namespace Zebble.IOS
{
    using UIKit;

    public abstract class IosContainerBase<TView> : UIView where TView : View
    {
        protected TView View;
        protected bool IsDisposed;

        public IosContainerBase(TView view) => View = view;

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                IsDisposed = true;
                try { foreach (var subview in Subviews) subview?.Dispose(); }
                catch { /* No loggins is needed */ }
            }

            base.Dispose(disposing);
        }
    }

    public class IosContainer : IosContainerBase<View>
    {
        public IosContainer(View view) : base(view) { }
    }
}