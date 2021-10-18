namespace Zebble
{
    using System;
    using System.Linq;
    using Olive;

    [EscapeGCop("X and Y are meaningful in this context.")]
    public struct Point
    {
        public float X, Y;

        public bool IsEmpty => X == 0 && Y == 0;

        public Point(float x, float y) { X = x; Y = y; }

        public override string ToString() => $"({X},{Y})";

        public static implicit operator Point(string text) => Parse(text);

        public static Point Parse(string text)
        {
            text = text.OrEmpty().TrimStart("(").TrimEnd(")").Trim();

            var parts = text.Split(',').Trim().ToArray();

            try
            {
                return new Point(parts[0].To<float>(), parts[1].To<float>());
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse '{text}' to Polygon.Point.", ex);
            }
        }

        /// <summary>
        /// Euclidean distance for two points
        /// </summary>
        public double DistanceTo(Point point)
        {
            var xDiff = X - point.X;
            var yDiff = Y - point.Y;

            return Math.Sqrt(xDiff.ToThePowerOf(2) + yDiff.ToThePowerOf(2));
        }

        /// <summary>
        /// Returns a new point with X = myX + another's X (and the same for Y).
        /// </summary>
        public Point Add(Point another) => new Point(X + another.X, Y + another.Y);

        /// <summary>
        /// Returns a new point with X = myX - another's X (and the same for Y).
        /// </summary>
        public Point Subtract(Point another) => new Point(X - another.X, Y - another.Y);

        /// <summary>
        /// Returns a new point with X and Y being the sum of mine and the specified additional value.
        /// </summary>
        public Point Add(float addX, float addY) => new Point(X + addX, Y + addY);
    }
}