namespace System
{
    using System.Runtime.CompilerServices;
    using Android.Graphics;
    using Android.Graphics.Drawables;
    using Android.Views;
    using Android.Widget;
    using Zebble.AndroidOS;
    using Zebble.Device;
    using Zebble.Services;
    using static Android.Widget.ImageView;
    using Olive;
    using Linq;

    public static class AndroidRenderExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color Render(this Zebble.Color color) => Render(color, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Zebble.Color ToZebble(this Color color) => new(color.R, color.G, color.B, color.A);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color Render(this Zebble.Color color, float alpha)
        {
            if (color is null) return new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, (int)alpha);
            else return new Color(color.Red, color.Green, color.Blue, color.Alpha);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAnyDown(this MotionEventActions action)
        {
            switch (action)
            {
                case MotionEventActions.Down:
                case MotionEventActions.PointerDown:
                case MotionEventActions.Pointer2Down: return true;
                default: return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAnyUp(this MotionEventActions action)
        {
            switch (action)
            {
                case MotionEventActions.Up:
                case MotionEventActions.PointerUp:
                case MotionEventActions.Pointer2Up: return true;
                default: return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GravityFlags Render(this Zebble.Alignment alignment)
        {
            switch (alignment)
            {
                case Zebble.Alignment.None: return GravityFlags.NoGravity;
                case Zebble.Alignment.TopLeft: return GravityFlags.Left | GravityFlags.Top;
                case Zebble.Alignment.Left: return GravityFlags.CenterVertical | GravityFlags.Left;
                case Zebble.Alignment.BottomLeft: return GravityFlags.Left | GravityFlags.Bottom;
                case Zebble.Alignment.TopRight: return GravityFlags.Right | GravityFlags.Top;
                case Zebble.Alignment.Right: return GravityFlags.Right | GravityFlags.CenterVertical;
                case Zebble.Alignment.BottomRight: return GravityFlags.Right | GravityFlags.Bottom;
                case Zebble.Alignment.TopMiddle: return GravityFlags.Center | GravityFlags.Top;
                case Zebble.Alignment.Middle: return GravityFlags.Center;
                case Zebble.Alignment.BottomMiddle: return GravityFlags.Center | GravityFlags.Bottom;
                case Zebble.Alignment.Justify: return GravityFlags.FillHorizontal;
                default:
                    throw new ArgumentOutOfRangeException("alignment");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LinearLayout.LayoutParams GetFrame(this Zebble.View view)
        {
            return new LinearLayout.LayoutParams(Scale.ToDevice(view.ActualWidth), Scale.ToDevice(view.ActualHeight));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SetRelativeFrame(View native, Zebble.View view)
        {
            var width = Scale.ToDevice(view.ActualWidth);
            var height = Scale.ToDevice(view.ActualHeight);

            native.LayoutParameters = new FrameLayout.LayoutParams(width, height)
            {
                TopMargin = Scale.ToDevice(view.ActualY),
                LeftMargin = Scale.ToDevice(view.ActualX)
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SetAbsoluteFrame(View native, Zebble.View view)
        {
            var width = Scale.ToDevice(view.ActualWidth);
            var height = Scale.ToDevice(view.ActualHeight);

            if (view is Zebble.ScrollView)
            {
                if (view.ActualWidth.AlmostEquals(Zebble.View.Root.ActualWidth)) width = ViewGroup.LayoutParams.MatchParent;
                if (view.ActualHeight.AlmostEquals(Zebble.View.Root.ActualHeight)) height = ViewGroup.LayoutParams.MatchParent;
            }

            native.LayoutParameters = new FrameLayout.LayoutParams(width, height);
            native.TranslationX = Scale.ToDevice(view.ActualX);
            native.TranslationY = Scale.ToDevice(view.ActualY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetFrame(this View native, Zebble.View view)
        {
            if (view.Absolute)
            {
                SetAbsoluteFrame(native, view);
                return;
            }

            if (view is Zebble.TextInput)
            {
                SetRelativeFrame(native, view);
                return;
            }

            if (view is Zebble.Stack)
            {
                var hasTextInput = view.AllDescendents().OfType<Zebble.TextInput>().Any();
                if (view.Parent is Zebble.Stack parent && parent.Direction == Zebble.RepeatDirection.Vertical && hasTextInput)
                {
                    SetRelativeFrame(native, view);
                    return;
                }
            }

            SetAbsoluteFrame(native, view);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetXOffsetToContainerGestureView(this Zebble.View view)
        {
            var native = view.Native as View;

            if (native is null) return 0;

            var parent = native.Parent as View;

            if (native is ScrollView || native is IGestureView) return 0;
            else if (parent is IGestureView) return native.Left;
            else return native.Left + view.parent.GetXOffsetToContainerGestureView();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IGestureView FindContainerGestureView(this View view)
        {
            if (view is null) return null;

            if (view is IGestureView) return view as IGestureView;

            return (view.Parent as View)?.FindContainerGestureView();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetYOffsetToContainerGestureView(this Zebble.View view)
        {
            var native = view?.Native as View;

            if (native is null) return 0;

            if (native is ScrollView || native is IGestureView) return 0;
            if (native.Parent is IGestureView) return native.Top;
            else
            {
                return Scale.ToDevice(view.ActualY) + view.parent.GetYOffsetToContainerGestureView();
            }
        }

        /// <summary>
        /// If the specified object is null, or its native handler is IntPtr.Zero (i.e. is disposed) it returns false.
        /// Also if it's a Bitmap object which is recycled, it returns false.
        /// Otherwise it returns true.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAlive(this Java.Lang.Object obj)
        {
            try
            {
                if (obj is null) return false;
                if (obj.Handle == IntPtr.Zero) return false;
                if ((obj as Bitmap)?.IsRecycled == true) return false;

                return true;
            }
            catch
            {
                /* No logging is needed */
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T TraverseUpToFind<T>(this View view) where T : View
        {
            while (view != null)
            {
                var parent = view.Parent;

                if (parent is T result) return result;
                view = parent as View;
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ShouldRenderBorderLines(this Zebble.Effective effective)
        {
            return effective.BorderLeft() > 0 || effective.BorderRight() > 0 ||
                effective.BorderBottom() > 0 || effective.BorderTop() > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Zebble.Color VisibleBackgroundColor(this Zebble.View view)
        {
            if (view.Effective.HasBackgroundImage()) return Zebble.Colors.Transparent;
            if (!view.BackgroundColor.IsTransparent() || view == Zebble.UIRuntime.RenderRoot)
                return view.BackgroundColor;
            return view.parent?.VisibleBackgroundColor() ?? Zebble.Colors.Transparent;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static GradientDrawable.Orientation Render(this Zebble.GradientColor.Direction direction)
        {
            switch (direction)
            {
                case Zebble.GradientColor.Direction.Right:
                    return GradientDrawable.Orientation.LeftRight;
                case Zebble.GradientColor.Direction.DiagonalDown:
                    return GradientDrawable.Orientation.TlBr;
                case Zebble.GradientColor.Direction.DiagonalUp:
                    return GradientDrawable.Orientation.BlTr;
                case Zebble.GradientColor.Direction.Down:
                default:
                    return GradientDrawable.Orientation.TopBottom;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Android.Text.InputTypes RenderInputType(this Zebble.TextMode textMode)
        {
            switch (textMode)
            {
                case Zebble.TextMode.Password:
                    return Android.Text.InputTypes.ClassText | Android.Text.InputTypes.TextVariationPassword;

                case Zebble.TextMode.PersonName:
                    return Android.Text.InputTypes.ClassText | Android.Text.InputTypes.TextVariationPersonName;

                case Zebble.TextMode.Email:
                    return Android.Text.InputTypes.ClassText | Android.Text.InputTypes.TextVariationEmailAddress | Android.Text.InputTypes.TextFlagNoSuggestions;

                case Zebble.TextMode.Url:
                    return Android.Text.InputTypes.ClassText | Android.Text.InputTypes.TextVariationUri;

                case Zebble.TextMode.Telephone:
                    return Android.Text.InputTypes.ClassPhone;

                case Zebble.TextMode.Integer:
                    return Android.Text.InputTypes.ClassNumber | Android.Text.InputTypes.NumberVariationNormal;

                case Zebble.TextMode.Decimal:
                    return Android.Text.InputTypes.ClassNumber | Android.Text.InputTypes.NumberFlagDecimal;

                default:
                    return Android.Text.InputTypes.ClassText | Android.Text.InputTypes.TextVariationNormal;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScaleType RenderImageAlignment(this Zebble.View view, Bitmap image = null)
        {
            if (view.BackgroundImageStretch == Zebble.Stretch.Default) return ScaleType.Matrix;

            if (view.BackgroundImageStretch == Zebble.Stretch.Fill) return ScaleType.FitXy;

            if (view.BackgroundImageStretch == Zebble.Stretch.AspectFill) return ScaleType.Matrix;

            var imageRatio = image?.Height > 0 ? (image.Width / (double)image.Height) : 1;
            var boxRatio = view.ActualHeight > 0 ? (view.ActualWidth / (double)view.ActualHeight) : 1;

            var isImageWide = imageRatio.Round(3) > boxRatio.Round(3);

            switch (view.BackgroundImageAlignment)
            {
                case Zebble.Alignment.TopLeft: return ScaleType.FitStart;
                case Zebble.Alignment.BottomRight: return ScaleType.FitEnd;
                case Zebble.Alignment.TopMiddle: return isImageWide ? ScaleType.FitStart : ScaleType.FitCenter;
                case Zebble.Alignment.BottomMiddle: return isImageWide ? ScaleType.FitEnd : ScaleType.FitCenter;
                case Zebble.Alignment.Left: return isImageWide ? ScaleType.FitCenter : ScaleType.FitStart;
                case Zebble.Alignment.Right: return isImageWide ? ScaleType.FitCenter : ScaleType.FitEnd;
                case Zebble.Alignment.BottomLeft: return isImageWide ? ScaleType.FitEnd : ScaleType.FitStart;
                case Zebble.Alignment.TopRight: return isImageWide ? ScaleType.FitStart : ScaleType.FitEnd;
                default: return ScaleType.FitCenter;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Android.Animation.ITimeInterpolator RenderInterpolator(this Zebble.Animation animation)
        {
            var easing = animation.Easing;
            var factor = animation.EasingFactor;

            var factorValue = (float)(int)factor / 2;

            switch (easing)
            {
                case Zebble.AnimationEasing.EaseIn:
                    return new Android.Views.Animations.AccelerateInterpolator(factorValue);
                case Zebble.AnimationEasing.EaseOut:
                    return new Android.Views.Animations.DecelerateInterpolator(factorValue);
                case Zebble.AnimationEasing.EaseInOut:
                    return new ZebbleAccelerateDecelerateInterpolator((int)factor);
                case Zebble.AnimationEasing.Linear: return new Android.Views.Animations.LinearInterpolator();
                case Zebble.AnimationEasing.EaseInBounceOut: return new ZebbleBounceInterpolator(animation.Bounciness, animation.Bounces);
                default: throw new NotSupportedException(easing + " is not supported.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Android.Views.Animations.IInterpolator RenderRotationInterpolator(this Zebble.Animation animation)
        {
            var easing = animation.Easing;
            var factor = animation.EasingFactor;

            var factorValue = (float)(int)factor / 2;

            switch (easing)
            {
                case Zebble.AnimationEasing.EaseIn:
                    return new Android.Views.Animations.AccelerateInterpolator(factorValue);
                case Zebble.AnimationEasing.EaseOut:
                    return new Android.Views.Animations.DecelerateInterpolator(factorValue);
                case Zebble.AnimationEasing.EaseInOut:
                    return new ZebbleAccelerateDecelerateInterpolator((int)factor);
                case Zebble.AnimationEasing.Linear: return new Android.Views.Animations.LinearInterpolator();
                case Zebble.AnimationEasing.EaseInBounceOut: return new ZebbleBounceInterpolator(animation.Bounciness, animation.Bounces);
                default: throw new NotSupportedException(easing + " is not supported.");
            }
        }

        public class ZebbleBounceInterpolator : Android.Views.Animations.BounceInterpolator
        {
            readonly double Bounciness, Bounces;

            public ZebbleBounceInterpolator(double bounciness, double bounces)
            {
                Bounciness = bounciness;
                Bounces = bounces;
            }

            public override float GetInterpolation(float time)
            {
                if (time == 0.0f || time == 1.0f) return time;
                else
                {
                    var point = 0.3f * Bounces;
                    var twoPi = (float)(Math.PI * 2.7f);
                    return (float)Math.Pow(2.0f, -(5.0f * Bounciness) * time) *
                        (float)Math.Sin((time - (point / 5.0f)) * twoPi / point) + 1.0f;
                }
            }
        }

        public class ZebbleAccelerateDecelerateInterpolator : Android.Views.Animations.AccelerateDecelerateInterpolator
        {
            readonly int Power;
            public ZebbleAccelerateDecelerateInterpolator(int power) => Power = power;

            public override float GetInterpolation(float time)
            {
                time = (float)Math.Pow(time, Power);
                return (float)(Math.Cos((time + 1) * Math.PI) / 2.0f) + 0.5f;
            }
        }

        public static Zebble.Point GetPoint(this MotionEvent @event, int index = 0)
        {
            return new Zebble.Point(@event.GetX(index), @event.GetY(index));
        }

        public static Android.Views.InputMethods.ImeAction Render(this Zebble.KeyboardActionType keyboardActionType)
        {
            switch (keyboardActionType)
            {
                case Zebble.KeyboardActionType.Go: return Android.Views.InputMethods.ImeAction.Go;
                case Zebble.KeyboardActionType.Done: return Android.Views.InputMethods.ImeAction.Done;
                case Zebble.KeyboardActionType.Next: return Android.Views.InputMethods.ImeAction.Next;
                case Zebble.KeyboardActionType.Search: return Android.Views.InputMethods.ImeAction.Search;
                case Zebble.KeyboardActionType.Send: return Android.Views.InputMethods.ImeAction.Send;
                default: return Android.Views.InputMethods.ImeAction.Unspecified;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Zebble.Point RelativeTo(this Zebble.Point absoluteScreenPoint, Zebble.View target)
        {
            var targetX = Scale.ToDevice(target.CalculateAbsoluteX());
            var targetY = Scale.ToDevice(target.CalculateAbsoluteY());

            return Scale.ToZebble(new Zebble.Point(absoluteScreenPoint.X - targetX, absoluteScreenPoint.Y - targetY));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Zebble.Point AbsoluteTo(this Zebble.Point relativeScreenPoint, Zebble.View target)
        {
            var targetX = Scale.ToDevice(target.CalculateAbsoluteX());
            var targetY = Scale.ToDevice(target.CalculateAbsoluteY());

            return new Zebble.Point(relativeScreenPoint.X + targetX, relativeScreenPoint.Y + targetY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Zebble.Color ToColor(this int androidColor)
        {
            return new Zebble.Color(
                (byte)Color.GetRedComponent(androidColor),
                (byte)Color.GetGreenComponent(androidColor),
                (byte)Color.GetBlueComponent(androidColor),
                (byte)Color.GetAlphaComponent(androidColor));
        }

        internal static Android.Text.InputTypes Render(this Zebble.AutoCapitalizationType autoCapitalization)
        {
            switch (autoCapitalization)
            {
                case Zebble.AutoCapitalizationType.Words:
                    return Android.Text.InputTypes.TextFlagCapWords;
                case Zebble.AutoCapitalizationType.AllCharacters:
                    return Android.Text.InputTypes.TextFlagCapCharacters;
                case Zebble.AutoCapitalizationType.Sentences:
                default:
                    return Android.Text.InputTypes.TextFlagCapSentences;
            }
        }

        internal static Android.Text.InputTypes Render(this Zebble.SpellCheckingType spellChecking)
        {
            switch (spellChecking)
            {
                case Zebble.SpellCheckingType.No:
                    return Android.Text.InputTypes.TextFlagNoSuggestions;
                case Zebble.SpellCheckingType.Default:
                case Zebble.SpellCheckingType.Yes:
                default:
                    return Android.Text.InputTypes.TextFlagAutoCorrect;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Zebble.Size ToDevice(this Zebble.Size zebbleSize) => zebbleSize.Scale(Screen.Density);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Zebble.Point ToZebble(this Zebble.Point native) => Scale.ToZebble(native);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ViewGroup.LayoutParams ToLayoutParams(this Zebble.Size size, Zebble.View view)
        {
            if (view.Parent is Zebble.ScrollView)
                return new LinearLayout.LayoutParams((int)size.Width, (int)size.Height);

            return new FrameLayout.LayoutParams((int)size.Width, (int)size.Height);
        }
    }
}