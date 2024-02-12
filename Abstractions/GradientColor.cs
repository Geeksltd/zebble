namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Olive;

    public class GradientColor : Color
    {
        const float DEFAULT_CHANGE_POINT = 35;

        public List<Item> Items { get; set; } = new();
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
        public Direction GradientDirection { get; set; }

        /// <param name="startPoint">from 0,0 to 1,1</param>
        /// <param name="endPoint">from 0,0 to 1,1</param>
        public GradientColor(Point startPoint, Point endPoint) : this(Direction.Down)
        {
            Alpha = byte.MaxValue;
            StartPoint = startPoint;
            EndPoint = endPoint;
        }

        public GradientColor(Color startColor, Color endColor, float changeAtPercentage = DEFAULT_CHANGE_POINT, Direction direction = Direction.Down) : this(direction)
        {
            Add(startColor, changeAtPercentage);
            EndWith(endColor);
        }

        public GradientColor(Direction direction = Direction.Down) : base(byte.MaxValue, byte.MaxValue, byte.MaxValue)
        {
            GradientDirection = direction;
            Alpha = byte.MaxValue;

            switch (direction)
            {
                case Direction.Right: StartPoint = new Point(0, 0); EndPoint = new Point(1, 0); break;
                case Direction.Down: StartPoint = new Point(0, 0); EndPoint = new Point(0, 1); break;
                case Direction.DiagonalDown: StartPoint = new Point(0, 0); EndPoint = new Point(1, 1); break;
                case Direction.DiagonalUp: StartPoint = new Point(0, 1); EndPoint = new Point(1, 0); break;
                default: throw new NotSupportedException();
            }
        }

        internal static GradientColor ParseFromCss(string text)
        {
            try
            {
                var parts = text.TrimStart("linear-gradient(").TrimEnd(")").Split(',').Trim().ToList();

                var angle = parts.FirstOrDefault().OrEmpty().ToLower();

                if (angle.StartsWith("to "))
                {
                    angle = angle.TrimStart("to ").Trim();

                    switch (angle)
                    {
                        case "right": angle = "Right"; break;
                        case "bottom": angle = "Down"; break;
                        case "top right": angle = "DiagonalUp"; break;
                        case "bottom right": angle = "DiagonalDown"; break;
                        default: throw new NotSupportedException("Angle " + angle + " is not supported.");
                    }
                }
                else angle = null;

                if (angle.HasValue()) parts.RemoveAt(0);

                var direction = angle.TryParseAs<Direction>() ?? Direction.Right;

                var result = new GradientColor(direction);

                for (var i = 0; i < parts.Count; i++)
                {
                    var stop = (100f + i) / (parts.Count + 1f);
                    if (i == parts.Count - 1) stop = 100;
                    result.Add(Parse(parts[i]), stop);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new FormatException("Failed to parse '" + text + "' to a GradientColor: " + ex.Message);
            }
        }

        internal static GradientColor ParseFromParts(IEnumerable<string> elements)
        {
            var result = new GradientColor();

            foreach (var e in elements)
            {
                var color = Parse(e.RemoveFrom(" to "));
                var percent = e.RemoveBeforeAndIncluding(" to ").TrimEnd("%").To<float>();
                result.Add(color, percent);
            }

            return result;
        }

        public override int GetHashCode() => ToString().GetHashCode();

        public override string ToString() => Items.ToString(" | ");

        public override bool Equals(object obj)
        {
            var another = obj as GradientColor;
            if (another is null) return false;

            return ToString() == another.ToString();
        }

        /// <param name="stopAtPercentage">0 to 100</param>
        public GradientColor Add(Color color, float stopAtPercentage)
        {
            Items.Add(new Item(color, stopAtPercentage));
            return this;
        }

        public GradientColor EndWith(Color color) => Add(color, 100);

        public enum Direction { Right, Down, DiagonalDown, DiagonalUp };

        public class Item
        {
            public Color Color { get; set; }
            public float StopAtPercentage { get; set; }
            public Item(Color color, float percentage) { Color = color; StopAtPercentage = percentage; }

            public override string ToString() => $"{Color} to {StopAtPercentage}%";
        }
    }
}