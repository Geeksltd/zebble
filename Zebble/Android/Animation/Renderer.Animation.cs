namespace Zebble
{
    using System;
    using Android.Animation;
    using Android.Views;
    using Olive;

    partial class Renderer
    {
        ViewPropertyAnimator CreatePropertyAnimator(Animation ani)
        {
            if (ani.ViewPropertyAnimator != null)
                return ani.ViewPropertyAnimator;

            return ani.ViewPropertyAnimator = Result.Animate()
                  .SetInterpolator(ani.RenderInterpolator())
                  .SetDuration((long)ani.Duration.TotalMilliseconds)
                  .SetListener(View.GetOrCreateListener(ani));
        }

        void AnimateFloat(Animation animation, float from, float to, Action<float> frameAction, Action<float> completeAction = null)
        {
            if (IsDead(out var view) || from.AlmostEquals(to)) return;
            Animate(ValueAnimator.OfFloat(from, to), animation, to, frameAction, completeAction);
        }

        ValueAnimator AnimateColor(Animation animation, Color from, Color to, Action<Android.Graphics.Color> frameAction)
        {
            if (IsDead(out var view)) return null;
            if (from == to) return null;

            var fromColor = from.Render().ToArgb();
            var toColor = to.Render().ToArgb();

            var ani = ValueAnimator.OfObject(new ArgbEvaluator(), fromColor, toColor);
            Animate(ani, animation, toColor, v => frameAction(new Android.Graphics.Color(v)));
            return ani;
        }

        void Animate<TValue>(ValueAnimator ani, Animation animation, TValue to, Action<TValue> frameAction, Action<TValue> completeAction = null)
        {
            void applyFrame(TValue value, bool isUpdate = true)
            {
                try
                {
                    if (isUpdate) frameAction(value);
                    else if (completeAction != null) completeAction(value);
                    else frameAction(value);

                    Result?.Invalidate();
                }
                catch (ObjectDisposedException)
                {
                    // The object is dead since.
                }
            }

            ani.SetInterpolator(animation.RenderInterpolator());
            ani.SetDuration((long)animation.Duration.TotalMilliseconds);
            ani.AddListener(View.GetOrCreateListener(animation));

            ani.Update += (arg, e) =>
            {
                if (IsDead(out var vi)) return;
                if (ani.RepeatCount == -1 && vi.Opacity == 0) return;
                applyFrame(e.Animation.AnimatedValue.ToString().To<TValue>());
            };

            ani.RepeatCount = animation.Repeats < 0 ? -1 /*Infinite*/ : animation.Repeats - 1;
            ani.StartDelay = animation.Delay.Milliseconds;
            ani.AnimationEnd += (_, e) => applyFrame(to, isUpdate: false);
            animation.SlowAnimators.Add(ani);
        }
    }
}