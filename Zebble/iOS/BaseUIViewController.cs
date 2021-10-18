namespace Zebble
{
    using System;
    using UIKit;

    public abstract class BaseUIViewController : UIViewController
    {
        protected BaseUIViewController() { }

        protected BaseUIViewController(IntPtr handle) : base(handle)
        {
            if (Device.OS.IsAtLeastiOS(13))
                ModalPresentationStyle = UIModalPresentationStyle.FullScreen;
        }

        public override void ViewDidLoad() => base.ViewDidLoad();

        public override void ViewWillAppear(bool animated) => base.ViewWillAppear(animated);

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            BecomeFirstResponder();
        }

        public override void ViewWillDisappear(bool animated)
        {
            ResignFirstResponder();
            base.ViewWillDisappear(animated);
        }

        public override void ViewDidDisappear(bool animated) => base.ViewDidDisappear(animated);

        public override void MotionEnded(UIEventSubtype motion, UIEvent evt)
        {
            UIRuntime.OnViewMotionEnded.Raise(motion);
        }

        public override UIStatusBarStyle PreferredStatusBarStyle()
        {
            if (Device.Screen.StatusBar.HasLightContent) return UIStatusBarStyle.DarkContent;
            return UIStatusBarStyle.LightContent;
        }
    }
}