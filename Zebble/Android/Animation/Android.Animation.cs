namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using Android.Animation;
    using Android.Views;
    using Zebble.AndroidOS;
    using Zebble.Device;
    using Olive;

    partial class Animation
    {
        internal readonly List<Animator> SlowAnimators = new List<Animator>();
        internal ViewPropertyAnimator ViewPropertyAnimator;
        bool IsAlreadyCompleted;

        internal void RaiseCompleted(bool empty = false)
        {
            if (IsAlreadyCompleted) return;
            else IsAlreadyCompleted = true;

            OnNativeCompleted(empty);
            ViewPropertyAnimator = null;
            SlowAnimators.Clear();
        }

        internal void Apply(View view)
        {
            var animator = ViewPropertyAnimator;

            if (animator == null && SlowAnimators.None())
            {
                OnNativeStarted();
                RaiseCompleted(empty: true);
                return;
            }

            if (animator != null)
            {
                var almostFullWidth = 0.9 * Scale.ToDevice(View.Root.ActualWidth);
                var native = view.Native();
                if (native == null) return;
                if (native.Width > almostFullWidth && Gpu.CanAnimate(native))
                    animator.WithLayer(); // Large, but not too large. Use hardware layer.

                animator.Start();
            }

            SlowAnimators.Do(x => x.Start());
            OnNativeStarted();
        }
    }
}