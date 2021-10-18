namespace Zebble.AndroidOS
{
    using System;
    using Android.Views;
    using Olive;

    public class PinchGestureRecognizer : BaseGestureRecognizer
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

            var oldDistance = OldFirstTouchPoint.DistanceTo(OldSecondTouchPoint);

            if (oldDistance > 0)
            {
                var newDistance = FirstTouchPoint.DistanceTo(SecondTouchPoint);
                var changeScale = newDistance / oldDistance;

                OnGestureRecognized?.Invoke(handler, FirstTouchPoint, SecondTouchPoint, (float)changeScale);
            }
            else Log.For(this).Warning(oldDistance.ToString());

            OldFirstTouchPoint = FirstTouchPoint;
            OldSecondTouchPoint = SecondTouchPoint;
        }
    }
}