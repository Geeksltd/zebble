namespace Zebble
{
    using UIKit;
    using System;
    using CoreGraphics;
    using System.Collections.Generic;
    using System.Linq;
    using CoreAnimation;
    using Foundation;
    using Olive;

    partial class Animation
    {
        readonly List<Timeline> Timelines = new();

        struct Timeline
        {
            public string Property;
            public CALayer Layer;
            public CAAnimation Animation;

            internal void Register() => Layer.AddAnimation(Animation, Property);
        }

        internal void AddNative(CALayer layer, string property, NSObject toValue)
        {
            CAAnimation anim;

            if (Easing == AnimationEasing.EaseInBounceOut)
                anim = CAKeyFrameAnimation.FromKeyPath(property).Set(x => x.Values = RenderBounceValues());
            else
            {
                // Move the current value to the animation.
                var from = layer.ValueForKeyPath(property.ToNs());
                if (toValue is UIColor && from is null)
                    from ??= UIColor.FromCGColor(layer.BackgroundColor ?? new CGColor(0, 0));

                anim = CABasicAnimation.FromKeyPath(property)
                    .Set(x => x.SetFrom(from))
                    .Set(x => x.SetTo(toValue));
            }

            AddTimeline(property, layer, anim);

            // Immediately set the target value on the object, so after the animation it keeps it.
            layer.SetValueForKeyPath(toValue, property.ToNs());
        }

        void AddTimeline(string property, CALayer layer, CAAnimation anim)
        {
            anim.Duration = Duration.TotalSeconds;
            anim.TimingFunction = RenderTimingFunction();

            anim.RepeatCount = Math.Max(1, Repeats < 0 ? float.PositiveInfinity : Repeats - 1);
            anim.BeginTime = (double)Delay.Milliseconds / 1000;

            Timelines.Add(new Timeline
            {
                Property = property,
                Layer = layer,
                Animation = anim
            });
        }

        [EscapeGCop("These are special numbers.")]
        internal CAMediaTimingFunction RenderTimingFunction()
        {
            var easing = Easing;
            var factor = EasingFactor;

            if (easing == AnimationEasing.EaseIn || easing == AnimationEasing.EaseInBounceOut)
            {
                if (factor == EasingFactor.Cubic)
                    return CAMediaTimingFunction.FromControlPoints(0.55f, 0.055f, 0.675f, 0.19f);
                if (factor == EasingFactor.Quadratic)
                    return CAMediaTimingFunction.FromControlPoints(0.55f, 0.085f, 0.68f, 0.53f);
                if (factor == EasingFactor.Quartic)
                    return CAMediaTimingFunction.FromControlPoints(0.895f, 0.03f, 0.685f, 0.22f);
                if (factor == EasingFactor.Quintic)
                    return CAMediaTimingFunction.FromControlPoints(0.755f, 0.05f, 0.855f, 0.06f);
            }

            if (easing == AnimationEasing.EaseOut)
            {
                if (factor == EasingFactor.Cubic)
                    return CAMediaTimingFunction.FromControlPoints(0.215f, 0.61f, 0.355f, 1f);
                if (factor == EasingFactor.Quadratic)
                    return CAMediaTimingFunction.FromControlPoints(0.25f, 0.46f, 0.45f, 0.94f);
                if (factor == EasingFactor.Quartic)
                    return CAMediaTimingFunction.FromControlPoints(0.165f, 0.84f, 0.44f, 1f);
                if (factor == EasingFactor.Quintic)
                    return CAMediaTimingFunction.FromControlPoints(0.23f, 1f, 0.32f, 1f);
            }

            if (easing == AnimationEasing.EaseInOut)
            {
                if (factor == EasingFactor.Cubic)
                    return CAMediaTimingFunction.FromControlPoints(0.645f, 0.045f, 0.355f, 1f);
                if (factor == EasingFactor.Quadratic)
                    return CAMediaTimingFunction.FromControlPoints(0.455f, 0.03f, 0.515f, 0.955f);
                if (factor == EasingFactor.Quartic)
                    return CAMediaTimingFunction.FromControlPoints(0.77f, 0f, 0.175f, 1f);
                if (factor == EasingFactor.Quintic)
                    return CAMediaTimingFunction.FromControlPoints(0.86f, 0f, 0.07f, 1f);
            }

            if (easing == AnimationEasing.Linear)
                return CAMediaTimingFunction.FromName(CAMediaTimingFunction.Linear);

            return CAMediaTimingFunction.FromName(CAMediaTimingFunction.Default);
        }

        [EscapeGCop("TODO: Check these numbers.")]
        NSObject[] RenderBounceValues()
        {
            var bounciness = 320 / Bounciness;

            return Enumerable.Range(0, 120).Select(t =>
                    210 - Math.Abs(bounciness * Math.Pow(Math.E, -t / 40f) * Math.Cos(bounciness * t)))
                    .Select(x => x.ToNs()).Cast<NSObject>().ToArray();
        }

        internal void Apply(View view)
        {
            if (Timelines.None())
            {
                OnNativeStarted();
                OnNativeCompleted(empty: true);
                return;
            }

            CATransaction.Begin();
            OnNativeStarted();
            CATransaction.CompletionBlock = () => OnNativeCompleted();
            Timelines.Do(x => x.Register());
            CATransaction.Commit();

            Timelines.Clear(); // help GC
        }
    }
}