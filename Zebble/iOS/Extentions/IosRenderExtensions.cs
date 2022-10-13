namespace System
{
    using CoreAnimation;
    using CoreGraphics;
    using Foundation;
    using System.Linq;
    using UIKit;
    using Zebble;
    using drawing = Drawing;
    using Olive;

    public static class IosRenderExtensions
    {
        internal static CALayer CurrentLayer;

        public static string Render(this PageTransition transition)
        {
            switch (transition)
            {
                case PageTransition.SlideDown: return "fromBottom";
                case PageTransition.SlideUp: return "fromTop";
                default: return "fromTop";
            }
        }

        public static UIEdgeInsets Render(this Gap gap) => new(gap.Top(), gap.Left(), gap.Bottom(), gap.Right());

        public static Size ToZebble(this CGSize size) => new((float)size.Width, (float)size.Height);

        public static UITextSpellCheckingType RenderSpellChecking(this SpellCheckingType spell)
        {
            switch (spell)
            {
                case SpellCheckingType.Yes:
                    return UITextSpellCheckingType.Yes;
                case SpellCheckingType.No:
                    return UITextSpellCheckingType.No;
                default:
                    return UITextSpellCheckingType.Default;
            }
        }

        public static UITextAutocapitalizationType RenderAutocapitalization(this AutoCapitalizationType cap)
        {
            switch (cap)
            {
                case AutoCapitalizationType.AllCharacters:
                    return UITextAutocapitalizationType.AllCharacters;
                case AutoCapitalizationType.Words:
                    return UITextAutocapitalizationType.Words;
                case AutoCapitalizationType.Sentences:
                    return UITextAutocapitalizationType.Sentences;
                default:
                    return UITextAutocapitalizationType.None;
            }
        }

        public static UITextAutocorrectionType RenderAutoCorrection(this AutoCorrectionType autoCorrection)
        {
            switch (autoCorrection)
            {
                case AutoCorrectionType.Yes:
                    return UITextAutocorrectionType.Yes;
                case AutoCorrectionType.No:
                    return UITextAutocorrectionType.No;
                default:
                    return UITextAutocorrectionType.Default;
            }
        }

        public static CGRect GetFrame(this View view)
        {
            return new CGRect(view.ActualX, view.ActualY, view.ActualWidth, view.ActualHeight);
        }

        public static CGRect GetEffectiveFrame(this View view)
        {
            var effective = view.Effective;
            return new CGRect(
                x: effective.BorderAndPaddingLeft(),
                y: effective.BorderAndPaddingTop(),
                width: view.ActualWidth - view.HorizontalPaddingAndBorder(),
                height: view.ActualHeight - view.VerticalPaddingAndBorder()
            );
        }

        public static bool AlmostEquals(this nfloat @this, nfloat that) => ((float)@this).AlmostEquals((float)that);

        public static NSNumber ToNs(this nfloat value) => ((float)(nfloat.IsNaN(value) ? 0.0 : value)).ToNs();

        public static NSNumber ToNs(this float value) => new(value);

        public static NSString ToNs(this string value) => new(value);

        public static NSNumber ToNs(this double value) => new(value);

        public static NSNumber ToNs(this int value) => new(value);

        public static NSObject ToNs(this CGImage value) => NSObject.FromObject(value);

        internal static drawing.SizeF Render(this Size size) => new(size.Width, size.Height);

        public static UIColor Render(this Color color)
        {
            if (color is null)
                return new UIColor(0, 0, 0, byte.MaxValue);

            return UIColor.FromRGBA(color.Red, color.Green, color.Blue, color.Alpha);
        }

        public static CGColor ToCG(this Color color) => color.Render().CGColor;

        public static NSNumber RenderPercentage(this float percentage) => new(percentage / 100);

        public static UITextAlignment Render(this Alignment alignment)
        {
            switch (alignment)
            {
                case Alignment.None:
                case Alignment.Left:
                case Alignment.TopLeft:
                case Alignment.BottomLeft:
                    return UITextAlignment.Left;

                case Alignment.Right:
                case Alignment.TopRight:
                case Alignment.BottomRight:
                    return UITextAlignment.Right;

                case Alignment.Middle:
                case Alignment.TopMiddle:
                case Alignment.BottomMiddle:
                    return UITextAlignment.Center;

                case Alignment.Justify:
                    return UITextAlignment.Justified;
                default:
                    throw new NotImplementedException();
            }
        }

        public static UIControlContentVerticalAlignment RenderVerticalAlignment(this Alignment alignment)
        {
            switch (alignment)
            {
                case Alignment.Middle:
                case Alignment.Left:
                case Alignment.Right:
                    return UIControlContentVerticalAlignment.Center;

                case Alignment.BottomLeft:
                case Alignment.BottomMiddle:
                case Alignment.BottomRight:
                    return UIControlContentVerticalAlignment.Bottom;

                default:
                    return UIControlContentVerticalAlignment.Top;
            }
        }

        public static drawing.RectangleF Bottom(this drawing.RectangleF bounds, drawing.SizeF size, float marginTop)
        {
            var top = bounds.Location.Y + bounds.Size.Height + marginTop;
            return new drawing.RectangleF(new drawing.PointF(bounds.Location.X, top), size);
        }

        public static void AddSubview(this UIView view, params UIView[] subviews)
        {
            foreach (var subview in subviews)
                view.AddSubview(subview);
        }

        public static UIKeyboardType RenderType(this TextMode mode)
        {
            switch (mode)
            {
                case TextMode.PersonName: return UIKeyboardType.NamePhonePad;
                case TextMode.Email: return UIKeyboardType.EmailAddress;
                case TextMode.Telephone: return UIKeyboardType.PhonePad;
                case TextMode.Url: return UIKeyboardType.Url;
                case TextMode.Integer: return UIKeyboardType.NumberPad;
                case TextMode.Decimal: return UIKeyboardType.DecimalPad;
                default: return UIKeyboardType.Default;
            }
        }

        public static UIReturnKeyType Render(this KeyboardActionType keyboardActionType)
        {
            switch (keyboardActionType)
            {
                case KeyboardActionType.Done: return UIReturnKeyType.Done;
                case KeyboardActionType.Go: return UIReturnKeyType.Go;
                case KeyboardActionType.Next: return UIReturnKeyType.Next;
                case KeyboardActionType.Search: return UIReturnKeyType.Search;
                case KeyboardActionType.Send: return UIReturnKeyType.Send;
                default:
                    return UIReturnKeyType.Default;
            }
        }

        public static CGPoint Render(this Point point) => new(point.X, point.Y);

        public static CALayer SyncBackgroundFrame(this CALayer parentLayer, CGRect newFrame)
        {
            if (newFrame == null) return null;

            if (parentLayer?.Sublayers?.FirstOrDefault() is CAGradientLayer result)
            {
                result.Frame = new CGRect(0, 0, newFrame.Width, newFrame.Height);
                return result;
            }

            return null;
        }

        public static Point GetTouchPoint(this UIGestureRecognizer recognizer, int pointerIndex)
        {
            var point = recognizer.LocationOfTouch(pointerIndex, recognizer.View);
            return new Point((float)point.X, (float)point.Y);
        }

        public static CATransform3D Transform(this View view)
        {
            var xRot = view.RotationX.ToRadians();
            var yRot = view.RotationY.ToRadians();
            var zRot = view.Rotation.ToRadians();

            var result = CATransform3D.Identity;
            result.m34 = 1.0f / 500.0f;

            if (xRot != 0) result = result.Concat(CATransform3D.MakeRotation(xRot, 1, 0, 0));
            if (yRot != 0) result = result.Concat(CATransform3D.MakeRotation(yRot, 0, 1, 0));
            if (zRot != 0) result = result.Concat(CATransform3D.MakeRotation(zRot, 0, 0, 1));

            if (view.ScaleX != 1 || view.ScaleY != 1)
                result = result.Concat(CATransform3D.MakeScale(view.ScaleX, view.ScaleY, 1));

            return result;
        }

        internal static CGPath ToCGPath(this CGRect frame, float[] corners)
        {
            using (var path = new UIBezierPath())
            {
                path.AddLine(corners[0], 0, frame.Width - corners[1], 0);

                var center = new CGPoint(frame.Width - corners[1], corners[1]);
                if (corners[1] > 0)
                    path.AddArc(center, corners[1], 270f.ToRadians(), 0, clockWise: true);
                path.AddLineTo(new CGPoint(frame.Width, frame.Height - corners[2]));

                center = new CGPoint(frame.Width - corners[2], frame.Height - corners[2]);
                if (corners[2] > 0)
                    path.AddArc(center, corners[2], 0, 90f.ToRadians(), clockWise: true);
                path.AddLineTo(new CGPoint(corners[3], frame.Height));

                center = new CGPoint(corners[3], frame.Height - corners[3]);
                if (corners[3] > 0)
                    path.AddArc(center, corners[3], 90f.ToRadians(), 180f.ToRadians(), clockWise: true);
                path.AddLineTo(new CGPoint(0, corners[0]));

                center = new CGPoint(corners[0], corners[0]);
                if (corners[0] > 0)
                    path.AddArc(center, corners[0], 180f.ToRadians(), 270f.ToRadians(), clockWise: true);

                path.ClosePath();

                return path.CGPath;
            }
        }

        internal static void AddLine(this UIBezierPath path, nfloat fromX, nfloat fromY, nfloat toX, nfloat toY)
        {
            path.MoveTo(new CGPoint(fromX, fromY));
            path.AddLineTo(new CGPoint(toX, toY));
        }
    }
}