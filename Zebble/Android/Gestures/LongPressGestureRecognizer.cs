namespace Zebble.AndroidOS
{
    using System;
    using System.Timers;
    using Android.Views;
    using Zebble.Device;

    public class LongPressGestureRecognizer : BaseGestureRecognizer
    {
        Timer MultiTapTimer;
        DateTime StartTime;

        static readonly TimeSpan TAP_DURATION_THRESHOLD = TimeSpan.FromMilliseconds(500);
        readonly int TapMoveThreshhold = Scale.ToDevice(5);
        private int CurrentTapCount;
        private readonly int NumberOfTouchesRequired = 1;
        public Action<Zebble.View, Point, int> OnGestureRecognized;

        public override void ProcessMotionEvent(Zebble.View handler, MotionEvent eventArgs)
        {
            if (eventArgs.Action == MotionEventActions.Down) OnDown(eventArgs);
            else if (State == GestureRecognizerState.Cancelled ||
                     State == GestureRecognizerState.Ended ||
                     State == GestureRecognizerState.Failed) return;

            else if (eventArgs.ActionMasked == MotionEventActions.Cancel) State = GestureRecognizerState.Cancelled;

            else if (eventArgs.ActionMasked == MotionEventActions.Up) OnUp(handler, eventArgs);
        }

        void OnDown(MotionEvent eventArgs)
        {
            StartTime = DateTime.UtcNow;
            State = (eventArgs.PointerCount == NumberOfTouchesRequired) ? GestureRecognizerState.Began : GestureRecognizerState.Failed;
            CurrentTapCount = 0;
            PointerId = eventArgs.GetPointerId(0);
            FirstTouchPoint = eventArgs.GetPoint();

            if (NumberOfTouchesRequired > 1 && State == GestureRecognizerState.Began)
                ResetMultiTapTimer(isActive: true);
        }

        void OnUp(Zebble.View handler, MotionEvent eventArgs)
        {
            SecondTouchPoint = eventArgs.GetPoint();
            NumberOfTouches = eventArgs.PointerCount;

            if ((DateTime.UtcNow - StartTime) < TAP_DURATION_THRESHOLD)
            {
                State = GestureRecognizerState.Failed;
                return;
            }

            if (NumberOfTouches < NumberOfTouchesRequired)
            {
                State = GestureRecognizerState.Failed;
                return;
            }

            CurrentTapCount++;

            if (NumberOfTouchesRequired == 1)
            {
                if (Math.Abs(FirstTouchPoint.X - SecondTouchPoint.X) <= TapMoveThreshhold &&
                    Math.Abs(FirstTouchPoint.Y - SecondTouchPoint.Y) <= TapMoveThreshhold)
                {
                    OnRecognized(handler);
                }
            }
            else if (CurrentTapCount == NumberOfTouchesRequired)
            {
                NumberOfTouches = 1;
                OnRecognized(handler);
                ResetMultiTapTimer(isActive: false);
                CurrentTapCount = 0;
            }
        }

        void MultiTapTimerElapsed(object sender, ElapsedEventArgs e)
        {
            CurrentTapCount = 0;
            ResetMultiTapTimer(isActive: false);
        }

        void ResetMultiTapTimer(bool isActive)
        {
            if (MultiTapTimer != null)
            {
                MultiTapTimer.Elapsed -= MultiTapTimerElapsed;
                MultiTapTimer.AutoReset = true;
                MultiTapTimer.Stop();
                MultiTapTimer.Close();
            }

            if (isActive)
            {
                State = GestureRecognizerState.Possible;
                MultiTapTimer = new Timer { Interval = TAP_DURATION_THRESHOLD.TotalMilliseconds * (NumberOfTouchesRequired - 1), AutoReset = false };
                MultiTapTimer.Elapsed += MultiTapTimerElapsed;
                MultiTapTimer.Start();
            }
            else
            {
                CurrentTapCount = 0;
                if (State == GestureRecognizerState.Possible) State = GestureRecognizerState.Failed;
            }
        }

        public override void OnRecognized(Zebble.View handler)
        {
            if (OnGestureRecognized != null)
                OnGestureRecognized.Invoke(handler, FirstTouchPoint, NumberOfTouches);
        }
    }
}