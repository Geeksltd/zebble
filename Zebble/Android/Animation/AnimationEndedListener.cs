namespace Zebble
{
    using System;
    using Android.Animation;

    internal class AnimationEndedListener : Java.Lang.Object, Animator.IAnimatorListener
    {
        public event Action Ended;

        public void OnAnimationCancel(Animator animation)
        {
            Ended?.Invoke();
            Ended = null;
        }

        public void OnAnimationEnd(Animator animation)
        {
            Ended?.Invoke();
            Ended = null;
        }

        public void OnAnimationRepeat(Animator animation) { }

        public void OnAnimationStart(Animator animation) { }

        protected override void Dispose(bool disposing)
        {
            Ended = null;
            base.Dispose(disposing);
        }
    }

    partial class View
    {
        AnimationEndedListener AnimatorListener;

        internal AnimationEndedListener GetOrCreateListener(Animation animation)
        {
            AnimatorListener = AnimatorListener ?? (AnimatorListener = new AnimationEndedListener());
            AnimatorListener.Ended += () => Thread.UI.Post(() => { if (!IsDisposing) animation.RaiseCompleted(); });

            return AnimatorListener;
        }
    }
}