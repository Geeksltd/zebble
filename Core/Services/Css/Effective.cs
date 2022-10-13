namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Olive;

    internal class Effective
    {
        readonly View View;

        public Effective(View view) => View = view;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float BorderTop() => View.Style.border?.top ?? View.Css.border?.top ?? 0;

        public float BorderAndPaddingTop() => BorderTop() + View.Padding.Top();

        public float BorderAndPaddingBottom() => BorderBottom() + View.Padding.Bottom();

        public float BorderAndPaddingLeft() => BorderLeft() + View.Padding.Left();

        public float BorderAndPaddingRight() => BorderRight() + View.Padding.Right();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float BorderBottom() => View.Style.border?.bottom ?? View.Css.border?.bottom ?? 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float BorderLeft() => View.Style.border?.left ?? View.Css.border?.left ?? 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float BorderRight() => View.Style.border?.right ?? View.Css.border?.right ?? 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float BorderRadiusTopLeft() => View.Style.borderRadius?.TopLeft ?? View.Css.borderRadius?.TopLeft ?? 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float BorderRadiusTopRight() => View.Style.borderRadius?.TopRight ?? View.Css.borderRadius?.TopRight ?? 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float BorderRadiusBottomLeft() => View.Style.borderRadius?.BottomLeft ?? View.Css.borderRadius?.BottomLeft ?? 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float BorderRadiusBottomRight() => View.Style.borderRadius?.BottomRight ?? View.Css.borderRadius?.BottomRight ?? 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float BorderTotalHorizontal() => BorderLeft() + BorderRight();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float BorderTotalVertical() => BorderTop() + BorderBottom();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasBorder()
        {
            return BorderLeft() > 0 || BorderRight() > 0 || BorderBottom() > 0 || BorderTop() > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasBorderRadius()
        {
            return GetBorderRadiusCorners().Any(v => v > 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<float> GetBorderRadiusCorners()
        {
            yield return BorderRadiusTopLeft();
            yield return BorderRadiusTopRight();
            yield return BorderRadiusBottomLeft();
            yield return BorderRadiusBottomRight();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color BorderColor() => View.Style.border?.color ?? View.Css.border?.color ?? Colors.Black;

        public IBorder Border
        {
            get
            {
                return new Border
                {
                    left = View.Style.border?.left ?? View.Css.border?.left ?? 0,
                    right = View.Style.border?.right ?? View.Css.border?.right ?? 0,
                    top = View.Style.border?.top ?? View.Css.border?.top ?? 0,
                    bottom = View.Style.border?.bottom ?? View.Css.border?.bottom ?? 0,
                    color = View.Style.border?.color ?? View.Css.border?.color ?? Colors.Black
                };
            }
        }

        public IBorderRadius BorderRadius
        {
            get
            {
                return new BorderRadius
                {
                    TopLeft = View.Style.borderRadius?.TopLeft ?? View.Css.borderRadius?.TopLeft ?? 0,
                    TopRight = View.Style.borderRadius?.TopRight ?? View.Css.borderRadius?.TopRight ?? 0,
                    BottomLeft = View.Style.borderRadius?.BottomLeft ?? View.Css.borderRadius?.BottomLeft ?? 0,
                    BottomRight = View.Style.borderRadius?.BottomRight ?? View.Css.borderRadius?.BottomRight ?? 0,
                };
            }
        }

        public bool HasAnyBorderRadius()
        {
            if (BorderRadiusBottomLeft() > 0) return true;
            if (BorderRadiusBottomRight() > 0) return true;
            if (BorderRadiusTopLeft() > 0) return true;
            if (BorderRadiusTopRight() > 0) return true;

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IFont Font()
        {
            return new Font
            {
                size = View.Style.font?.size ?? View.Css.font?.size ?? 10,
                name = (View.Style.font?.name ?? View.Css.font?.name).Or(Zebble.Font.DefaultSystemFont),
                bold = (View.Style.font?.bold ?? View.Css.font?.bold) ?? false,
                italic = (View.Style.font?.italic ?? View.Css.font?.italic) ?? false
            };
        }

        internal bool HasBackgroundImage()
        {
            if (View.BackgroundImagePath.HasValue()) return true;
            return View.BackgroundImageData?.Any() == true;
        }
    }
}