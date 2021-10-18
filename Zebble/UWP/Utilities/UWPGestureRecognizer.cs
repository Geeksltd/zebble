namespace Zebble.UWP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.System;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Input;
    using Olive;

    class UWPGestureRecognizer
    {
        const int MOUSE_WHEEL_DELTA_DEGREES = 8  /*slow it down: */ * 2;

        WeakReference<UIElement> ElementRef;
        WeakReference<View> ViewRef;
        DateTime? RaisedTapped;
        bool WaitUntilPointerReleased;
        readonly HashSet<uint> Pointers = new HashSet<uint>();
        readonly Dictionary<uint, Point> Points = new Dictionary<uint, Point>();
        List<Point> CirclePoints = new List<Point>();

        PannedEventArgs PreviousRefrence;

        UIElement Element => ElementRef.GetTargetOrDefault();
        View View => ViewRef.GetTargetOrDefault();

        public UWPGestureRecognizer(UIElement element, View view)
        {
            ElementRef = element.GetWeakReference();
            ViewRef = view.GetWeakReference();

            if (UIRuntime.IsDevMode)
                if (view.id.IsAnyOf("ZebbleInspectorHighlightMask", "ZebbleInspectorHighlightBorder")) return;

            if (UIRuntime.IsDevMode || view.Tapped.IsHandled()) element.Tapped += Element_Tapped;

            if (view.LongPressed.IsHandled()) element.RightTapped += Element_RightTapped;

            if (view.Panning.IsHandled() || view.Swiped.IsHandled())
            {
                element.ManipulationMode = ManipulationModes.All;
                element.ManipulationDelta += Element_ManipulationDelta;
                HookUpPointerEvents();
            }

            if (view.Touched.IsHandled())
                element.PointerPressed += OnTouched;

            if (view.PanFinished.IsHandled())
            {
                element.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.System;
                element.ManipulationCompleted += Element_ManipulationCompleted;
            }

            element.PointerWheelChanged += Element_PointerWheelChanged;
        }

        async void Element_PointerWheelChanged(object sender, PointerRoutedEventArgs arg)
        {
            var view = View;
            if (view?.Enabled != true) return;

            var pointer = arg.GetCurrentPoint(Element);
            var changeDegrees = pointer.Properties.MouseWheelDelta / MOUSE_WHEEL_DELTA_DEGREES;
            if (changeDegrees == 0) return;

            var point = pointer.Position.ToPoint();

            const float FRAMES = 10f;
            var scaleIncrement = (float)Math.Pow(1f + changeDegrees / 100f, 1f / FRAMES);
            var rotationIncrement = changeDegrees / FRAMES;

            for (var i = 1; i <= FRAMES; i++)
            {
                if (arg.IsControlHeld())
                    view.RaisePinching(new PinchedEventArgs(view, point, point, scaleIncrement));

                if (arg.IsShiftHeld())
                    view.RaiseUserRotating(new UserRotatingEventArgs(view, point, point, rotationIncrement));

                await Task.Delay(Animation.OneFrame);
            }
        }

        void Element_ManipulationCompleted(object _, ManipulationCompletedRoutedEventArgs arg)
        {
            var view = View;
            if (view?.Enabled != true) return;

            if (!view.PanFinished.IsHandled()) return;

            var end = arg.Position.ToPoint();

            var start = new Point(
                (float)(end.X - arg.Cumulative.Translation.X),
                (float)(end.Y - arg.Cumulative.Translation.Y));
            var velocity = new Point((float)arg.Velocities.Linear.X, (float)arg.Velocities.Linear.Y);

            var args = new PannedEventArgs(view, start, end, velocity, NumberOfTouchPoints) { PreviousEvent = PreviousRefrence };
            PreviousRefrence = args;

            view.RaisePanFinished(args);
        }

        int NumberOfTouchPoints => Pointers.Count;

        void HookUpPointerEvents()
        {
            Element.PointerPressed += PointerPressed;
            Element.PointerReleased += PointerReleased;
            Element.PointerExited += PointerExited;
            Element.PointerEntered += PointerEntered;
            Element.PointerCaptureLost += PointerCaptureLost;
            Element.PointerCanceled += PointerCanceled;
            Element.PointerMoved += Element_PointerMoved;
        }

        void Element_PointerMoved(object sender, PointerRoutedEventArgs args)
        {
            var view = View; if (view is null) return;

            if (Pointers.Contains(args.Pointer.PointerId))
                Points[args.Pointer.PointerId] = args.GetCurrentPoint(Element).Position.ToPoint();
        }

        void PointerPressed(object _, PointerRoutedEventArgs args)
        {
            if (Pointers.Contains(args.Pointer.PointerId)) return;
            Pointers.Add(args.Pointer.PointerId);
            Points.Add(args.Pointer.PointerId, args.GetCurrentPoint(Element).Position.ToPoint());
        }

        void PointerReleased(object _, PointerRoutedEventArgs args)
        {
            WaitUntilPointerReleased = false;
            Pointers.Remove(GetPoint(args));
            Points.Remove(GetPoint(args));
        }

        void PointerExited(object sender, PointerRoutedEventArgs args)
        {
            WaitUntilPointerReleased = false;
            Pointers.Remove(GetPoint(args));
            Points.Remove(GetPoint(args));
        }

        void PointerEntered(object sender, PointerRoutedEventArgs args)
        {
            if (Pointers.Contains(args.Pointer.PointerId)) return;
            Pointers.Add(GetPoint(args));
            Points.Add(args.Pointer.PointerId, args.GetCurrentPoint(Element).Position.ToPoint());
        }

        void PointerCaptureLost(object sender, PointerRoutedEventArgs args)
        {
            Pointers.Remove(GetPoint(args));
            Points.Remove(GetPoint(args));
        }

        void PointerCanceled(object _, PointerRoutedEventArgs __)
        {
            WaitUntilPointerReleased = false;
            Pointers.Clear();
            Points.Clear();
        }

        uint GetPoint(PointerRoutedEventArgs args) => args.GetCurrentPoint(Element).PointerId;

        void OnTouched(object sender, PointerRoutedEventArgs args)
        {
            var view = View; if (view is null) return;

            var currentPoint = args.GetCurrentPoint(Element).Position.ToPoint();

            if (view.Touched.IsHandled())
            {
                view.RaiseTouched(new TouchEventArgs(view, currentPoint, 1));
                args.Handled = true;
            }

            if (view.Panning.IsHandled())
            {
                var arg = new PannedEventArgs(view, currentPoint, currentPoint, new Point(), NumberOfTouchPoints) { PreviousEvent = PreviousRefrence };
                PreviousRefrence = arg;
                view.RaisePanning(arg);
                args.Handled = true;
            }
        }

        void Element_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs args)
        {
            var view = View; if (view is null) return;

            if (WaitUntilPointerReleased) return;
            if (args.IsInertial) return;
            if (RaisedTapped >= DateTime.Now.AddMilliseconds(-200)) return;

            if (view.Swiped.IsHandled())
            {
                var total = args.Cumulative.Translation;

                if (Math.Abs(total.X) >= UIRuntime.SwipeThreshold && Math.Abs(total.X) > Math.Abs(total.Y))
                {
                    if (view.Enabled)
                    {
                        WaitUntilPointerReleased = true;
                        var direction = total.X > 0 ? Direction.Right : Direction.Left;
                        var touches = NumberOfTouchPoints;
                        view.RaiseSwipped(new SwipedEventArgs(view, direction, touches));

                        Thread.Pool.Run(() => Task.Delay(500.Milliseconds()).ContinueWith(x => WaitUntilPointerReleased = false));
                    }

                    args.Handled = true;
                    return;
                }
            }

            if (view.Panning.IsHandled())
            {
                if (Math.Abs(args.Cumulative.Expansion) < 0.1)
                {
                    var change = args.Delta.Translation;
                    var start = args.Position.ToPoint();
                    var end = new Point((float)(start.X + change.X), (float)(start.Y + change.Y));

                    var velocity = new Point((float)args.Velocities.Linear.X, (float)args.Velocities.Linear.Y);

                    var arg = new PannedEventArgs(view, start, end, velocity, NumberOfTouchPoints) { PreviousEvent = PreviousRefrence };
                    PreviousRefrence = arg;
                    view.RaisePanning(arg);
                    args.Handled = true;
                }
            }
        }

        void Element_RightTapped(object _, RightTappedRoutedEventArgs arg)
        {
            var view = View;
            if (view?.Enabled != true) return;

            var pos = arg.GetPosition(Element).ToPoint();
            var touches = NumberOfTouchPoints;
            arg.Handled = true;
            view.RaiseLongPressed(new TouchEventArgs(view, pos, touches));
        }

        static bool IsHeld(VirtualKey key)
        {
            var state = CoreWindow.GetForCurrentThread().GetKeyState(key);
            return (state & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
        }

        void Element_Tapped(object sender, TappedRoutedEventArgs args)
        {
            var view = View; if (view is null) return;

            if (UIRuntime.IsDevMode)
            {
                if (IsHeld(VirtualKey.Control))
                {
                    args.Handled = true;
                    RaisedTapped = DateTime.Now;
                    Thread.Pool.RunAction(() => UIWorkBatch.Run(() => UIRuntime.Inspector.Load(view)).GetAwaiter());
                    return;
                }
            }

            if (!view.Tapped.IsHandled()) return;

            var touches = NumberOfTouchPoints;
            var pos = new TouchEventArgs(view, args.GetPosition(Element).ToPoint(), touches);
            view.RaiseTapped(pos);

            UIRuntime.RenderRoot.CurrentDescendants().Where(t => t is TextInput)
            .Except(view).Do(t => (t as TextInput).UnFocus());

            RaisedTapped = DateTime.Now;
            args.Handled = true;
        }

        internal void Dispose()
        {
            var element = Element;
            if (element != null)
            {
                element.PointerPressed -= PointerPressed;
                element.PointerReleased -= PointerReleased;
                element.PointerExited -= PointerExited;
                element.PointerEntered -= PointerEntered;
                element.PointerCaptureLost -= PointerCaptureLost;
                element.PointerCanceled -= PointerCanceled;
                element.PointerMoved -= Element_PointerMoved;
                element.ManipulationDelta -= Element_ManipulationDelta;
                element.PointerPressed -= OnTouched;
                element.ManipulationCompleted -= Element_ManipulationCompleted;
                element.Tapped -= Element_Tapped;
                element.RightTapped -= Element_RightTapped;
                element.PointerWheelChanged -= Element_PointerWheelChanged;
            }

            ElementRef?.SetTarget(null);
            ViewRef?.SetTarget(null);
        }
    }
}