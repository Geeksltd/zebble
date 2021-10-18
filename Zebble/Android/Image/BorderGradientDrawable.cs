using System;
using System.Linq;
using Android.Graphics.Drawables;
using Zebble.Device;
using Olive;

namespace Zebble.AndroidOS
{
    class BorderGradientDrawable : GradientDrawable
    {
        View View;
        internal Color SolidColor;

        public BorderGradientDrawable(View view)
        {
            View = view;

            SetColor();
            SetBorderRadius();

            var hasComplexBorders = View.Effective.ShouldRenderBorderLines() && !View.Border.IsUniform();
            if (!hasComplexBorders) SetBorderLine();
        }

        void SetColor()
        {
            if (View.BackgroundColor is GradientColor color)
            {
                SetOrientation(color.GradientDirection.Render());
                SetColors(color.Items.Select(c => c.Color.Render().ToArgb()).ToArray());
            }
            else
            {
                SetColor((SolidColor = View.BackgroundColor).Render().ToArgb());
            }
        }

        void SetBorderRadius()
        {
            var effective = View.Effective;
            var corners = new[] { effective.BorderRadiusTopLeft(), effective.BorderRadiusTopRight(),
                effective.BorderRadiusBottomRight(), effective.BorderRadiusBottomLeft() };

            if (corners.Distinct().IsSingle()) // Uniform.
            {
                if (corners[0] == 0) return;
                SetCornerRadius(Scale.ToDevice(corners[0]));
            }
            else
            {
                var radii = corners.SelectMany(x => new[] { x, x });
                SetCornerRadii(radii.Select(x => (float)Scale.ToDevice(x)).ToArray());
            }
        }

        void SetBorderLine()
        {
            if (View.Effective.ShouldRenderBorderLines())
                SetStroke(Scale.ToDevice(View.Effective.BorderLeft()),
                    View.Effective.BorderColor().Render());
        }

        protected override void Dispose(bool disposing)
        {
            View = null;
            base.Dispose(disposing);
        }
    }
}