namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class PannedEventArgs
    {
        /// <summary>
        /// The view on which this event was triggered.
        /// </summary>
        public View View { get; }

        /// <summary>
        /// For Panning event, it's the start of the latest fragment of panning.
        /// For PanFinished event, it's the very start of the whole panning process.
        /// </summary>
        public Point From { set; get; }

        /// <summary>
        /// For Panning event, it's the end of the latest fragment of panning which is usually very close to the From value (within one or two pixes).
        /// For PanFinished event, it's the very last position when the touch was released.
        /// </summary>
        public Point To { set; get; }

        /// <summary>
        /// Gets the number of touches engaged in this panning gesture.
        /// </summary>
        public int Touches { set; get; }

        /// <summary>
        /// Gets the speed of panning (points per second) on each of the X and Y axises.
        /// </summary>
        public Point Velocity { get; set; }

        /// <summary>
        /// Gets the approximate angle of the panning direction based on the most recent movements.
        /// For example direct panning up will be 0, right: 90, bottom: 180 and left:270.
        /// </summary>
        public float Angle => GetAllPrevoiusPointsAngle();

        /// <summary>
        /// An instance of previous pan event.
        /// </summary>
        public PannedEventArgs PreviousEvent { get; set; }

        public PannedEventArgs(View view, Point from, Point to, Point velocity, int numberOfTouches)
        {
            View = view;
            From = from;
            To = to;
            Velocity = velocity;
            Touches = numberOfTouches;
        }

        internal float GetAngle(Point from, Point to)
        {
            var destPointX = from.X - to.X;
            var destPointY = from.Y - to.Y;
            if (destPointX == 0 && destPointY > 0) return 90.0f;
            else if (destPointX == 0 && destPointY < 0) return 270.0f;
            var theta = Math.Atan2(destPointY, destPointX);
            theta *= 180 / Math.PI;

            return (float)theta;
        }

        internal float GetAllPrevoiusPointsAngle(PannedEventArgs parent = null, int index = 0)
        {
            float angle;
            var selfAngle = GetAngle(From, To);
            var points = new List<float>();
            index += 1;

            if (index <= 10)
            {
                if (parent is null)
                {
                    if (PreviousEvent is null) return selfAngle;
                    angle = GetAllPrevoiusPointsAngle(PreviousEvent, index);
                }
                else
                {
                    selfAngle = GetAngle(parent.From, parent.To);
                    if (parent.PreviousEvent is null) return selfAngle;
                    angle = GetAllPrevoiusPointsAngle(parent.PreviousEvent, index);
                }
            }
            else angle = selfAngle;

            points.Add(angle);
            points.Add(selfAngle);
            return points.Average();
        }
    }
}