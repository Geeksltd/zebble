namespace Zebble.AndroidOS
{
    using System;
    using Android.Views;
    using Zebble.Device;

    public class SwipeGestureRecognizer : BaseGestureRecognizer
    {
        const int MINIMUM_SWIPE_DISTANCE = 10, MAX_SWIPE_DURATION = 500;
        Direction Direction;
        DateTime StartTime;

        internal Action<Zebble.View, Direction, int> OnGestureRecognized;

        public override void ProcessMotionEvent(Zebble.View handler, MotionEvent eventArgs)
        {
            if (eventArgs.Action == MotionEventActions.Down)
            {
                OnDown(eventArgs);
                if (State == GestureRecognizerState.Began) PointerId = eventArgs.GetPointerId(0);
                return;
            }

            if (State == GestureRecognizerState.Cancelled || State == GestureRecognizerState.Ended
                || State == GestureRecognizerState.Failed) return;

            if (eventArgs.ActionMasked == MotionEventActions.Up ||
                // For some reason in Zommable scroll view, Cancel is raised instead of Up
                eventArgs.ActionMasked == MotionEventActions.Cancel)
                OnUp(handler, eventArgs);
        }

        void OnDown(MotionEvent eventArgs)
        {
            State = GestureRecognizerState.Began;
            FirstTouchPoint = eventArgs.GetPoint();
            StartTime = DateTime.UtcNow;
        }

        void OnUp(Zebble.View handler, MotionEvent eventArgs)
        {
            if (FirstTouchPoint.X == 0 && FirstTouchPoint.Y == 0) return;

            var tooSlow = (DateTime.UtcNow - StartTime).Milliseconds > MAX_SWIPE_DURATION;
            if (tooSlow) return;

            SecondTouchPoint = eventArgs.GetPoint();
            var velocityX = Scale.ToZebble(SecondTouchPoint.X - FirstTouchPoint.X);
            var velocityY = Scale.ToZebble(SecondTouchPoint.Y - FirstTouchPoint.Y);

            if (Math.Abs(velocityX) < MINIMUM_SWIPE_DISTANCE && Math.Abs(velocityY) < MINIMUM_SWIPE_DISTANCE) return;

            Direction = GetSwipeDirection(velocityX, velocityY);
            NumberOfTouches = eventArgs.PointerCount;

            OnRecognized(handler);
        }

        Direction GetSwipeDirection(double velocityX, double velocityY)
        {
            var isHorizontalSwipe = Math.Abs(velocityX) > Math.Abs(velocityY);
            if (isHorizontalSwipe) return velocityX > 0 ? Direction.Right : Direction.Left;
            else return velocityY > 0 ? Direction.Down : Direction.Up;
        }

        public override void OnRecognized(Zebble.View handler)
        {
            OnGestureRecognized?.Invoke(handler, Direction, NumberOfTouches);
        }
    }
}