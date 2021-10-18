namespace Zebble.AndroidOS
{
    using System;
    using Android.Views;
    using Olive;

    public class RotationGestureRecognizer : BaseGestureRecognizer
    {
        public Action<Zebble.View, Point, Point, float> OnGestureRecognized;
        Point? OldFirstTouchPoint, OldSecondTouchPoint;

        public override void ProcessMotionEvent(Zebble.View handler, MotionEvent args)
        {
            if (args.PointerCount == 2 && (args.Action.IsAnyDown() || args.Action == MotionEventActions.Move))
            {
                FirstTouchPoint = args.GetPoint(0);
                SecondTouchPoint = args.GetPoint(1);
                if (args.ActionMasked == args.Action) OnRecognized(handler);
                else
                {
                    OldFirstTouchPoint = FirstTouchPoint;
                    OldSecondTouchPoint = SecondTouchPoint;
                }
            }

            if (args.Action.IsAnyUp() || args.Action == MotionEventActions.Cancel)
                OldFirstTouchPoint = OldSecondTouchPoint = null;
        }

        public override void OnRecognized(Zebble.View handler)
        {
            if (OnGestureRecognized is null) return;

            var angle = CalculateAngle();

            if (!angle.AlmostEquals(0))
                OnGestureRecognized?.Invoke(handler, FirstTouchPoint, SecondTouchPoint, angle);

            OldFirstTouchPoint = FirstTouchPoint;
            OldSecondTouchPoint = SecondTouchPoint;
        }

        float CalculateAngle()
        {
            const float FULL_CIRCLE = 360;

            var prevPoint1 = OldFirstTouchPoint ?? FirstTouchPoint;
            var prevPoint2 = OldSecondTouchPoint ?? SecondTouchPoint;

            var angle1 = (float)Math.Atan2((FirstTouchPoint.Y - SecondTouchPoint.Y),
                (FirstTouchPoint.X - SecondTouchPoint.X));

            var angle2 = (float)Math.Atan2((prevPoint1.Y - prevPoint2.Y), (prevPoint1.X - prevPoint2.X));

            return (angle1 - angle2).ToDegreeFromRadians() % (int)FULL_CIRCLE;
        }
    }
}