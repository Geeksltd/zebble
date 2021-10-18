namespace Zebble
{
    using System;
    using System.Linq;
    using System.Text;
    using Olive;

    internal class ZebbleCssSetting
    {
        public string Key, CSharpValue, StringValue, CastType;
        static readonly string[] CssProperties = "width,height,x,y,font,backgroundcolor,backgroundImagePath,backgroundImageData,backgroundImageStretch,backgroundImageAlignment,border,Opacity,padding,margin,TextColor,Visible,TextAlignment,BackgroundImage,Ignored,Absolute,WrapText,left,top,TextTransform,Zindex,ScaleX,ScaleY,Rotation,RotationX,RotationY"
            .ToLower().Split(',').Trim().ToArray();

        internal string ToCode(string variable, string property)
        {
            var r = new StringBuilder();

            if (NeedsAsyncMethodCall()) r.Append("await ");

            if (CastType.HasValue()) r.Append($"({variable} as {CastType}).Perform(x => x.");
            else r.Append(variable + ".");

            if (NeedsAsyncMethodCall()) r.Append($"Css.{MethodName}({CSharpValue})");
            else if (NeedsMethodCall()) r.Append(GetPropertyPath(property) + "(" + CSharpValue + ")");
            else if (Key.StartsWith("Background(")) r.Append(GetPropertyPath());
            else r.Append(GetPropertyPath(property) + " = " + CSharpValue);

            if (CastType.HasValue()) r.Append(")");

            r.Append(";");
            return r.ToString();
        }

        string MethodName => Key + "Async".OnlyWhen(Key == "Ignored");

        bool NeedsAsyncMethodCall() => Key.IsAnyOf("Direction", "Ignored"); // More can be added later

        bool NeedsMethodCall() => Key.IsAnyOf("Padding", "Margin", "BoxShadow") || NeedsAsyncMethodCall();

        internal string GetPropertyPath(string cssProperty = "Css")
        {
            var key = Key.Split('.').Trim().FirstOrDefault().ToLowerOrEmpty();

            return (cssProperty + ".").OnlyWhen(CssProperties.Contains(key)) + Key;
        }

        [EscapeGCop("Hardcoded numbers here are for priority.")]
        internal int GetPriority()
        {
            if (Key.StartsWith("Font")) return 80;
            if (Key.StartsWith("Padding")) return 70;
            if (Key.StartsWith("Margin")) return 60;

            switch (Key)
            {
                case "BackgroundImageStretch": return 110;
                case "Height": return 100;
                case "Width": return 90;
                case "BackgroundImagePath": return -10;
                default: return 1;
            }
        }
    }
}