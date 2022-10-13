namespace Zebble
{
    using System;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Olive;

    internal static class BorderExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUniform(this IBorder @this)
        {
            return new[] { @this.Left, @this.Right, @this.Top, @this.Bottom }.Distinct().IsSingle();
        }
    }

    public interface IBorder
    {
        float Left { get; }
        float Right { get; }
        float Top { get; }
        float Bottom { get; }

        Color Color { get; }
        bool HasValue();

        float TotalHorizontal { get; }
        float TotalVertical { get; }
    }

    public class Border : IBorder
    {
        internal float? left;
        internal float? right;
        internal float? top;
        internal float? bottom;
        internal Color color;
        internal event Action Changed, HorizontalSizeChanged, VerticalSizeChanged;

        static Border() => OliveExtensions.TryParseProviders.Register(Parse);

        public Border() { }

        public Border(float width, Color color) { Width = width; Color = color; }

        public Color Color
        {
            get => color ?? Colors.Black;
            set { if (color == value) return; color = value; Changed?.Invoke(); }
        }

        public float Width
        {
            set
            {
                left = right = top = bottom = value;
                Changed?.Invoke();
                HorizontalSizeChanged?.Invoke();
                VerticalSizeChanged?.Invoke();
            }
        }

        public float Left
        {
            get => left ?? 0;
            set
            {
                if (left == value) return;
                left = value;
                Changed?.Invoke();
                HorizontalSizeChanged?.Invoke();
            }
        }

        public float Right
        {
            get => right ?? 0;
            set
            {
                if (right == value) return;
                right = value;
                Changed?.Invoke();
                HorizontalSizeChanged?.Invoke();
            }
        }

        public float Top
        {
            get => top ?? 0;
            set
            {
                if (top == value) return;
                top = value;
                VerticalSizeChanged?.Invoke();
                Changed?.Invoke();
            }
        }

        public float Bottom
        {
            get => bottom ?? 0;
            set
            {
                if (bottom == value) return;
                bottom = value;
                HorizontalSizeChanged?.Invoke();
                Changed?.Invoke();
            }
        }

        public static implicit operator Border(int value) => new() { Width = value };

        public bool HasValue()
        {
            return Left > 0 || Right > 0 || Bottom > 0 || Top > 0;
        }

        public override string ToString()
        {
            var parts = new[] { Top, Right, Bottom, Left };

            if (parts.Distinct(x => x).IsSingle()) return Left + "px " + Color;
            return parts.Select(x => x + "px".Unless(x == 0)).ToString(" ") + " " + Color;
        }

        public static Border Parse(string text)
        {
            if (text.IsEmpty()) return new Border();

            var parts = text.Replace(" ", ",").KeepReplacing(",,", ",").Split(',')
               .Select(x => x.TrimEnd("px").TrimEnd("pt").TrimEnd("%"))
               .ToArray();

            if (parts.Length == 2 && parts.First().Is<int>() && parts.Last().StartsWith("#"))
                return new Border(parts.First().To<int>(), Color.Parse(parts.Last()));

            var numberParts = parts
               .Select(x => x.TryParseAs<int>())
               .Except(x => x is null)
               .Select(x => x.Value)
               .ToArray();

            if (numberParts.IsSingle())
                return numberParts.First();

            if (parts.Length < 4)
                return new Border { top = numberParts[0], bottom = numberParts[0], left = numberParts[1], right = numberParts[1] };

            if (numberParts.None()) return new Border();

            return new Border { Top = numberParts[0], Right = numberParts[1], Bottom = numberParts[2], Left = numberParts[3] };
        }

        public override int GetHashCode() => ToString().GetHashCode();

        public static bool operator !=(Border @this, Border another) => !(@this == another);

        public static bool operator ==(Border @this, Border another) => @this?.Equals(another) ?? another is null;

        public override bool Equals(object obj)
        {
            var another = obj as Border;

            if (another is null) return false;

            if (top != another.top) return false;
            if (bottom != another.bottom) return false;
            if (left != another.left) return false;
            if (right != another.right) return false;
            if (color != another.color) return false;

            return true;
        }

        public float TotalHorizontal => Left + Right;

        public float TotalVertical => Top + Bottom;
    }
}