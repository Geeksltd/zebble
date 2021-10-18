namespace System
{
    using UIKit;

    public static class UIViewExtensions
    {
        public static UIView FindFirstResponder(this UIView view)
        {
            if (view.IsFirstResponder) return view;

            foreach (var subView in view.Subviews)
            {
                var firstResponder = subView.FindFirstResponder();
                if (firstResponder != null) return firstResponder;
            }

            return default(UIView);
        }

        public static UIView FindSuperviewOfType(this UIView view, UIView stopAt, Type type)
        {
            if (view.Superview != null)
            {
                if (type.IsInstanceOfType(view.Superview)) return view.Superview;

                if (view.Superview != stopAt)
                    return view.Superview.FindSuperviewOfType(stopAt, type);
            }

            return default(UIView);
        }

        public static UIView FindTopSuperviewOfType(this UIView view, UIView stopAt, Type type)
        {
            var superview = view.FindSuperviewOfType(stopAt, type);
            var topSuperView = superview;
            while (superview != null && superview != stopAt)
            {
                superview = superview.FindSuperviewOfType(stopAt, type);
                if (superview != null)
                    topSuperView = superview;
            }

            return topSuperView;
        }

        public static UIMotionEffect SetParallaxIntensity(this UIView view, float parallaxDepth, float? verticalDepth = null)
        {
            if (Zebble.Device.OS.IsAtLeastiOS(7))
            {
                var vertical = verticalDepth ?? parallaxDepth;

                var verticalMotionEffect = new UIInterpolatingMotionEffect("center.y", UIInterpolatingMotionEffectType.TiltAlongVerticalAxis)
                {
                    MinimumRelativeValue = (-vertical).ToNs(),
                    MaximumRelativeValue = vertical.ToNs()
                };

                var horizontalMotionEffect = new UIInterpolatingMotionEffect("center.x", UIInterpolatingMotionEffectType.TiltAlongHorizontalAxis)
                {
                    MinimumRelativeValue = (-parallaxDepth).ToNs(),
                    MaximumRelativeValue = parallaxDepth.ToNs()
                };

                var group = new UIMotionEffectGroup()
                {
                    MotionEffects = new UIMotionEffect[] { horizontalMotionEffect, verticalMotionEffect }
                };

                view.AddMotionEffect(group);

                return group;
            }

            return null;
        }
    }
}