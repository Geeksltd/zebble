namespace Zebble.AndroidOS
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using Android.Runtime;
    using Android.Views;
    using Zebble.Device;
    using Olive;
    using Android.Widget;

    public interface IGestureView
    {
        Zebble.View GetHostView();

        ViewGroup GetGestureLayout();

        Zebble.View DetectHandler(MotionEvent ev, Zebble.View view = null);
    }

    public class AndroidGestureView : AndroidGestureView<FrameLayout>
    {
        public AndroidGestureView(Zebble.View view) : base(view) { }
    }

    public class AndroidGestureView<TLayout> : FrameLayout, IGestureView where TLayout : ViewGroup
    {
        static readonly ConcurrentDictionary<long, WeakReference<Zebble.View>> RecentHandlers = new();

        /// <summary>It's either the root view or a scroll view.</summary>
        Zebble.View Host;

        protected TLayout LayoutContainer { get; set; }

        // The actual value of ScrollView.EnableScrolling as set by the virtual dom.
        // We need to retain the original value, as later on we might have to change it during some swipe events.
        bool? OriginalScrollEnableScrolling;

        ConcurrentList<BaseGestureRecognizer> Recognizers = new();

        [Preserve]
        public AndroidGestureView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public AndroidGestureView(Zebble.View view) : base(Renderer.Context)
        {
            Host = view;

            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);

            AddGestureRecognizer(new TapGestureRecognizer { OnGestureRecognized = HandleTapped });

            AddGestureRecognizer(new LongPressGestureRecognizer { OnGestureRecognized = HandleLongPressed });

            AddGestureRecognizer(new SwipeGestureRecognizer { OnGestureRecognized = OnSwiped });

            AddGestureRecognizer(new PanGestureRecognizer(x => DetectHandler(x)));

            AddGestureRecognizer(new PinchGestureRecognizer { OnGestureRecognized = OnPinched });

            AddGestureRecognizer(new RotationGestureRecognizer { OnGestureRecognized = OnRotated });
        }

        public override ViewGroup.LayoutParams LayoutParameters
        {
            get => base.LayoutParameters;
            set
            {
                if (Host == UIRuntime.RenderRoot &&
                    value.Width != ViewGroup.LayoutParams.MatchParent && value.Height != ViewGroup.LayoutParams.MatchParent)
                    return;

                base.LayoutParameters = value;
            }
        }

        TextInput[] FindTextInputs()
        {
            var result = UIRuntime.RenderRoot.AllDescendents().OfType<TextInput>().ToArray();
            return result;
        }

        void HandleTapped(Zebble.View handler, Point point, int touches)
        {
            HideKeyBoard();

            point = point.RelativeTo(handler);
            handler.RaiseTapped(new Zebble.TouchEventArgs(handler, point, touches));
        }

        void HandleTouched(Zebble.View handler, Point point)
        {
            point = point.RelativeTo(handler);
            var args = new Zebble.TouchEventArgs(handler, point, 1);
            handler.RaiseTouched(args);
        }

        void HandleLongPressed(Zebble.View handler, Point point, int touches)
        {
            HideKeyBoard();
            handler.RaiseLongPressed(new Zebble.TouchEventArgs(handler, point, touches));
        }

        void OnSwiped(Zebble.View handler, Direction direction, int touches)
        {
            if (handler.WithAllParents().None(x => x.CannotHandle(x.Swiped))) return;

            handler.RaiseSwipped(new SwipedEventArgs(handler, direction, touches));

            if (Host is Zebble.ScrollView scrollView && !scrollView.EnableZooming)
                DisableUnwantedScrollingWhenSwiping(scrollView, direction);
        }

        void DisableUnwantedScrollingWhenSwiping(Zebble.ScrollView scrollView, Direction swipeDirection)
        {
            if (OriginalScrollEnableScrolling is null)
                OriginalScrollEnableScrolling = scrollView.EnableScrolling; // Just the first time.

            if (OriginalScrollEnableScrolling.Value)
            {
                if (scrollView.Direction == RepeatDirection.Vertical)
                    // If swiping horizontally, don't scroll:
                    scrollView.EnableScrolling = !swipeDirection.IsHorizontal();
                else
                    // If swiping vertically, don't scroll:
                    scrollView.EnableScrolling = !swipeDirection.IsVertical();
            }

            // Known bug: This will now not allow the user code to change EnableScrolling at runtime
            // TODO: Fix it by differentiating between when we set EnableScrolling here and when it's done
            // by user code. Also this very logic may have to change in that case.
        }

        void OnPinched(Zebble.View handler, Point touch1, Point touch2, float changeScale)
        {
            handler.RaisePinching(new PinchedEventArgs(handler, touch1, touch2, changeScale));
        }

        void OnRotated(Zebble.View handler, Point point1, Point point2, float angle)
        {
            handler.RaiseUserRotating(new UserRotatingEventArgs(handler, point1, point2, angle));
        }

        void AddGestureRecognizer(BaseGestureRecognizer gesture)
        {
            Recognizers.Add(gesture.Set(x => x.NativeView = this));
        }

        void HideKeyBoard()
        {
            Keyboard.Hide();
            FindTextInputs().Do(t => t.UnFocus());
        }

        public Zebble.View DetectHandler(MotionEvent ev, Zebble.View view = null)
        {
            if (Host is null || Host.IsDisposing) return null;

            Zebble.View result = null;

            if (RecentHandlers.TryGetValue(ev.DownTime, out var handlerRef))
                result = handlerRef.GetTargetOrDefault();

            if (result != null && !result.IsDisposing) return result;

            var point = ev.GetPoint();

            point.X = Scale.ToZebble(point.X);
            point.X += Host.CalculateAbsoluteX();

            point.Y = Scale.ToZebble(point.Y);
            point.Y += Host.CalculateAbsoluteY();

            if (Host is Zebble.ScrollView scroll && view is null)
            {
                point.X -= scroll.ScrollX;
                point.Y -= scroll.ScrollY;
            }

            result = new HitTester(point).FindHandler();
            if (result != null)
                RecentHandlers[ev.DownTime] = result.GetWeakReference();

            // Clean up            
            RecentHandlers.RemoveWhere(x => x.Key < ev.DownTime - 10000);

            return result;
        }

        public Zebble.View GetHostView() => Host;

        public ViewGroup GetGestureLayout() => this;

        public override bool OnTouchEvent(MotionEvent ev)
        {
            var handler = DetectHandler(ev);
            if (handler is null) return true;

            if (ev.Action == MotionEventActions.Down)
                HandleTouched(handler, ev.GetPoint());

            foreach (var r in Recognizers)
                r.ProcessMotionEvent(handler, ev);

            // Gesture is controlling by the handler.
            if (handler.Native() is IIndependentZebbleAndroidGestureView)
                return false;

            // Confirm it's handled, so any other underlying gesture view
            // or third party component won't receive the event.
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Recognizers?.Do(x => x.Dispose());
                Recognizers?.Clear();
                Recognizers = null;
                Host = null;
            }

            base.Dispose(disposing);
        }
    }
}