namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using Android.Graphics;
    using Android.Text;
    using Android.Util;
    using Zebble.Device;
    using Olive;
    using System.Collections.Concurrent;

    partial interface IFont { Typeface Render(); }

    partial class Font
    {
        static Android.Widget.TextView Sample;
        static readonly Rect SampleRectangle = new();
        static readonly TextPaint SamplePaint = new();
        static readonly ConcurrentDictionary<string, Typeface> FontCache = new();

        const float TEXT_WIDTH_ERROR = 3;

        public static string DefaultSystemFont => "Roboto";

        float CalculateTextHeight(float width, string text)
        {
            if (text.IsEmpty()) text = "Tag";

            SamplePaint.TextSize = Scale.ToDevice(EffectiveSize);
            SamplePaint.FakeBoldText = Bold;
            SamplePaint.SetTypeface(Render());

            var fontMetrics = SamplePaint.GetFontMetrics();
            var actualHeight = SamplePaint.FontSpacing - Math.Abs((fontMetrics.Bottom - fontMetrics.Top - EffectiveSize - SamplePaint.FontSpacing) / 4f);

            var linePadding = GetUnwantedExtraTopPadding();

            using (var layout = new StaticLayout(text, 0, text.Length, SamplePaint, Scale.ToDevice(width), null, 1, 0, false))
            {
                var lineCount = layout.Height / actualHeight;
                return Scale.ToZebble(layout.Height + Math.Abs(layout.BottomPadding) + Math.Abs(layout.TopPadding) + linePadding * lineCount);
            }
        }

        float CalculateFontLineHeight()
        {
            using var paint = CreatePaint();

            var text = "Tag";

            Rect result = new();
            paint.GetTextBounds(text, 0, text.Length, result);

            return result.Height() + CalculateAutomaticExtraTopPadding();
        }

        float CalculateAutomaticExtraTopPadding()
        {
            using var paint = CreatePaint();

            return paint.Descent() - paint.Ascent() - EffectiveSize;
        }

        Paint CreatePaint()
        {
            var paint = new Paint { TextSize = EffectiveSize, };

            paint.SetTypeface(Render());
            paint.SetStyle(Paint.Style.Fill);

            return paint;
        }

        Android.Widget.TextView GetSample()
        {
            var result = Sample ?? (Sample = new Android.Widget.TextView(Renderer.Context));

            result.SetTypeface(Render(), Render().Style);
            result.SetTextSize(ComplexUnitType.Px, Scale.ToDevice(EffectiveSize));

            return result;
        }

        float CalculateTextWidth(string text)
        {
            GetSample().Paint.GetTextBounds(text, 0, text.Length, SampleRectangle);
            return Scale.ToZebble(SampleRectangle.Right + SampleRectangle.Left + Scale.ToDevice(TEXT_WIDTH_ERROR));
        }

        public Typeface Render() => FontCache.GetOrAdd(ToString(), key =>
        {
            if (Name.ContainsAny([".ttf", ".otf"], caseSensitive: false))
                return Typeface.CreateFromAsset(UIRuntime.CurrentActivity.Assets, Name.RemoveFrom("#"));

            var fontStyle = TypefaceStyle.Normal;
            if (Bold && Italic) fontStyle = TypefaceStyle.BoldItalic;
            else if (Bold) fontStyle = TypefaceStyle.Bold;
            else if (Italic) fontStyle = TypefaceStyle.Italic;
            return Typeface.Create(Name, fontStyle);
        });

        static float GetScaledFontSize(float fontSize) => fontSize;
    }
}