namespace Zebble.CompileZbl
{
    using System;
    using System.Linq;
    using System.Xml.Linq;
    using Olive;

    class MarkupPropertySetting
    {
        public string NodeType, Key, Value, Bindable;

        public MarkupPropertySetting(string nodeType, XAttribute attribute)
        {
            NodeType = nodeType;
            Key = attribute.Name.LocalName;

            if (Key == "style") Key = "Style"; // Common error

            var value = attribute.Value;
            Bindable = string.Empty;

            if (value.StartsWith("@"))
            {
                value = value.Substring(1);

                if (value.StartsWith("{") && value.EndsWith("}"))
                {
                    Bindable = value.Trim('{', '}');
                    value = null;
                }
            }
            else if (Key.IsNoneOf("DataSource", "Source"))
            {
                // Typed?
                var propertyName = Key.Split('.').Trim().Last();

                if (value.TrimEnd("%").TryParseAs<double>().HasValue && !SeemsText(propertyName))
                {
                    var number = value.TrimEnd("%").To<double>();
                    value = number + "f".Unless(number.Round(0).AlmostEquals(number)) + ".Percent()".OnlyWhen(value.EndsWith("%"));
                }
                else if (value.TryParseAs<bool>().HasValue) { /* Keep */ }
                else if (value.ToLower().IsAnyOf("content", "container") && Key.IsAnyOf("Width", "Height"))
                    value = "Length.AutoStrategy." + value.ToPascalCaseId();
                else
                {
                    value = GetAsEnum(value).Or(GetStringExpression(value));
                }
            }

            Value = value;
        }

        public string GenerateSetExpression()
        {
            foreach (var call in new[] { "Style.Padding", "Style.Margin", "Width", "Height" })
                if (Key.ContainsWholeWord(call))
                    return Key + "(" + Value + ")";

            return Key + " = " + Value;
        }

        public string GenerateBindingExpression()
        {
            var last = Key.Split('.').Last();
            var beforeLast = Key.Split('.').ExceptLast().ToString(".");

            var binding = ".Bind(";

            if (last.StartsWith("on-"))
            {
                last = last.TrimStart("on-");

                if (last.EndsWith(".ChangedByInput")) binding = ".Set(v => v." + last + " += ";
                else binding = ".On(v => v." + last;
            }
            else binding += "nameof(" + NodeType.OnlyWhen(NodeType != "class").WithSuffix(".") + last + ")";

            binding += ", () => " + Bindable + ")";

            if (beforeLast.IsEmpty()) return binding;

            return ".Set(x => x." + beforeLast + binding + ")";
        }

        public string GenerateEventHandlerExpression()
        {
            var expression = Value.Trim('\"');

            if (expression.EndsWith("()") && expression.Lacks("=>"))
                expression = "() => " + expression;

            if (Key.EndsWith(".ChangedByInput"))
                return ".Set(x => x." + Key.TrimStart("on-") + " += " + expression + ")";

            return ".On(x => x." + Key.TrimStart("on-") + ", " + expression + ")";
        }

        static bool SeemsText(string propertyName) => propertyName.EndsWithAny("Text", "Title", "Header", "Label");

        string GetAsEnum(string value)
        {
            if (value.ToPascalCaseId().ToLower() != value.ToLower()) return null; // Expression?

            var direct = Key.Split('.').Trim().Last();

            if (direct.IsAnyOf("Stretch", "Alignment", "TextTransform", "KeyboardActionType", "IconLocation", "VideoQuality", "TextMode"))
                return direct + "." + value.ToPascalCaseId();

            switch (direct)
            {
                case "Direction": return "RepeatDirection." + value;
                case "HorizontalAlignment": return "HorizontalAlignment." + value;
                case "TextAlignment":
                case "BackgroundImageAlignment": return "Alignment." + value;
                case "BackgroundImageStretch": return "Stretch." + value;
                case "Platform": return "DevicePlatform." + value;
                case "Transition": return "PageTransition." + value;
                case "Easing": return "AnimationEasing." + value;
                default: return null;
            }
        }

        public static string GetStringExpression(string literal)
        {
            if (literal.IsEmpty()) return "string.Empty";

            return "\"{0}\"".FormatWith(literal.Replace("\\", @"\\").Replace("\"", @"\""").Replace("\t", @"\t")
            .Replace("\r", @"\r").Replace("\n", @"\n"));
        }
    }
}