namespace Zebble.IOS
{
    using CoreGraphics;
    using Foundation;
    using System;
    using System.Linq;
    using UIKit;

    class UIFirstTouchGestureRecognizer : UIGestureRecognizer
    {
        Action<CGPoint> Handler;

        public UIFirstTouchGestureRecognizer(Action<CGPoint> handler) => Handler = handler;

        public override void TouchesBegan(NSSet touches, UIEvent evt) => Handler?.Invoke(LocationOfTouch(0, View));

        protected override void Dispose(bool disposing)
        {
            Handler = null;
            base.Dispose(disposing);
        }
    }
}