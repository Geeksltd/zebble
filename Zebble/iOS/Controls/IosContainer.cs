namespace Zebble.IOS
{
    using UIKit;

    public class IosContainer : UIView
    {
        bool IsDisposing;

        public IosContainer(View view) { }

        public override void TouchesMoved(Foundation.NSSet touches, UIEvent evt)
        {
            base.TouchesMoved(touches, evt);
            var touch = touches.AnyObject as UITouch;
            if (touch is null) return;
            var location = touch.LocationInView(UIRuntime.Window.Subviews[0]);
        }

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