namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using Android.Graphics;
    using Android.Text;
    using Android.Util;
    using Zebble.Device;
    using Olive;

    partial interface IFont { Typeface Render(); }

    partial class Font
    {
        static Android.Widget.TextView Sample;
        static Rect SampleRectangle = new Rect();
        static TextPaint SamplePaint = new TextPaint();
        static Dictionary<string, Typeface> FontCache = new Dictionary<string, Typeface>();

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

            using (var layout = StaticLayout.Builder.Obtain(text, 0, text.Length, SamplePaint, Scale.ToDevice(width)).Build())
            {
                var linePadding = GetUnwantedExtraTopPadding();
                var lineCount = layout.Height / actualHeight;
                return Scale.ToZebble(layout.Height + Math.Abs(layout.BottomPadding) + Math.Abs(layout.TopPadding) + linePadding * lineCount);
            }
        }

        float CalculateFontLineHeight() => GetTextHeight(1000, "Tag");

        float CalculateAutomaticExtraTopPadding()
        {
            using (var paint = new Paint { TextSize = EffectiveSize })
            {
                var typeFace = TypefaceStyle.Normal;

                if (Italic && Bold) typeFace = TypefaceStyle.BoldItalic;
                else if (Bold) typeFace = TypefaceStyle.Bold;
                else if (Italic) typeFace = TypefaceStyle.Italic;

                paint.SetTypeface(Typeface.Create(Name, typeFace));
                paint.SetStyle(Paint.Style.Fill);
                return paint.Descent() - paint.Ascent() - EffectiveSize;
            }
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
            return Scale.ToZebble(SampleRectangle.Right + SampleRectangle.Left + TEXT_WIDTH_ERROR);
        }

        public Typeface Render()
        {
            if (FontCache.TryGetValue(ToString(), out var result)) return result;

            if (Name.ContainsAny(new string[] { ".ttf", ".otf" }, caseSensitive: false))
                result = Typeface.CreateFromAsset(UIRuntime.CurrentActivity.Assets, Name.RemoveFrom("#"));
            else
            {
                var fontStyle = TypefaceStyle.Normal;
                if (Bold && Italic) fontStyle = TypefaceStyle.BoldItalic;
                else if (Bold) fontStyle = TypefaceStyle.Bold;
                else if (Italic) fontStyle = TypefaceStyle.Italic;
                result = Typeface.Create(Name, fontStyle);
            }

            FontCache[ToString()] = result;
            return result;
        }
    }
}