namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Olive;

    public class Color
    {
        string toString;
        static Dictionary<string, Color> ColorParseCache = new();

        const int COLOR_CHARACTERS = 6;

        public Color(byte red, byte green, byte blue) : this(red, green, blue, byte.MaxValue) { }

        public Color(byte red, byte green, byte blue, byte alpha) { Red = red; Green = green; Blue = blue; Alpha = alpha; }

        static Color() => OliveExtensions.TryParseProviders.Register(Parse);

        public byte Red { get; }
        public byte Green { get; }
        public byte Blue { get; }
        public byte Alpha { get; protected set; }

        public bool IsTransparent() => Alpha == 0;

        public static implicit operator Color(string text) => Parse(text);

        /// <summary>
        /// Gets the inverted color which is based on 255 minus each color component.
        /// </summary>
        public Color Invert()
        {
            return new Color((byte)(byte.MaxValue - Red), (byte)(byte.MaxValue - Green), (byte)(byte.MaxValue - Blue));
        }

        /// <summary>
        /// Will return a color from its HEX string format. Example: #A2B044.
        /// It also supports opacity, e.g: #A2B044 20%. 
        /// Also you can seperate gradietn elements with a bar (|) character.
        /// </summary>
        public static Color Parse(string text)
        {
            if (ColorParseCache.TryGetValue(text, out var result)) return result;

            result = Colors.FromName(text);

            if (result != null)
                lock (ColorParseCache)
                    return ColorParseCache[text] = result;

            var gradientElements = text.OrEmpty().Split('|').Trim().ToArray();

            if (gradientElements.None()) result = Colors.Transparent;
            else if (gradientElements.IsSingle() && gradientElements.First().ToUpper() == "TRANSPARENT") result = Colors.Transparent;
            else if (text.StartsWith("linear-gradient(")) result = GradientColor.ParseFromCss(text);
            else if (gradientElements.IsSingle()) result = ParsePlainColor(gradientElements.Single());
            else result = GradientColor.ParseFromParts(gradientElements);

            lock (ColorParseCache)
                return ColorParseCache[text] = result;
        }

        static Color ParseHex(string code)
        {
            if (code.Length == 3) code = $"{code[0]}{code[0]}{code[1]}{code[1]}{code[2]}{code[2]}";

            if (code.Length != COLOR_CHARACTERS) return Colors.Red; // invalid color.

            return new Color(
                    red: byte.Parse(code.Substring(0, 2), NumberStyles.HexNumber),
                    green: byte.Parse(code.Substring(2, 2), NumberStyles.HexNumber),
                    blue: byte.Parse(code.Substring(4, 2), NumberStyles.HexNumber)
                  );
        }

        static Color ParsePlainColor(string colorText)
        {
            if (ColorParseCache.TryGetValue(colorText, out var result)) return result;

            var text = colorText;

            try
            {
                text = text.KeepReplacing("  ", " ");

                var firstPart = text.Split(' ').FirstOrDefault();
                var alpha = text.Split(' ').LastOrDefault().Unless(firstPart);

                if (!firstPart.StartsWith("#")) return Colors.Black;

                firstPart = firstPart.TrimStart("#");

                result = ParseHex(firstPart);

                if (alpha?.EndsWith("%") == true)
                    result.Alpha = (byte)(byte.MaxValue * alpha.TrimEnd("%").To<int>() / 100.0);
                else result.Alpha = byte.MaxValue;

                lock (ColorParseCache)
                    ColorParseCache[colorText] = result;

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse the color: '" + text + "'", ex);
            }
        }

        public override string ToString()
        {
            if (toString is not null) return toString;

            if (Alpha == 0) return toString = "Transparent";

            return toString = string.Format("#{0:X2}{1:X2}{2:X2}", Red, Green, Blue) +
    (Alpha / (double)byte.MaxValue).ToString("P0").Unless("100%").WithPrefix(" ");
        }

        public override int GetHashCode() => ToString().GetHashCode();

        public static bool operator !=(Color @this, Color another) => !(@this == another);

        public static bool operator ==(Color @this, Color another) => @this?.Equals(another) ?? another is null;

        public override bool Equals(object obj)
        {
            var another = obj as Color;
            if (another is null) return false;

            if (IsTransparent() && another.IsTransparent()) return true;

            if (Red != another.Red) return false;
            if (Green != another.Green) return false;
            if (Blue != another.Blue) return false;
            if (Alpha != another.Alpha) return false;

            return true;
        }

        public Color Lighten(int amount)
        {
            if (IsTransparent()) return this;

            byte lighten(byte part) => (byte)(part + amount).LimitMax(byte.MaxValue).LimitMin(0);
            return new Color(lighten(Red), lighten(Green), lighten(Blue), Alpha);
        }

        public Color Darken(int amount) => Lighten(-amount);

        static bool TryParse(string text, Type type, out object result)
        {
            result = null;

            if (!type.IsA<Color>()) return false;

            try { result = Parse(text); return true; }
            catch
            {
                // No logging is needed
                return false;
            }
        }
    }
}