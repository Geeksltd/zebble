namespace Zebble
{
    public class TouchEventArgs
    {
        /// <summary>
        /// The view on which this event was triggered.
        /// </summary>
        public View View { get; }

        public Point Point { get; }

        /// <summary>
        /// Gets the number of touches engaged in this touch gesture.
        /// </summary>
        public int Touches { get; }

        public TouchEventArgs(View view, Point point, int numberOfTouches)
        {
            View = view;
            Point = point;
            Touches = numberOfTouches;
        }
    }
}