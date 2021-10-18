namespace Zebble.AndroidOS
{
    using System;
    using Android.Views;
    using Zebble.Device;
    using Olive;

    public class PanGestureRecognizer : BaseGestureRecognizer
    {
        public Action<Zebble.View, Point, Point, Point, int> OnGestureRecognized;

        int MINIMUM_MOVE_DISTANCE = 5;

        Point LatestPoint, TargetPositionAtStart, PanningVelocity, MaxPanningVelocity;
        VelocityTracker vTracker;
        VelocityTracker VTracker => vTracker ?? (vTracker = VelocityTracker.Obtain());
        Zebble.View PanTarget;
        ScrollView HaltedParentScrollView;
        Func<MotionEvent, Zebble.View> HandlerDetector;
        PannedEventArgs PreviousEvent;
        bool RaisedPanned, IsStarted, ScrollBarDecided;

        public PanGestureRecognizer(Func<MotionEvent, Zebble.View> handlerDetector) => HandlerDetector = handlerDetector;

        public override void ProcessMotionEvent(Zebble.View handler, MotionEvent eventArgs)
        {
            switch (eventArgs.Action)
            {
                case MotionEventActions.Down:
                    StartPanning(eventArgs); break;

                case MotionEventActions.Move:
                case MotionEventActions.Scroll:
                case MotionEventActions.Cancel:
                    OnMove(eventArgs); break;

                case MotionEventActions.Up:
                case MotionEventActions.Outside:
                    FinishPanning(eventArgs); break;

                default: break;
            }

            if (eventArgs.Action == MotionEventActions.Cancel)
            {
                ReleaseHaltedScrollbar();
                IsStarted = false;
            }
        }

        void StartPanning(MotionEvent eventArgs)
        {
            PanTarget = HandlerDetector(eventArgs);
            if (PanTarget is null) return;

            IsStarted = false;
            RaisedPanned = false;
            ReleaseHaltedScrollbar();
            PointerId = eventArgs.GetPointerId(0);

            LatestPoint = FirstTouchPoint = new Point(eventArgs.RawX, eventArgs.RawY);
            NumberOfTouches = eventArgs.PointerCount;

            var viewLocationOnScreen = new int[2];
            NativeView.GetLocationOnScreen(viewLocationOnScreen);

            VTracker.Clear();
            PanningVelocity = MaxPanningVelocity = new Point();

            TargetPositionAtStart = Scale.ToDevice(new Point(PanTarget.CalculateAbsoluteX(), PanTarget.CalculateAbsoluteY()));
        }

        void TrackVelocity(MotionEvent ev)
        {
            LatestPoint = new Point(ev.RawX, ev.RawY);
            VTracker.AddMovement(ev);
            VTracker.ComputeCurrentVelocity(1);
            PanningVelocity = new Point(VTracker.XVelocity.Round(2), VTracker.YVelocity.Round(2));

            if (Math.Abs(PanningVelocity.X) > Math.Abs(MaxPanningVelocity.X))
                MaxPanningVelocity.X = PanningVelocity.X;

            if (Math.Abs(PanningVelocity.Y) > Math.Abs(MaxPanningVelocity.Y))
                MaxPanningVelocity.Y = PanningVelocity.Y;
        }

        void OnMove(MotionEvent eventArgs)
        {
            var handler = PanTarget;
            if (handler is null) return;

            NumberOfTouches = eventArgs.PointerCount;

            var from = LatestPoint;
            TrackVelocity(eventArgs);

            if (!IsStarted)
            {
                var move = Scale.ToZebble((float)FirstTouchPoint.DistanceTo(LatestPoint));
                if (move < MINIMUM_MOVE_DISTANCE) return;
                IsStarted = true;
                HaltParentScroller();
            }

            var fromPoint = from.Subtract(TargetPositionAtStart).ToZebble();
            var toPoint = LatestPoint.Subtract(TargetPositionAtStart).ToZebble();

            var arg = new PannedEventArgs(handler, fromPoint, toPoint, PanningVelocity, NumberOfTouches) { PreviousEvent = PreviousEvent };

            PreviousEvent = arg;

            RaisedPanned = true;
            handler.RaisePanning(arg);
        }

        void HaltParentScroller()
        {
            if (ScrollBarDecided) return;

            var scrollView = PanTarget?.FindParent<ScrollView>();
            if (scrollView is null) return;

            if (!scrollView.EnableScrolling) return; // Not mine to change.

            var swipeDirection = Math.Abs(PanningVelocity.Y) > Math.Abs(PanningVelocity.X) ? RepeatDirection.Vertical : RepeatDirection.Horizontal;

            if (scrollView.Direction != swipeDirection)
            {
                HaltedParentScrollView = scrollView;
                scrollView.EnableScrolling = false;
            }

            ScrollBarDecided = true;
        }

        void ReleaseHaltedScrollbar()
        {
            if (HaltedParentScrollView != null)
                HaltedParentScrollView.EnableScrolling = true;

            HaltedParentScrollView = null;
            ScrollBarDecided = false;
        }

        void FinishPanning(MotionEvent ev)
        {
            IsStarted = false;
            ReleaseHaltedScrollbar();
            var handler = PanTarget;
            if (handler is null) return;
            if (!RaisedPanned) return;

            PanTarget = null;
            RaisedPanned = false;

            var fromPoint = FirstTouchPoint.Subtract(TargetPositionAtStart).ToZebble();
            var toPoint = LatestPoint.Subtract(TargetPositionAtStart).ToZebble();

            var arg = new PannedEventArgs(handler, fromPoint, toPoint, PanningVelocity, NumberOfTouches)
            { PreviousEvent = PreviousEvent };

            if (ev.EventTime - ev.DownTime < 200)
                arg.Velocity = MaxPanningVelocity;

            PreviousEvent = null;

            ReleaseTracker();
            handler.RaisePanFinished(arg);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ReleaseTracker();
            base.Dispose(disposing);
        }

        void ReleaseTracker()
        {
            var tracker = VTracker;
            vTracker = null;
            if (tracker.IsAlive())
            {
                tracker.Clear();
                tracker.Recycle();
            }
        }

        public override void OnRecognized(Zebble.View handler) => throw new NotImplementedException();
    }
}