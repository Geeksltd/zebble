namespace Zebble
{
    using System;
    using System.Linq;
    using UIKit;
    using Olive;

    partial interface IFont { UIFont Render(); }

    partial class Font
    {
        static string systemFontName;

        public static string DefaultSystemFont
        {
            get
            {
                if (systemFontName.HasValue()) return systemFontName;
                return systemFontName = UIFont.SystemFontOfSize(100).Name;
            }
        }

        float CalculateTextHeight(float width, string text)
        {
            if (text.IsEmpty()) text = "Tg";

            using (var label = new UILabel
            {
                Frame = new CoreGraphics.CGRect(0, 0, width, float.MaxValue),
                Lines = 0,
                LineBreakMode = UILineBreakMode.WordWrap,
                Font = Render(),
                Text = text
            })
            {
                label.SizeToFit();
                var result = (float)label.Frame.Height;
                if (result == 0)
                    return EffectiveSize * 1.4f;

                return result;
            }
        }

        float CalculateFontLineHeight()
        {
            var font = Render();

            if (font.LineHeight != 0) return (float)font.LineHeight;
            else return EffectiveSize * 1.4f;
        }

        float CalculateAutomaticExtraTopPadding() => 0;

        float CalculateTextWidth(string text)
        {
            using (var label = new UILabel { Font = Render(), Text = text })
                return (float)label.IntrinsicContentSize.Width;
        }

        public UIFont Render()
        {
            if (Name == DefaultSystemFont) return UIFont.SystemFontOfSize(EffectiveSize);
            else return UIFont.FromName(GetFontName(Name), EffectiveSize);
        }

        static float GetScaledFontSize(float fontSize) => (float)new UIFontMetrics(UIFontTextStyle.Body.GetConstant()).GetScaledValue(fontSize * 1.1f);
    }
}