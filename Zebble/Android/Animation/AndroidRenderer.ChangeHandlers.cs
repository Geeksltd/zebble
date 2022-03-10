namespace Zebble
{
    using System;
    using AndroidOS;
    using Android.Graphics.Drawables;
    using Android.Widget;
    using Zebble.Device;
    using Olive;

    partial class Renderer
    {
        void OnBoundsChanged(BoundsChangedEventArgs args)
        {
            if (IsDead(out var view)) return;
            if (Result is null) return;

            var newLeft = Scale.ToDevice(args.X);
            var newTop = Scale.ToDevice(args.Y);
            var newFrame = new Size(args.Width, args.Height).ToDevice().ToLayoutParams(view);

            var currentFrame = Result.LayoutParameters;

            if (currentFrame != null)
            {
                if (currentFrame.Width == newFrame.Width && currentFrame.Height == newFrame.Height)
                    if (Result.TranslationX == newLeft && Result.TranslationY == newTop)
                    {
                        // Nothing is changed
                        return;
                    }
            }

            if (currentFrame == null || !args.Animated())
            {
                Result.LayoutParameters = newFrame;
                Result.TranslationX = newLeft;
                Result.TranslationY = newTop;
                return;
            }

            var leftChanged = Result.TranslationX != newLeft;
            var topChanged = Result.TranslationY != newTop;
            var widthChanged = currentFrame.Width != newFrame.Width;
            var heightChanged = currentFrame.Height != newFrame.Height;

            if (widthChanged || heightChanged)
            {
                var wFrom = currentFrame.Width;
                var hFrom = currentFrame.Height;
                if (wFrom < 0) wFrom = Scale.ToDevice(view.Width.CurrentValue);
                if (hFrom < 0) hFrom = Scale.ToDevice(view.Height.CurrentValue);

                Log.For(this).Warning("Instead of animating Width and Height, animate Scale.");

                if (leftChanged)
                    AnimateFloat(args.Animation, Result.TranslationX, newLeft, v => Result.TranslationX = v);

                if (topChanged)
                    AnimateFloat(args.Animation, Result.TranslationY, newTop, v => Result.TranslationY = v);

                if (widthChanged)
                    AnimateFloat(args.Animation, wFrom, newFrame.Width,
                        v => currentFrame.Width = AndroidContainerExtentions.FixLayoutSize((int)v, view),
                        v => currentFrame.Width = AndroidContainerExtentions.FixFinalLayoutSize((int)v, Result));

                if (heightChanged)
                    AnimateFloat(args.Animation, hFrom, newFrame.Height,
                        v => currentFrame.Height = AndroidContainerExtentions.FixLayoutSize((int)v, view, isHeight: true),
                        v => currentFrame.Height = AndroidContainerExtentions.FixFinalLayoutSize((int)v, Result, isHeight: true));
            }
            else
            {
                if (leftChanged) CreatePropertyAnimator(args.Animation).TranslationX(newLeft);
                if (topChanged) CreatePropertyAnimator(args.Animation).TranslationY(newTop);
            }
        }

        void Transform(TransformationChangedEventArgs args)
        {
            if (IsDead(out var view)) return;

            Result.PivotX = Scale.ToDevice(args.AbsoluteOriginX());
            Result.PivotY = Scale.ToDevice(args.AbsoluteOriginY());

            bool different(float one, float other) => !one.AlmostEquals(other, tolerance: 0.05f);

            if (args.Animated())
            {
                var canUsePropertyAnimator = args.Animation.Repeats == 1 || args.Animation.Repeats == 0;
                if (canUsePropertyAnimator)
                {
                    var ani = CreatePropertyAnimator(args.Animation);
                    if (different(args.RotateZ, Result.Rotation)) ani.Rotation(args.RotateZ);
                    if (different(args.RotateX, -Result.RotationX)) ani.RotationX(-args.RotateX);
                    if (different(args.RotateY, Result.RotationY)) ani.RotationY(args.RotateY);
                    if (different(args.ScaleX, Result.ScaleX)) ani.ScaleX(args.ScaleX);
                    if (different(args.ScaleY, Result.ScaleY)) ani.ScaleY(args.ScaleY);
                }
                else
                {
                    if (different(Result.Rotation, args.RotateZ))
                        AnimateFloat(args.Animation, Result.Rotation, args.RotateZ, v => Result.Rotation = v);

                    if (different(Result.RotationX, -args.RotateX))
                        AnimateFloat(args.Animation, Result.RotationX, -args.RotateX, v => Result.RotationX = v);

                    if (different(Result.RotationY, args.RotateY))
                        AnimateFloat(args.Animation, Result.RotationY, args.RotateY, v => Result.RotationY = v);

                    if (different(Result.ScaleY, args.ScaleY))
                        AnimateFloat(args.Animation, Result.ScaleY, args.ScaleY, v => Result.ScaleY = v);

                    if (different(Result.ScaleX, args.ScaleX))
                        AnimateFloat(args.Animation, Result.ScaleX, args.ScaleX, v => Result.ScaleX = v);
                }
            }
            else
            {
                Result.Rotation = args.RotateZ;
                Result.RotationX = -args.RotateX;
                Result.RotationY = args.RotateY;
                Result.ScaleY = args.ScaleY;
                Result.ScaleX = args.ScaleX;
            }
        }

        void OnZIndexChanged()
        {
            if (IsDead(out var view)) return;
            try
            {
                Result.SetZ(view.ZIndex);
                Result.OutlineProvider = null;
            }
            catch { /* Strange! But no logging is needed */ }
        }

        void OnOpacityChanged(UIChangedEventArgs<float> args)
        {
            if (IsDead(out var view)) return;
            if (Result.Alpha.AlmostEquals(args.Value)) return;

            if (args.Animated())
            {
                if (args.Animation.Repeats == 1) CreatePropertyAnimator(args.Animation).Alpha(args.Value);
                else AnimateFloat(args.Animation, Result.Alpha, args.Value, v => Result.Alpha = v);
            }
            else Result.Alpha = args.Value;
        }

        void OnTextColorChanged(TextColorChangedEventArgs args)
        {
            if (IsDead(out var view)) return;

            if (Result is Android.Widget.TextView control)
            {
                if (args.Animated()) AnimateColor(args.Animation, control.CurrentTextColor.ToColor(), args.Value, control.SetTextColor);
                else control.SetTextColor(args.Value.Render());
            }
        }

        void OnBackgroundColorChanged(UIChangedEventArgs<Color> args)
        {
            if (IsDead(out var view)) return;

            try { Result.OutlineProvider = null; } catch { /*Strange but no logging needed!*/ }

            if (!args.Animated()) SetBackgroundAndBorder();
            else
            {
                var drawable = Result.Background as BorderGradientDrawable;

                if (drawable == null)
                {
                    var oldColor = (Result.Background as ColorDrawable)?.Color.ToZebble() ?? Colors.Transparent;
                    AnimateColor(args.Animation, oldColor, args.Value, Result.SetBackgroundColor);
                }

                else if (view.BackgroundColor is GradientColor)
                {
                    Log.For(this).Error("Cannot animate gradient colour");
                    SetBackgroundAndBorder();
                }
                else
                {
                    var oldColor = drawable.SolidColor ?? Colors.Transparent;
                    AnimateColor(args.Animation, oldColor, args.Value, x => drawable.SetColor(x));
                }
            }
        }
    }
}