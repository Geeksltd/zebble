namespace Zebble
{
    using System;
    using CoreAnimation;
    using CoreGraphics;
    using UIKit;
    using Olive;

    partial class Renderer
    {
        CAShapeLayer TopBorderLayer, RightBorderLayer, BottomBorderLayer, LeftBorderLayer;

        void RenderBorder()
        {
            if (IsDead(out View view)) return;

            var borderRadius = View.Effective.BorderRadius;
            ApplyCornerRadius(borderRadius);

            var border = View.Effective.Border;
            ApplyBorderLines(border, borderRadius);
        }

        void ApplyCornerRadius(IBorderRadius borderRadius)
        {
            if (IsDead(out View view)) return;
            
            var layer = Layer;
            if (layer is null) return;

            if (!borderRadius.HasValue()) layer.Mask = null;
            else layer.Mask = new CAShapeLayer
            {
                Frame = Result.Bounds,
                BackgroundColor = UIColor.Clear.CGColor,
                MasksToBounds = false,
                FillColor = Colors.Black.ToCG(),
                Path = Result.Bounds.ToCGPath(borderRadius.GetEffectiveRadiusCorners(view))
            };
        }

        void RemoveSideBorders()
        {
            RemoveSideBorder(ref TopBorderLayer);
            RemoveSideBorder(ref BottomBorderLayer);
            RemoveSideBorder(ref LeftBorderLayer);
            RemoveSideBorder(ref RightBorderLayer);
        }

        void ApplyBorderLines(IBorder border, IBorderRadius borderRadius)
        {
            var layer = Layer;
            if (layer is null) return;

            if (border.IsUniform() && !border.HasValue())
            {
                // Same line all over (or no line)
                RemoveSideBorders();
                layer.BorderWidth = border.Left;
                layer.BorderColor = border.Color.ToCG();
            }
            else
            {
                layer.BorderWidth = 0; // We need a shape added to the layer for each one.

                var width = (float)Result.Frame.Width;
                var height = (float)Result.Frame.Height;
                if (width == 0 || height == 0)
                {
                    RemoveSideBorders();
                }
                else
                {
                    DrawTopBorderLine(border, borderRadius);
                    DrawRightBorderLine(border, borderRadius);
                    DrawBottomBorderLine(border, borderRadius);
                    DrawLeftBorderLine(border, borderRadius);
                }
            }
        }

        void DrawTopBorderLine(IBorder border, IBorderRadius borderRadius)
        {
            if (border.Top == 0)
            {
                RemoveSideBorder(ref TopBorderLayer);
                return;
            }

            using (var path = new UIBezierPath())
            {
                var left = borderRadius.TopLeft;
                var right = borderRadius.TopRight;

                if (left > 0) path.AddArc(new CGPoint(left, left), left, 180f.ToRadians(), 270f.ToRadians(), clockWise: true);

                path.AddLine(left, 0, Result.Bounds.Width - right, 0);

                if (right > 0)
                    path.AddArc(new CGPoint(Result.Bounds.Width - right, right), right, 270f.ToRadians(), 0, clockWise: true);

                AddToLayer(ref TopBorderLayer, border.Color, border.Top, path.CGPath);
            }
        }

        void DrawRightBorderLine(IBorder border, IBorderRadius borderRadius)
        {
            if (border.Right == 0)
            {
                RemoveSideBorder(ref RightBorderLayer);
                return;
            }

            using (var path = new UIBezierPath())
            {
                var top = borderRadius.TopRight;
                var bottom = borderRadius.BottomRight;
                if (top > 0) path.AddArc(new CGPoint(Result.Bounds.Width - top, top), top, 270f.ToRadians(), 0, clockWise: true);

                path.AddLine(Result.Bounds.Width, top, Result.Bounds.Width, Result.Bounds.Height - bottom);

                if (bottom > 0)
                    path.AddArc(new CGPoint(Result.Bounds.Width - bottom, Result.Bounds.Height - bottom), bottom, 0, 90f.ToRadians(), clockWise: true);

                AddToLayer(ref RightBorderLayer, border.Color, border.Right, path.CGPath);
            }
        }

        void DrawBottomBorderLine(IBorder border, IBorderRadius borderRadius)
        {
            if (border.Bottom == 0)
            {
                RemoveSideBorder(ref BottomBorderLayer);
                return;
            }

            var height = Result.Bounds.Height;
            var width = Result.Bounds.Width;

            using (var path = new UIBezierPath())
            {
                var left = borderRadius.BottomLeft;
                var right = borderRadius.BottomRight;

                if (left > 0) path.AddArc(new CGPoint(left, height - left), left, 90f.ToRadians(), 180f.ToRadians(), clockWise: true);

                path.AddLine(left, height, width - right, height);

                if (right > 0)
                {
                    path.MoveTo(new CGPoint(width, Result.Bounds.Height - right));
                    path.AddArc(new CGPoint(width - right, height - right), right, 0, 90f.ToRadians(), clockWise: true);
                }

                AddToLayer(ref BottomBorderLayer, border.Color, border.Bottom, path.CGPath);
            }
        }

        void DrawLeftBorderLine(IBorder border, IBorderRadius borderRadius)
        {
            if (border.Left == 0)
            {
                RemoveSideBorder(ref LeftBorderLayer);
                return;
            }

            using (var path = new UIBezierPath())
            {
                var top = borderRadius.TopLeft;
                var bottom = borderRadius.BottomLeft;

                if (top > 0) path.AddArc(new CGPoint(top, top), top, 180f.ToRadians(), 270f.ToRadians(), clockWise: true);

                path.AddLine(0, top, 0, Result.Bounds.Height - bottom);

                if (bottom > 0)
                {
                    path.MoveTo(new CGPoint(bottom, Result.Bounds.Height));
                    path.AddArc(new CGPoint(bottom, Result.Bounds.Height - bottom), bottom, 90f.ToRadians(), 180f.ToRadians(), clockWise: true);
                }

                AddToLayer(ref LeftBorderLayer, border.Color, border.Left, path.CGPath);
            }
        }

        void AddToLayer(ref CAShapeLayer layer, Color color, float width, CGPath path)
        {
            if (layer == null)
                Layer?.AddSublayer(layer = new CAShapeLayer());

            layer.Frame = Result.Bounds;
            layer.FillColor = UIColor.Clear.CGColor;
            layer.BackgroundColor = UIColor.Clear.CGColor;
            layer.StrokeColor = color.ToCG();
            layer.LineWidth = width;
            layer.Path = path;
        }

        void RemoveSideBorder(ref CAShapeLayer layer)
        {
            layer?.RemoveFromSuperLayer();
            layer?.Dispose();
            layer = null;
        }
    }
}