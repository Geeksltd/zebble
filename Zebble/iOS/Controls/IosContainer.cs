namespace Zebble.IOS
{
    using UIKit;

    public class IosContainer : UIView
    {
        bool IsDisposing;

        public IosContainer(View view) { }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposing)
            {
                IsDisposing = true;
                try { foreach (var subview in Subviews) subview?.Dispose(); }
                catch { /* No loggins is needed */ }
            }

            base.Dispose(disposing);
        }
    }
}