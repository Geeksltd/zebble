namespace Zebble
{
    using System;

    public static class StylesheetPropertyExtensions
    {
        public static Length.PercentageLengthRequest Percent(this int value) => new Length.PercentageLengthRequest(value);

        public static Length.PercentageLengthRequest Percent(this float value) => new Length.PercentageLengthRequest(value);

        public static Length.PercentageLengthRequest Percent(this double value) => new Length.PercentageLengthRequest((float)value);

        public static Stylesheet X(this Stylesheet sheet, Length.LengthRequest value) => sheet.Set(x => x.X = value);

        public static Stylesheet Y(this Stylesheet sheet, Length.LengthRequest value) => sheet.Set(x => x.Y = value);

        public static Stylesheet Height(this Stylesheet sheet, Length.LengthRequest value) => sheet.Set(x => x.Height = value);

        public static Stylesheet Width(this Stylesheet sheet, Length.LengthRequest value) => sheet.Set(x => x.Width = value);

        public static Stylesheet X(this Stylesheet sheet, float value) => sheet.Set(x => x.X = value);

        public static Stylesheet Y(this Stylesheet sheet, float value) => sheet.Set(x => x.Y = value);

        public static Stylesheet Height(this Stylesheet sheet, float value) => sheet.Set(x => x.Height = value);

        public static Stylesheet Width(this Stylesheet sheet, float value) => sheet.Set(x => x.Width = value);

        public static Stylesheet Size(this Stylesheet sheet, float value) => sheet.Width(value).Height(value);

        public static Stylesheet X(this Stylesheet sheet, int value) => sheet.Set(x => x.X = value);

        public static Stylesheet Y(this Stylesheet sheet, int value) => sheet.Set(x => x.Y = value);

        public static Stylesheet Height(this Stylesheet sheet, int value) => sheet.Set(x => x.Height = value);

        public static Stylesheet Width(this Stylesheet sheet, int value) => sheet.Set(x => x.Width = value);

        public static Stylesheet Size(this Stylesheet sheet, int value) => sheet.Width(value).Height(value);

        public static Stylesheet ZIndex(this Stylesheet sheet, int value) => sheet.Set(x => x.ZIndex = value);

        public static Stylesheet ScaleX(this Stylesheet sheet, float? value) => sheet.Set(x => x.ScaleX = value);

        public static Stylesheet ScaleY(this Stylesheet sheet, float? value) => sheet.Set(x => x.ScaleY = value);

        public static Stylesheet Visible(this Stylesheet sheet, bool value = true) => sheet.Set(x => x.Visible = value);

        public static Stylesheet Ignored(this Stylesheet sheet, bool value = true) => sheet.Set(x => x.Ignored = value);

        /// <summary>Sets Visible to false.</summary>
        public static Stylesheet Hide<TView>(this Stylesheet view) => view.Visible(value: false);

        public static Stylesheet Absolute(this Stylesheet sheet, bool value = true) => sheet.Set(x => x.Absolute = value);

        public static Stylesheet Opacity(this Stylesheet sheet, float value) => sheet.Set(x => x.Opacity = value);

        public static Stylesheet Background(this Stylesheet sheet, Color color = null, string path = null, Alignment? alignment = null, Stretch? stretch = null)
        {
            if (color != null) sheet.BackgroundColor = color;
            if (path != null) sheet.BackgroundImagePath = path;
            if (alignment.HasValue) sheet.BackgroundImageAlignment = alignment.Value;
            if (stretch.HasValue) sheet.BackgroundImageStretch = stretch.Value;

            return sheet;
        }

        public static Stylesheet Margin(this Stylesheet sheet, Length.LengthRequest all)
        {
            sheet.Margin.Left = all;
            sheet.Margin.Right = all;
            sheet.Margin.Top = all;
            sheet.Margin.Bottom = all;

            return sheet;
        }

        public static Stylesheet Margin(this Stylesheet sheet, Length.LengthRequest vertical, Length.LengthRequest horizontal)
        {
            sheet.Margin.Top = sheet.Margin.Bottom = vertical;
            sheet.Margin.Left = sheet.Margin.Right = horizontal;

            return sheet;
        }

        public static Stylesheet Margin(this Stylesheet sheet, float? all = null, float? horizontal = null, float? vertical = null, float? top = null, float? right = null, float? bottom = null, float? left = null)
        {
            var finalLeft = left ?? horizontal ?? all;
            var finalRight = right ?? horizontal ?? all;
            var finalTop = top ?? vertical ?? all;
            var finalBottom = bottom ?? vertical ?? all;

            if (finalLeft.HasValue) sheet.Margin.Left = finalLeft;
            if (finalRight.HasValue) sheet.Margin.Right = finalRight;
            if (finalTop.HasValue) sheet.Margin.Top = finalTop;
            if (finalBottom.HasValue) sheet.Margin.Bottom = finalBottom;

            return sheet;
        }

        public static Stylesheet Padding(this Stylesheet sheet, float? all = null, float? horizontal = null, float? vertical = null, float? top = null, float? right = null, float? bottom = null, float? left = null)
        {
            var finalLeft = left ?? horizontal ?? all;
            var finalRight = right ?? horizontal ?? all;
            var finalTop = top ?? vertical ?? all;
            var finalBottom = bottom ?? vertical ?? all;

            if (finalLeft.HasValue) sheet.Padding.Left = finalLeft;
            if (finalRight.HasValue) sheet.Padding.Right = finalRight;
            if (finalTop.HasValue) sheet.Padding.Top = finalTop;
            if (finalBottom.HasValue) sheet.Padding.Bottom = finalBottom;

            return sheet;
        }

        public static Stylesheet Border(this Stylesheet sheet, float? all = null, float? top = null, float? right = null, float? bottom = null, float? left = null, Color color = null)
        {
            if (all.HasValue) sheet.Border.Width = all.Value;

            if (top.HasValue) sheet.Border.Top = top.Value;
            if (left.HasValue) sheet.Border.Left = left.Value;
            if (bottom.HasValue) sheet.Border.Bottom = bottom.Value;
            if (right.HasValue) sheet.Border.Right = right.Value;

            if (color != null) sheet.Border.Color = color;

            return sheet;
        }

        public static Stylesheet BorderRadius(this Stylesheet sheet, float? all = null, float? topLeft = null, float? topRight = null, float? bottomRight = null, float? bottomLeft = null)
        {
            if (all.HasValue) sheet.borderRadius = all.Value;

            if (topLeft.HasValue) sheet.BorderRadius.TopLeft = topLeft.Value;
            if (topRight.HasValue) sheet.BorderRadius.TopRight = topRight.Value;
            if (bottomRight.HasValue) sheet.BorderRadius.BottomRight = bottomRight.Value;
            if (bottomLeft.HasValue) sheet.BorderRadius.BottomLeft = bottomLeft.Value;
            
            return sheet;
        }

        /// <summary>
        /// Sets both width and height to the same value.
        /// </summary>
        public static Stylesheet Size(this Stylesheet sheet, Length.LengthRequest size) => sheet.Width(size).Height(size);

        public static Stylesheet WrapText(this Stylesheet sheet, bool? value = true) => sheet.Set(x => x.WrapText = value);

        public static Stylesheet TextColor(this Stylesheet sheet, Color value) => sheet.Set(x => x.TextColor = value);

        public static Stylesheet TextAlignment(this Stylesheet sheet, Alignment value) => sheet.Set(x => x.TextAlignment = value);

        public static Stylesheet Font(this Stylesheet sheet, float? size = null, bool? bold = null, bool? italic = null, Color color = null)
        {
            if (size.HasValue) sheet.Font.Size = size.Value;
            if (bold.HasValue) sheet.Font.Bold = bold.Value;
            if (italic.HasValue) sheet.Font.Italic = italic.Value;
            if (color != null) sheet.TextColor = color;
            return sheet;
        }
    }
}