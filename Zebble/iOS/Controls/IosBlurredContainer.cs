namespace Zebble.IOS
{
    using System;
    using UIKit;

    public class IosBlurredContainer : IosContainerBase<Stack>
    {
        UIVisualEffectView BlurSubview;
        bool IsSubviewAdded;

        public IosBlurredContainer(Stack view) : base(view)
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

        public override void SubviewAdded(UIView uiview)
        {
            base.SubviewAdded(uiview);
            if (IsSubviewAdded == false) return;
            if (uiview == BlurSubview) return;
            BringSubviewToFront(BlurSubview);
        }

        bool IsDead(out Stack result)
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