namespace Zebble
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Olive;

    class CssSetting
    {
        public string CssKey, CssValue;
        string[] BackgroundPositions = { "left", "center", "right", "top", "bottom" };

        public CssSetting(string cssKey, string cssValue)
        {
            CssKey = cssKey;
            CssValue = cssValue;
            CleanUp();
        }

        void CleanUpBackground()
        {
            if (CssValue.StartsWith("rgb"))
            {
                CssKey = "BackgroundColor";
                CssValue = ParseRgbColor(CssValue);
            }
            else if (CssValue.Split(' ').Any(c => c.Contains("url")) && CssValue.Split(' ').Any(s => s.StartsWith("#")))
            {
                var color = CssValue.Split(' ').First(x => x.StartsWith("#"));
                var imagePath = CssValue.Split(' ').First(x => x.Contains("url"));
                CssKey = "Background(" + ToColorCSharpExpression(color) + "," + imagePath.Remove("url(").Remove(")") + ")";
                CssValue = "";
            }
            else if (CssValue.Split(' ').Any(c => c.Contains("url")) && CssValue.Split(' ').Any(s => BackgroundPositions.Contains(s)))
            {
                var color = CssValue.Split(' ').First(x => BackgroundPositions.Contains(x));
                var imagePath = CssValue.Split(' ').First(x => x.Contains("url"));
                CssKey = "Background(path:" + imagePath.Remove("url(").Remove(")") + ",alignment: Alignment." + GetBackgroundPosition(color) + ")";
                CssValue = "";
            }
            else if (ValueIsColor()) CssKey = "background-color";
            else CssKey = "background-image";
        }

        void CleanUpBackgroundColor()
        {
            CssKey = "BackgroundColor";

            if (CssValue.StartsWith("rgb"))
                CssValue = ParseRgbColor(CssValue);
        }

        void CleanUpPaddingMargin()
        {
            var parts = CssValue.Split(' ').Trim()
                   .Select(x => x
                   .TrimEnd("px", caseSensitive: false)
                   .TrimEnd("pt", caseSensitive: false)
                   .TrimEnd("em", caseSensitive: false))
                   .Trim().Select(x => x + "f".OnlyWhen(x.Contains("."))).ToArray();

            if (parts.Length == 2)
                CssValue = "vertical: " + parts.First() + ", horizontal: " + parts.Last();

            if (parts.Length == 4)
                CssValue = $"top: {parts.First()}, right: {parts.ElementAt(1)}, bottom: {parts.ElementAt(2)}, left: {parts.Last()}";
        }

        void CleanUpBackgroundSize()
        {
            CssKey = "BackgroundImageStretch";

            if (CssValue == "auto") CssValue = "AspectFill";
            if (CssValue == "cover") CssValue = "Fill";
            if (CssValue == "contain") CssValue = "Fit";
            if (CssValue == "initial") CssValue = "Default";
        }

        void CleanUp()
        {
            // border-right-width -> border-right
            if (CssKey.StartsWith("border-") && CssKey.EndsWith("-width") && CssKey.AllIndices("-").Count() == 2)
                CssKey = CssKey.ToLowerOrEmpty().TrimEnd("-width");

            if (CssKey == "background") CleanUpBackground();
            if (CssKey == "background-color") CleanUpBackgroundColor();
            if (CssKey == "background-size") CleanUpBackgroundSize();

            if (CssKey == "background-image") CssKey = "BackgroundImagePath";

            if (CssKey == "font-family")
            {
                CssKey = "Font.Name";
                CssValue = CssValue.Replace("'", "\"");

                if (!CssValue.StartsWith("calc(") && CssValue.Lacks("\""))
                    CssValue = "\"" + CssValue + "\"";
            }

            if (CssKey == "z-index") CssKey = "ZIndex";
            if (CssKey == "color") CssKey = "TextColor";

            if (CssValue.EndsWithAny("px", "pt") && CssValue.TrimEnd(2).Is<double>())
                CssValue = CssValue.TrimEnd(2);

            if (CssKey == "left") CssKey = "X";
            if (CssKey == "top") CssKey = "Y";

            if (CssKey.IsAnyOf("padding", "margin")) CleanUpPaddingMargin();

            if (CssValue.ToLower().IsAnyOf("content", "container") && CssKey.IsAnyOf("width", "height"))
                CssValue = "Length.AutoStrategy." + CssValue.ToLower().ToPascalCaseId();

            // Zebble is based on float.
            if (CssValue.Is<double>() && CssValue.Contains("."))
                CssValue += "f";

            if (CssValue.StartsWith("url(") && CssValue.EndsWith(")"))
                CssValue = CssValue.Substring(4).TrimEnd(1);

            if (CssKey == "background-position") CssKey = "BackgroundImageAlignment";

            if (CssKey.IsAnyOf("text-align", "BackgroundImageAlignment"))
            {
                if (!IsCalc()) CssValue = CssValue.ToPascalCaseId();
                if (CssValue == "Center") CssValue = "Middle";
            }

            // Font weight
            if (CssKey == "font-weight")
            {
                CssKey = "Font.Bold";

                if (CssValue == "bold") CssValue = "true";
                else if (!IsCalc()) CssValue = "false";
            }

            if (CssKey == "position")
            {
                CssKey = "Absolute";

                if (CssValue == "absolute") CssValue = "true";
                else if (!IsCalc()) CssValue = "false";
            }

            if (CssKey == "text-transform")
            {
                CssKey = "TextTransform";
                if (!IsCalc()) CssValue = CssValue.ToPascalCaseId();
            }

            if (CssKey == "display")
            {
                CssKey = "Ignored";

                if (CssValue == "none") CssValue = "true";
                // We consider all other values as block for now
                else CssValue = "false";
            }

            if (CssKey == "visibility")
            {
                CssKey = "Visible";

                if (CssValue == "hidden") CssValue = "false";
                // We consider all other values as visible for now
                else CssValue = "true";
            }

            if (CssKey.StartsWith("border-") && (CssKey.EndsWithAny("bottom", "top", "left", "right")))
                ProcessBorder();

            if (CssKey.StartsWith("border-") && (CssKey.EndsWithAny("bottom-color", "top-color", "left-color", "right-color")))
                ProcessBorder();

            if (CssKey.StartsWith("border-") && CssKey.EndsWith("-radius"))
                CssKey = "BorderRadius" + CssKey.TrimStart("border").TrimEnd("radius").Trim('-')
                    .Replace("-", " ").ToPascalCaseId().WithPrefix(".");

            if (CssKey == "white-space")
            {
                CssKey = "WrapText";
                CssValue = CssValue == "nowrap" ? "false" : "true";
            }

            if (CssKey == "line-height")
            {
                CssKey = "LineHeight";

                CssValue = CssValue.Trim()
                    .TrimEnd("px", caseSensitive: false)
                    .TrimEnd("pt", caseSensitive: false)
                    .TrimEnd("em", caseSensitive: false);
            }

            if (CssKey == "box-shadow") ProcessBoxShadow();
        }

        void ProcessBorder()
        {
            var keyParts = CssKey.Split('-');
            var valueParts = CssValue.Split(' ').Except("solid").ToArray();

            if (valueParts.Length == 2)
            {
                CssKey = "Border";
                var color = valueParts.Last();

                if (color.StartsWith("rgb"))
                    CssValue = ParseRgbColor(color);

                CssValue = "new Border { " + keyParts.Last().ToPascalCaseId() + " = " + valueParts.First().Remove("px") +
                           ", Color = \"" + color + "\"}";
            }

            if (valueParts.Length == 1)
            {
                CssKey = "Border";
                var color = valueParts.First();

                if (color.StartsWith("rgb")) color = ParseRgbColor(color);

                CssValue = "new Border { Color = \"" + color + "\"}";
            }
        }

        string GetBackgroundPosition(string position)
        {
            switch (position.ToUpper())
            {
                case "TOP":
                case "TOPCENTER":
                case "CENTERTOP": return "TopMiddle";

                case "RIGHTTOP":
                case "TOPRIGHT": return "TopRight";

                case "LEFTTOP":
                case "TOPLEFT": return "TopLeft";

                case "BOTTOM":
                case "BOTTOMCENTER":
                case "CENTERBOTTOM": return "BottomMiddle";

                case "RIGHTBOTTOM":
                case "BOTTOMRIGHT": return "BottomRight";

                case "LEFTBOTTOM":
                case "BOTTOMLEFT": return "BottomLeft";

                case "LEFT": return "Left";
                case "RIGHT": return "Right";

                case "CENTER": return "Middle";

                default: return "";
            }
        }

        bool IsCalc() => CssValue.StartsWith("calc(");

        bool SupportsLength() => CssKey.ToLowerOrEmpty().IsAnyOf("x", "y", "left", "right", "width", "height", "margin-left", "margin-right", "margin-top", "margin-bottom", "padding-left", "padding-right", "padding-top", "padding-bottom");

        bool IsViewPortUnit() => CssValue.ToLowerOrEmpty().ContainsAny(new[] { "vw", "vh", "vmax", "vmin" });

        public string GetCSharpValue()
        {
            if (IsCalc())
            {
                var result = CssValue.Substring("calc(".Length + 1).TrimEnd(2);

                if (SupportsLength())
                {
                    if (result.ToUpper() == "CONTAINER") result = "Length.AutoStrategy.Container";
                    else if (result.ToUpper() == "CONTENT") result = "Length.AutoStrategy.Content";
                    else
                    {
                        if (result.Lacks("=>")) result = "() => " + result;
                        result = result.WithWrappers("new Length.BindingLengthRequest(", ")");
                    }
                }

                return result;
            }
            else if (IsViewPortUnit())
            {
                if (SupportsLength())
                {
                    if (CssValue.ToLowerOrEmpty().EndsWith("vw"))
                    {
                        return CssValue.TrimEnd("vw").WithWrappers("new Length.BindingLengthRequest(View.Root.Width, x => x * (", "/ 100f))");
                    }
                    else if (CssValue.ToLowerOrEmpty().EndsWith("vh"))
                    {
                        return CssValue.TrimEnd("vh").WithWrappers("new Length.BindingLengthRequest(View.Root.Height, y=> y * (", "/ 100f))");
                    }
                    else if (CssValue.ToLowerOrEmpty().EndsWith("vmax"))
                    {
                        return CssValue.TrimEnd("vmax").WithWrappers("new Length.BindingLengthRequest(View.Root.Width, View.Root.Height, (x,y)=> Math.Max(x,y) * (", "/ 100f))");
                    }
                    else if (CssValue.ToLowerOrEmpty().EndsWith("vmin"))
                    {
                        return CssValue.TrimEnd("vmin").WithWrappers("new Length.BindingLengthRequest(View.Root.Width, View.Root.Height, (x,y)=> Math.Min(x,y) * (", "/ 100f))");
                    }
                }
                else
                    if (CssValue.ToLowerOrEmpty().EndsWith("vw"))
                {
                    return CssValue.TrimEnd("vw").WithWrappers("View.Root.ActualWidth * (", "/ 100f)");
                }
                else if (CssValue.ToLowerOrEmpty().EndsWith("vh"))
                {
                    return CssValue.TrimEnd("vh").WithWrappers("View.Root.ActualHeight * (", " / 100f)");
                }
                else if (CssValue.ToLowerOrEmpty().EndsWith("vmax"))
                {
                    return CssValue.TrimEnd("vmax").WithWrappers("Math.Max(View.Root.ActualWidth, View.Root.ActualHeight) * (", "/ 100f)");
                }
                else if (CssValue.ToLowerOrEmpty().EndsWith("vmin"))
                {
                    return CssValue.TrimEnd("vmin").WithWrappers("Math.Min(View.Root.ActualWidth, View.Root.ActualHeight) * (", "/ 100f)");
                }
            }

            if (CssValue.EndsWith("%"))
            {
                if (CssValue.StartsWith("#"))
                    return ToColorCSharpExpression(CssValue);
                else
                    return CssValue.TrimEnd(1) + ".Percent()";
            }

            if (ValueIsColor() || (CssKey.ToUpper().EndsWith("COLOR") && !CssValue.StartsWith("new Color("))) return ToColorCSharpExpression(CssValue);

            if (CssKey == "BackgroundImageAlignment") return "Alignment." + CssValue;
            if (CssKey == "TextTransform") return "TextTransform." + CssValue;
            if (CssKey == "BackgroundImageStretch") return "Stretch." + CssValue;
            if (CssValue == "auto") return "null";

            if (CssKey == "border") return GetBorderValue(CssValue, forCSharp: true);

            if (CssKey == "BackgroundImagePath")
            {
                if (CssValue.Contains("www."))
                    CssValue = "\"http:" + CssValue.Remove(")");
                else
                    return "\"" + GetStringValue() + "\"";
            }

            if (CssKey.StartsWith("border") && CssValue == "none") CssValue = "0";

            if (CssKey.Equals("opacity", StringComparison.OrdinalIgnoreCase) && !CssValue.EndsWith("f"))
                return CssValue + "f";

            if (CssValue.EndsWith("px", StringComparison.OrdinalIgnoreCase))
                return CssValue.TrimEnd("px");

            return CssValue;
        }

        static string ToColorCSharpExpression(string text)
        {
            if (Colors.GetKnownNames().Contains(text, caseSensitive: false))
                return "Colors." + Colors.GetKnownNames().FirstOrDefault(x => string.Equals(x, text, StringComparison.CurrentCultureIgnoreCase));
            else return "\"" + text + "\"";
        }

        bool ValueIsColor()
        {
            if (CssValue.StartsWith("linear-gradient(")) return true;

            if (Colors.FromName(CssValue) != null) return true;

            if (!CssValue.StartsWith("#")) return false;

            return CssValue.Length == "#F1F1F1".Length || CssValue.Length == "#FFF".Length;
        }

        internal string GetStringValue()
        {
            if (IsCalc()) return "{{{CALC}}}";

            if (CssValue == "auto") return null;

            if (CssKey == "border") return GetBorderValue(CssValue, forCSharp: false);

            if (CssKey == "BackgroundImagePath") return CssValue.Trim().Trim('\"', '\'').Trim();

            return CssValue;
        }

        string GetBorderValue(string text, bool forCSharp)
        {
            if (text.IsAnyOf("none", "0")) return "0";

            var parts = text.Replace(" ", ",").KeepReplacing(",,", ",").Split(',')
               .Select(x => x.TrimEnd("px").TrimEnd("pt").TrimEnd("%"))
               .ToArray();

            if (!forCSharp) return text;

            if (parts.Length == 2 && parts.First().Is<int>() && parts.Last().StartsWith("#"))
                return $"new Border({parts.First().To<int>()}, \"{parts.Last()}\")";

            if (parts.Length == 3 && parts[1] == "solid" && parts[0].TryParseAs<int>().HasValue)
                return $"new Border({parts.First()}, {ToColorCSharpExpression(parts.Last())})";

            var numberParts = parts.Select(x => x.TryParseAs<int>()).Except(x => x is null).Select(x => x.Value).ToArray();

            if (numberParts.IsSingle())
                return numberParts.First().ToString();

            if (parts.Length < 4 && parts.Length > 1)
                return $"new Border {{ Top = {numberParts[0]}, Bottom = {numberParts[0]}, Left = {numberParts[1]}, Right = {numberParts[1]} }}";

            if (parts.Length > 3)
            {
                if (parts.Any(x => x.StartsWith("#")))
                    return $"new Border {{ Top = {numberParts[0]}, Right = {numberParts[1]}, Bottom = {numberParts[2]}, Left = {numberParts[3]}, Color = \"{parts.First(x => x.StartsWith("#"))}\" }}";

                return $"new Border {{ Top = {numberParts[0]}, Right = {numberParts[1]}, Bottom = {numberParts[2]}, Left = {numberParts[3]} }}";
            }

            throw new Exception("Unrecognized border setting: " + text);
        }

        void ProcessBoxShadow()
        {
            if (CssValue.Contains(",") && CssValue.Lacks("rgb"))
                throw new Exception("Multiple box shadows aren't supported.");

            if (CssValue.Contains("inset"))
                throw new Exception("Inner box shadow (inset) isn't supported.");

            var valueParts = CssValue.Split(' ').Select(x => x.TrimEnd("px")).ToArray();

            if (valueParts.Length < 3)
                throw new Exception("To construct a box shadow, at least 3 parameters, v-offset, h-offset, and color are required.");

            var xOffset = valueParts[0];
            var yOffset = valueParts[1];
            var blurRadius = valueParts.Length >= 4 ? valueParts[2] : null;
            var expand = valueParts.Length == 5 ? valueParts[3] : null;

            var color = valueParts.Last();

            if (color.StartsWith("rgb"))
                color = ParseRgbColor(color);

            CssKey = "BoxShadow";
            CssValue = $"xOffset: {xOffset}, yOffset: {yOffset}, {$"blurRadius: {blurRadius}, ".OnlyWhen(blurRadius.HasValue())}{$"expand: {expand}, ".OnlyWhen(expand.HasValue())}color: \"{color}\"";
        }

        static string ParseRgbColor(string value)
        {
            var numbers = Regex.Split(value, @"\D+");

            if (numbers.Length == 5)
                return new Color(Convert.ToByte(numbers[1]), Convert.ToByte(numbers[2]), Convert.ToByte(numbers[3])).ToString();

            if (numbers.Length == 6)
                return new Color(Convert.ToByte(numbers[1]), Convert.ToByte(numbers[2]), Convert.ToByte(numbers[3]), Convert.ToByte(numbers[4])).ToString();

            if (numbers.Length == 7)
            {
                var floatNumber = Convert.ToDouble(numbers[4] + "." + numbers[5]);
                var alpha = (int)Math.Round(floatNumber * byte.MaxValue);
                return new Color(Convert.ToByte(numbers[1]), Convert.ToByte(numbers[2]), Convert.ToByte(numbers[3]), Convert.ToByte(alpha)).ToString();
            }

            return value;
        }
    }
}