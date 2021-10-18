namespace Zebble
{
    public class UserRotatingEventArgs
    {
        /// <summary>The view on which this event was triggered.</summary>
        public View View { get; }

        /// <summary>Location of the first touch. For Windows desktop, it's the location of the mouse.</summary>
        public Point Touch1 { get; }

        /// <summary>Location of the second touch. For Windows desktop, it's the location of the mouse.</summary>
        public Point Touch2 { get; }

        /// <summary>The center point between the two touch points.</summary>
        public Point Center { get; }

        /// <summary>The degrees (clock-wise) by which the object was rotated.</summary>
        public float Degrees { get; }

        public UserRotatingEventArgs(View view, Point touch1, Point touch2, float degrees)
        {
            View = view;
            Touch1 = touch1;
            Touch2 = touch2;
            Degrees = degrees;

            Center = new Point((touch1.X + touch2.X) / 2f, (touch1.Y + touch2.Y) / 2f);
        }

        public override string ToString() => Degrees + " around " + Center;
    }
}