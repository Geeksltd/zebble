namespace Zebble.IOS
{
    using System;
    using UIKit;

    public class IosBlurBox : IosContainerBase<BlurBox>
    {
        UIVisualEffectView BlurSubview;
        bool IsSubviewAdded;

        public IosBlurBox(BlurBox view) : base(view)
        {
            View.BlurredChanged.HandleOnUI(MaintainBlur);
            CreateBlur();
            MaintainBlur();
        }

        void CreateBlur() => BlurSubview = new UIVisualEffectView
        {
            Alpha = .975f,
            Effect = UIBlurEffect.FromStyle(UIBlurEffectStyle.Regular)
        };

        public void OnBoundsChanged() => BlurSubview.Set(x => x.Frame = Bounds);

        void MaintainBlur()
        {
            if (IsDead(out var view)) return;
            if (view.Blurred)
            {
                if (IsSubviewAdded) return;
                AddSubview(BlurSubview);
                IsSubviewAdded = true;
            }
            else
            {
                if (IsSubviewAdded == false) return;
                BlurSubview.RemoveFromSuperview();
                IsSubviewAdded = false;
            }
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            if (IsSubviewAdded == false) return;
            BringSubviewToFront(BlurSubview);
        }

        bool IsDead(out BlurBox result)
        {
            result = View;
            if (result is null || result.IsDisposing) return true;
            return result.IsDisposing;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                View?.BlurredChanged.RemoveActionHandler(MaintainBlur);
                BlurSubview?.RemoveFromSuperview();
                BlurSubview?.Dispose();
                BlurSubview = null;
            }

            base.Dispose(disposing);
        }
    }
}