namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Olive;

    public class Colors
    {
        // For performance.
        static readonly Color black = "#000000", white = "#FFFFFF";
        static readonly Color transparent = new(byte.MaxValue, byte.MaxValue, byte.MaxValue, alpha: 0);

        public static Color Transparent => transparent;
        public static Color White => white;
        public static Color Black => black;

        public static Color AliceBlue => "#F0F8FF";
        public static Color AntiqueWhite => "#FAEBD7";
        public static Color Aqua => "#00FFFF";
        public static Color Aquamarine => "#7FFFD4";
        public static Color Azure => "#F0FFFF";
        public static Color Beige => "#F5F5DC";
        public static Color Bisque => "#FFE4C4";

        public static Color BlanchedAlmond => "#FFEBCD";
        public static Color Blue => "#0000FF";
        public static Color BlueViolet => "#8A2BE2";
        public static Color Brown => "#A52A2A";
        public static Color BurlyWood => "#DEB887";
        public static Color CadetBlue => "#5F9EA0";
        public static Color Chartreuse => "#7FFF00";
        public static Color Chocolate => "#D2691E";
        public static Color Coral => "#FF7F50";
        public static Color CornflowerBlue => "#6495ED";
        public static Color Cornsilk => "#FFF8DC";
        public static Color Crimson => "#DC143C";
        public static Color Cyan => "#00FFFF";
        public static Color DarkBlue => "#00008B";
        public static Color DarkCyan => "#008B8B";
        public static Color DarkGoldenRod => "#B8860B";
        public static Color DarkGray => "#A9A9A9";
        public static Color DarkGrey => "#A9A9A9";
        public static Color DarkGreen => "#006400";
        public static Color DarkKhaki => "#BDB76B";
        public static Color DarkMagenta => "#8B008B";
        public static Color DarkOliveGreen => "#556B2F";
        public static Color DarkOrange => "#FF8C00";
        public static Color DarkOrchid => "#9932CC";
        public static Color DarkRed => "#8B0000";
        public static Color DarkSalmon => "#E9967A";
        public static Color DarkSeaGreen => "#8FBC8F";
        public static Color DarkSlateBlue => "#483D8B";
        public static Color DarkSlateGray => "#2F4F4F";
        public static Color DarkSlateGrey => "#2F4F4F";
        public static Color DarkTurquoise => "#00CED1";
        public static Color DarkViolet => "#9400D3";
        public static Color DeepPink => "#FF1493";
        public static Color DeepSkyBlue => "#00BFFF";
        public static Color DimGray => "#696969";
        public static Color DimGrey => "#696969";
        public static Color DodgerBlue => "#1E90FF";
        public static Color FireBrick => "#B22222";
        public static Color FloralWhite => "#FFFAF0";
        public static Color ForestGreen => "#228B22";
        public static Color Fuchsia => "#FF00FF";
        public static Color Gainsboro => "#DCDCDC";
        public static Color GhostWhite => "#F8F8FF";
        public static Color Gold => "#FFD700";
        public static Color GoldenRod => "#DAA520";
        public static Color Gray => "#808080";
        public static Color Grey => "#808080";
        public static Color Green => "#008000";
        public static Color GreenYellow => "#ADFF2F";
        public static Color HoneyDew => "#F0FFF0";
        public static Color HotPink => "#FF69B4";
        public static Color IndianRed => "#CD5C5C";
        public static Color Indigo => "#4B0082";
        public static Color Ivory => "#FFFFF0";
        public static Color Khaki => "#F0E68C";
        public static Color Lavender => "#E6E6FA";
        public static Color LavenderBlush => "#FFF0F5";
        public static Color LawnGreen => "#7CFC00";
        public static Color LemonChiffon => "#FFFACD";
        public static Color LightBlue => "#ADD8E6";
        public static Color LightCoral => "#F08080";
        public static Color LightCyan => "#E0FFFF";
        public static Color LightGoldenRodYellow => "#FAFAD2";
        public static Color LightGray => "#D3D3D3";
        public static Color LightGrey => "#D3D3D3";
        public static Color LightGreen => "#90EE90";
        public static Color LightPink => "#FFB6C1";
        public static Color LightSalmon => "#FFA07A";
        public static Color LightSeaGreen => "#20B2AA";
        public static Color LightSkyBlue => "#87CEFA";
        public static Color LightSlateGray => "#778899";
        public static Color LightSlateGrey => "#778899";
        public static Color LightSteelBlue => "#B0C4DE";
        public static Color LightYellow => "#FFFFE0";
        public static Color Lime => "#00FF00";
        public static Color LimeGreen => "#32CD32";
        public static Color Linen => "#FAF0E6";
        public static Color Magenta => "#FF00FF";
        public static Color Maroon => "#800000";
        public static Color MediumAquaMarine => "#66CDAA";
        public static Color MediumBlue => "#0000CD";
        public static Color MediumOrchid => "#BA55D3";
        public static Color MediumPurple => "#9370DB";
        public static Color MediumSeaGreen => "#3CB371";
        public static Color MediumSlateBlue => "#7B68EE";
        public static Color MediumSpringGreen => "#00FA9A";
        public static Color MediumTurquoise => "#48D1CC";
        public static Color MediumVioletRed => "#C71585";
        public static Color MidnightBlue => "#191970";
        public static Color MintCream => "#F5FFFA";
        public static Color MistyRose => "#FFE4E1";
        public static Color Moccasin => "#FFE4B5";
        public static Color NavajoWhite => "#FFDEAD";
        public static Color Navy => "#000080";
        public static Color OldLace => "#FDF5E6";
        public static Color Olive => "#808000";
        public static Color OliveDrab => "#6B8E23";
        public static Color Orange => "#FFA500";
        public static Color OrangeRed => "#FF4500";
        public static Color Orchid => "#DA70D6";
        public static Color PaleGoldenRod => "#EEE8AA";
        public static Color PaleGreen => "#98FB98";
        public static Color PaleTurquoise => "#AFEEEE";
        public static Color PaleVioletRed => "#DB7093";
        public static Color PapayaWhip => "#FFEFD5";
        public static Color PeachPuff => "#FFDAB9";
        public static Color Peru => "#CD853F";
        public static Color Pink => "#FFC0CB";
        public static Color Plum => "#DDA0DD";
        public static Color PowderBlue => "#B0E0E6";
        public static Color Purple => "#800080";
        public static Color RebeccaPurple => "#663399";
        public static Color Red => "#FF0000";
        public static Color RosyBrown => "#BC8F8F";
        public static Color RoyalBlue => "#4169E1";
        public static Color SaddleBrown => "#8B4513";
        public static Color Salmon => "#FA8072";
        public static Color SandyBrown => "#F4A460";
        public static Color SeaGreen => "#2E8B57";
        public static Color SeaShell => "#FFF5EE";
        public static Color Sienna => "#A0522D";
        public static Color Silver => "#C0C0C0";
        public static Color SkyBlue => "#87CEEB";
        public static Color SlateBlue => "#6A5ACD";
        public static Color SlateGray => "#708090";
        public static Color SlateGrey => "#708090";
        public static Color Snow => "#FFFAFA";
        public static Color SpringGreen => "#00FF7F";
        public static Color SteelBlue => "#4682B4";
        public static Color Tan => "#D2B48C";
        public static Color Teal => "#008080";
        public static Color Thistle => "#D8BFD8";
        public static Color Tomato => "#FF6347";
        public static Color Turquoise => "#40E0D0";
        public static Color Violet => "#EE82EE";
        public static Color Wheat => "#F5DEB3";

        public static Color WhiteSmoke => "#F5F5F5";
        public static Color Yellow => "#FFFF00";
        public static Color YellowGreen => "#9ACD32";

        static IEnumerable<PropertyInfo> All() => typeof(Colors).GetProperties(BindingFlags.Static | BindingFlags.Public);

        public static IEnumerable<string> GetKnownNames() => All().Select(x => x.Name);

        public static Color FromName(string name)
        {
            return All()
               .FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
               ?.GetValue(null) as Color;
        }

        /// <summary>Returns a random color from the list of colors with known names.</summary>
        public static Color PickRandom() => FromName(GetKnownNames().PickRandom());
    }
}