namespace Zebble
{
    public class SwipedEventArgs
    {
        /// <summary>
        /// The view on which this event was triggered.
        /// </summary>
        public View View { get; }

        public Direction Direction { get; }

        /// <summary>
        /// Gets the number of touches engaged in this swipe gesture.
        /// </summary>
        public int Touches { get; }

        public SwipedEventArgs(View view, Direction direction, int numberOfTouches)
        {
            View = view;
            Direction = direction;
            Touches = numberOfTouches;
        }
    }
}