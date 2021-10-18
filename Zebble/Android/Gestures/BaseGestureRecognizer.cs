using Android.Views;

namespace Zebble.AndroidOS
{
    public abstract class BaseGestureRecognizer : Java.Lang.Object
    {
        protected int NumberOfTouches, PointerId = -1;
        protected Point FirstTouchPoint, SecondTouchPoint;
        GestureRecognizerState state;
        public Android.Views.View NativeView;

        public GestureRecognizerState State
        {
            get => state;
            set
            {
                state = value;

                if (state == GestureRecognizerState.Cancelled ||
                    state == GestureRecognizerState.Ended ||
                    state == GestureRecognizerState.Failed)
                {
                    OnFinished();
                }
            }
        }

        protected virtual void OnFinished() => PointerId = -1;

        public virtual void OnRecognized(View handler)
        {
            state = GestureRecognizerState.Recognized;
        }

        public abstract void ProcessMotionEvent(View handler, MotionEvent eventArg);
    }
}