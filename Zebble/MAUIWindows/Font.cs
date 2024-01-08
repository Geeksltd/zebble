namespace Zebble
{
    using System;

    partial class Font
    {
        float CalculateAutomaticExtraTopPadding() => throw new NotSupportedException();
        float CalculateTextHeight(float width, string text) => throw new NotSupportedException();
        public static string DefaultSystemFont => throw new NotSupportedException();
        float CalculateTextWidth(string text) => throw new NotSupportedException();
        float CalculateFontLineHeight() => throw new NotSupportedException();
        static float GetScaledFontSize(float fontSize) => throw new NotSupportedException();
    }
}