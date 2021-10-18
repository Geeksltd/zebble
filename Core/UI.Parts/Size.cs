namespace Zebble
{
    using System;
    using Olive;

    public struct Size
    {
        public Size(float width, float height)
        {
            Width = width;
            Height = height;
        }

        public float Width, Height;

        public override string ToString() => Width + ", " + Height;

        public bool AlmostEquals(Size another) => AlmostEquals(another.Width, another.Height);

        public bool AlmostEquals(float width, float height) => Width.AlmostEquals(width) && Height.AlmostEquals(height);

        /// <summary>
        /// If necessary, reduces the size to fit within a specified maximum limit in a way that both width and height of the result are less than or equal to the smallest of this and the specified max.
        /// </summary>
        /// <param name="keepAspectRatio">Whether the resulting size's aspect ratio should be the same as the original size.</param>
        public Size LimitTo(Size max, bool keepAspectRatio = true)
        {
            if (keepAspectRatio)
            {
                var ratioX = (double)max.Width / Width;
                var ratioY = (double)max.Height / Height;
                var ratio = Math.Min(ratioX, ratioY);

                var newWidth = (int)(Width * ratio);
                var newHeight = (int)(Height * ratio);

                return new Size(newWidth, newHeight);
            }
            else
            {
                return new Size(Math.Min(Width, max.Width), Math.Min(Height, max.Height));
            }
        }

        /// <summary>
        /// Gets a new size with the same width and height as the specified value.
        /// </summary>
        public static Size Square(float value) => new Size(value, value);

        /// <summary>
        /// Gets a new size with the width and height multiplied by the specified multiplier.
        /// </summary>
        public Size Scale(float multiplier) => new Size(Width * multiplier, Height * multiplier);

        public Size Round(int digits = 0) => new Size(Width.Round(digits), Height.Round(digits));

        public Size RoundUp() => new Size((float)Math.Ceiling(Width), (float)Math.Ceiling(Height));

        public Size RoundDown() => new Size((float)Math.Floor(Width), (float)Math.Floor(Height));

        /// <summary>
        /// Returns true if both width and height are larger than another specified size.
        /// </summary> 
        public bool IsLargerThan(Size another) => Width > another.Width && Height > another.Height;

        /// <summary>
        /// Returns true if both width and height are smaller than another specified size.
        /// </summary> 
        public bool IsSmallerThan(Size another) => Width < another.Width && Height < another.Height;

        /// <summary>
        /// Gets width divided by height.
        /// </summary>
        public float AspectRatio()
        {
            if (Height.AlmostEquals(0))
            {
                if (Width.AlmostEquals(0)) return 1;
                return (Width * Height) < 0 ? float.NegativeInfinity : float.PositiveInfinity;
            }

            return Width / Height;
        }

        /// <summary>
        /// Determines whether width is bigger than height.
        /// </summary>
        public bool IsLandscape() => Width > Height;

        /// <summary>
        /// Returns width * height.
        /// </summary>
        public float Area() => Width * Height;
    }
}