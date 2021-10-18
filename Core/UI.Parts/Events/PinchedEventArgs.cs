namespace Zebble
{
    public class PinchedEventArgs
    {
        /// <summary>The view on which this event was triggered.</summary>
        public View View { get; }

        /// <summary>Location of the first touch. For Windows desktop, it's the location of the mouse.</summary>
        public Point Touch1 { get; }

        /// <summary>Location of the second touch. For Windows desktop, it's the location of the mouse.</summary>
        public Point Touch2 { get; }

        /// <summary>The center point between the two touch points.</summary>
        public Point Center { get; }

        /// <summary>
        /// Determines whether the selected area expanded (zoom in) or shrank (zoomed out).
        /// It will be true if ChangeScale is > 1.
        /// </summary>
        public bool Expanded => ChangeScale > 1;

        /// <summary>
        /// Gets the change in the scale of the pinching.
        /// For example for slow zoom in it may be 1.15. For slow zoom out it can be 0.85.
        /// For fast zoom in it may be 1.75. For fast zoom out it can be 0.25. You get the idea.
        /// </summary>
        public float ChangeScale { get; }

        /// <summary>
        /// Determines whether the selected area expanded (zoom in) or shrank (zoomed out).
        /// It's always the opposite of Expanded.
        /// </summary>
        public bool Shrank => !Expanded;

        public PinchedEventArgs(View view, Point touch1, Point touch2, float changeScale)
        {
            View = view;
            Touch1 = touch1;
            Touch2 = touch2;
            ChangeScale = changeScale;

            Center = new Point((touch1.X + touch2.X) / 2f, (touch1.Y + touch2.Y) / 2f);
        }

        public override string ToString()
        {
            return $"{(Expanded ? "Expanded" : "Shrank")} to {ChangeScale} around {Center}";
        }
    }
}