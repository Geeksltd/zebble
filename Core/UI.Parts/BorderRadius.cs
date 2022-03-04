namespace Zebble
{
    using System;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Olive;

    internal static class BorderRadiusExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUniform(this IBorderRadius @this) => @this.GetRadiusCorners().Distinct().IsSingle();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] GetRadiusCorners(this IBorderRadius @this)
        {
            return new[] { @this.TopLeft, @this.TopRight, @this.BottomRight, @this.BottomLeft };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] GetEffectiveRadiusCorners(this IBorderRadius @this, View view)
        {
            var max = (view.ActualWidth + view.ActualHeight) / 4;
            return @this.GetRadiusCorners().Select(x => x.LimitMax(max)).ToArray();
        }
    }

    public interface IBorderRadius
    {
        float TopLeft { get; }
        float TopRight { get; }
        float BottomLeft { get; }
        float BottomRight { get; }

        bool HasValue();
    }

    public class BorderRadius : IBorderRadius
    {
        internal float? topLeft;
        internal float? topRight;
        internal float? bottomLeft;
        internal float? bottomRight;
        internal event Action Changed;

        static BorderRadius() => OliveExtensions.TryParseProviders.Register(Parse);

        public BorderRadius() { }

        public BorderRadius(float radius) { Radius = radius; }

        public float Radius
        {
            set
            {
                bottomLeft = topRight = topLeft = bottomRight = value;
                Changed?.Invoke();
            }
        }

        public float TopLeft
        {
            get => topLeft ?? 0;
            set { if (topLeft == value) return; topLeft = value; Changed?.Invoke(); }
        }

        public float TopRight
        {
            get => topRight ?? 0;
            set { if (topRight == value) return; topRight = value; Changed?.Invoke(); }
        }

        public float BottomLeft
        {
            get => bottomLeft ?? 0;
            set { if (bottomLeft == value) return; bottomLeft = value; Changed?.Invoke(); }
        }

        public float BottomRight
        {
            get => bottomRight ?? 0;
            set { if (bottomRight == value) return; bottomRight = value; Changed?.Invoke(); }
        }

        public static implicit operator BorderRadius(float value) => new(value);

        public bool HasValue()
        {
            return bottomLeft > 0 || bottomRight > 0 || topLeft > 0 || topRight > 0;
        }

        public override string ToString()
        {
            var parts = new[] { TopLeft, TopRight, BottomLeft, BottomRight };

            if (parts.Distinct(x => x).IsSingle()) return TopLeft + "px";
            return parts.Select(x => x + "px".Unless(x == 0)).ToString(" ");
        }

        public static BorderRadius Parse(string text)
        {
            if (text.IsEmpty()) return new BorderRadius();

            var parts = text.Replace(" ", ",").KeepReplacing(",,", ",").Split(',')
               .Select(x => x.TrimEnd("px").TrimEnd("pt").TrimEnd("%"))
               .ToArray();

            if (parts.Length == 1)
                return new BorderRadius(parts.First().To<int>());

            var numberParts = parts
               .Select(x => x.TryParseAs<int>())
               .Except(x => x is null)
               .Select(x => x.Value)
               .ToArray();

            if (numberParts.IsSingle())
                return numberParts.First();

            if (parts.Length < 4)
                return new BorderRadius { topLeft = numberParts[0], bottomRight = numberParts[0], topRight = numberParts[1], bottomLeft = numberParts[1] };

            if (numberParts.None()) return new BorderRadius();

            return new BorderRadius { TopLeft = numberParts[0], TopRight = numberParts[1], BottomRight = numberParts[2], BottomLeft = numberParts[3] };
        }

        public override int GetHashCode() => ToString().GetHashCode();

        public static bool operator !=(BorderRadius @this, BorderRadius another) => !(@this == another);

        public static bool operator ==(BorderRadius @this, BorderRadius another) => @this?.Equals(another) ?? another is null;

        public override bool Equals(object obj)
        {
            var another = obj as BorderRadius;

            if (another is null) return false;

            if (topLeft != another.topLeft) return false;
            if (topRight != another.topRight) return false;
            if (bottomLeft != another.bottomLeft) return false;
            if (bottomRight != another.bottomRight) return false;

            return true;
        }
    }
}