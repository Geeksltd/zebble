namespace Zebble
{
    using System;
    using Microsoft.UI.Text;
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using Microsoft.UI.Xaml.Media;
    using foundation = Windows.Foundation;
    using Olive;

    partial interface IFont { FontFamily Render(); }

    partial class Font
    {
        static TextBlock Sample;
        static foundation.Size NoLimit = new(double.PositiveInfinity, double.PositiveInfinity);
        
        public static string DefaultSystemFont => "Segoe UI";

        float CalculateTextHeight(float width, string text)
        {
            width = width.LimitMin(0);

            var tb = GetTextBlock(text.Or("Tag"));
            tb.TextWrapping = TextWrapping.WrapWholeWords;
            tb.TextLineBounds = TextLineBounds.Full;
            tb.Measure(new foundation.Size(width, double.PositiveInfinity));
            return (float)tb.DesiredSize.Height - GetUnwantedExtraTopPadding();
        }

        float CalculateLineHeight()
        {
            var tb = GetTextBlock("Tag");
            tb.TextWrapping = TextWrapping.NoWrap;
            tb.TextLineBounds = TextLineBounds.Full;
            tb.Measure(NoLimit);
            return (float)tb.DesiredSize.Height -
              (float)(tb.DesiredSize.Height - EffectiveSize) / 2f;
        }

        float CalculateFontLineHeight() => Thread.UI.Run(() => CalculateLineHeight());

        float CalculateAutomaticExtraTopPadding()
        {
            var tb = GetTextBlock("T");

            tb.Measure(NoLimit);

            var characterSize = tb.ActualHeight;

            tb.TextLineBounds = TextLineBounds.TrimToBaseline;
            tb.Measure(NoLimit);
            tb.Arrange(new foundation.Rect(new foundation.Point(0, 0), tb.DesiredSize));
            var withoutBaseLine = tb.ActualHeight;

            return (float)(withoutBaseLine - characterSize) / 2;
        }

        TextBlock GetTextBlock(string text)
        {
            var result = Sample ??= new TextBlock();

            result.RenderFont(this);
            result.Text = text;
            result.TextWrapping = TextWrapping.NoWrap;
            result.TextLineBounds = TextLineBounds.Tight;
            result.Arrange(new foundation.Rect(new foundation.Point(0, 0), result.DesiredSize));
            return result;
        }

        float CalculateTextWidth(string text)
        {
            var tb = GetTextBlock(text);
            tb.Measure(NoLimit);
            return (float)tb.DesiredSize.Width;
        }

        public FontFamily Render()
        {
            try
            {
                var fontName = Name;
                if (Name.ContainsAny(new string[] { ".ttf", ".otf" }, caseSensitive: false))
                    fontName = fontName.EnsureStartsWith("/Assets/Fonts/");
                return new FontFamily(fontName);
            }
            catch (Exception ex)
            {
                throw new RenderException($"Could not find the font '{Name}'.", ex);
            }
        }

        static float GetScaledFontSize(float fontSize) => fontSize;
    }
}