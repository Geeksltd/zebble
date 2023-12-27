namespace Zebble
{
    using System;
    using System.Text;
    using Olive;

    public partial interface IFont
    {
        string Name { get; }
        float Size { get; }
        float EffectiveSize { get; }
        bool Bold { get; }
        bool Italic { get; }
        float GetTextWidth(string text);        
        float GetTextHeight(float width, string text);
        float GetLineHeight();
        float GetUnwantedExtraTopPadding();
    }

    public partial class Font : IFont
    {
        string toString;
        internal string name;
        internal float? size;
        internal bool? bold, italic;
        internal event Action Changed;

        public Font() { }
        public Font(string name, float size) { this.name = Clean(name); this.size = size; }

        static string Clean(string fontName) => fontName.OrEmpty().Remove("'", "\"");

        public string Name
        {
            get => name;
            set { value = Clean(value); if (name == value) return; name = value; toString = null; Changed?.Invoke(); }
        }

        public float Size
        {
            get => size ?? -1;
            set { if (size == value) return; size = value; toString = null; Changed?.Invoke(); }
        }

        public bool Bold
        {
            get => bold ?? false;
            set { if (bold == value) return; bold = value; toString = null; Changed?.Invoke(); }
        }

        public bool Italic
        {
            get => italic ?? false;
            set { if (italic == value) return; italic = value; toString = null; Changed?.Invoke(); }
        }

        public override string ToString()
        {
            var result = toString;

            if (result is null)
            {
                var r = new StringBuilder();
                r.Append(Name);
                r.Append(' ');
                r.Append(EffectiveSize);
                if (Bold) r.Append(" Bold");
                if (Italic) r.Append(" Italic");
                toString = result = r.ToString();
            }

            return result;
        }

        public override int GetHashCode() => ToString().GetHashCode();

        public static bool operator !=(Font @this, Font another) => !(@this == another);

        public static bool operator ==(Font @this, Font another) => @this?.Equals(another) ?? another is null;

        public override bool Equals(object obj)
        {
            var another = obj as Font;

            if (another is null) return false;

            return ToString() == another.ToString();
        }
    }
}