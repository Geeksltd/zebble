namespace Zebble
{
    using System;
    using System.Linq;
    using UIKit;
    using Olive;

    partial class Renderer
    {
        const int TAP_MOVE_THRESHHOLD = 10;

        PannedEventArgs PreviousReference;

        void AddGestures()
        {
            if (View is null) return;

            UITapGestureRecognizer tapRecognizer = null;

            if (View.Touched.IsHandled()) HandleFirstTouch();

            if (View.Tapped.IsHandled())
            {
                tapRecognizer = new UITapGestureRecognizer(g =>
                {
                    if (IsDead(out var view)) return;
                    UIRuntime.RenderRoot.CurrentDescendants().OfType<TextInput>().Do(x => x.UnFocus());

                    var loc = g.LocationInView(Result);
                    var pos = new Point((float)loc.X, (float)loc.Y);
                    var touches = (int)g.NumberOfTouches;
                    view.RaiseTapped(new TouchEventArgs(view, pos, touches));
                })
                {
                    NumberOfTapsRequired = 1,
                    NumberOfTouchesRequired = 1,
                    CancelsTouchesInView = false,
                };
                Result.AddGestureRecognizer(tapRecognizer);
            }

            if (View.Swiped.IsHandled())
            {
                void raise(Direction direction, int touches)
                {
                    if (IsDead(out var view)) return;
                    view.RaiseSwipped(new SwipedEventArgs(view, direction, touches));
                }

                var swipeRightGestureRecognizer = new UISwipeGestureRecognizer(g => raise(Direction.Right, (int)g.NumberOfTouches))
                {
                    Direction = UISwipeGestureRecognizerDirection.Right
                };

                var swipeLeftGestureRecognizer = new UISwipeGestureRecognizer(g => raise(Direction.Left, (int)g.NumberOfTouches))
                {
                    Direction = UISwipeGestureRecognizerDirection.Left
                };

                Result.AddGestureRecognizer(swipeRightGestureRecognizer);
                Result.AddGestureRecognizer(swipeLeftGestureRecognizer);
            }

            if (View.LongPressed.IsHandled()) HandleLongPressGestures(tapRecognizer);

            if (View.Panning.IsHandled() || View.PanFinished.IsHandled()) HandlePanning();

            if (View.Pinching.IsHandled()) HandlePinching();

            if (View.UserRotating.IsHandled()) HandleRotating();
        }

        UIView GetUltraSuperViewOrSelf(UIView view) => view.Superview == null ? view : GetUltraSuperViewOrSelf(view.Superview);

        void HandleLongPressGestures(UITapGestureRecognizer tapRecognizer)
        {
            var startPoint = new Point();
            var startTime = DateTime.UtcNow;
            var raised = false;
            var moved = false;
            var ultraParent = GetUltraSuperViewOrSelf(Result);

            var longPressGestureRecognizer = new UILongPressGestureRecognizer(g =>
            {
                if (IsDead(out var view)) return;
                var nativePoint = g.LocationInView(ultraParent);

                var point = new Point((float)nativePoint.X, (float)nativePoint.Y);
                var touches = (int)g.NumberOfTouches;

                if (g.State == UIGestureRecognizerState.Began)
                {
                    startPoint = point;
                    startTime = DateTime.UtcNow;
                    raised = false;
                }
                else if (!raised && DateTime.UtcNow.Subtract(startTime) >= 500.Milliseconds())
                {
                    if (Math.Abs(startPoint.X - point.X) >= TAP_MOVE_THRESHHOLD || Math.Abs(startPoint.Y - point.Y) >= TAP_MOVE_THRESHHOLD)
                        moved = true;

                    if (moved) return;

                    view.RaiseLongPressed(new TouchEventArgs(view, point, touches));
                    raised = true;
                }
            })
            {
                MinimumPressDuration = 0,
                Delegate = new LongPressGestureRecognizerDelegate()
            };

            if (tapRecognizer is not null)
                longPressGestureRecognizer.ShouldBeRequiredToFailByGestureRecognizer(tapRecognizer);

            Result.AddGestureRecognizer(longPressGestureRecognizer);
        }

        void HandlePanning()
        {
            var panGestureRecognizer = new UIPanGestureRecognizer(g =>
            {
                if (IsDead(out var view)) return;

                var nativePoint = g.LocationInView(Result.Superview);
                var firstPoint = new Point();
                var fromPoint = new Point();
                var toPoint = new Point();

                var nativeVelocity = g.VelocityInView(g.View);
                var velocityPoint = new Point((float)nativeVelocity.X, (float)nativeVelocity.Y);
                var touches = (int)g.NumberOfTouches;

                if (g.State == UIGestureRecognizerState.Began)
                {
                    firstPoint = fromPoint = toPoint = new Point((float)nativePoint.X, (float)nativePoint.Y);
                }

                if (g.State == UIGestureRecognizerState.Changed)
                {
                    fromPoint = toPoint;
                    toPoint = new Point((float)nativePoint.X, (float)nativePoint.Y);

                    var arg = new PannedEventArgs(view, fromPoint, toPoint, velocityPoint, touches) { PreviousEvent = PreviousReference };
                    PreviousReference = arg;
                    view.RaisePanning(arg);

                    g.SetTranslation(CoreGraphics.CGPoint.Empty, Result);
                }

                if (g.State == UIGestureRecognizerState.Ended)
                {
                    toPoint = new Point((float)nativePoint.X, (float)nativePoint.Y);

                    var arg = new PannedEventArgs(view, firstPoint, toPoint, velocityPoint, touches) { PreviousEvent = PreviousReference };
                    PreviousReference = arg;
                    view.RaisePanFinished(arg);
                }
            });

            var parentScrollView = View.FindParent<ScrollView>();
            if (parentScrollView is not null)
            {
                if (parentScrollView.IsRendered()) AttachToScroller(parentScrollView, panGestureRecognizer);
                else parentScrollView.Rendered.Event += () => AttachToScroller(parentScrollView, panGestureRecognizer);
            }

            Result.AddGestureRecognizer(panGestureRecognizer);
        }

        void AttachToScroller(ScrollView parentScrollView, UIPanGestureRecognizer panGestureRecognizer)
        {
            Thread.UI.Post(() =>
            {
                try
                {
                    var recognizer = parentScrollView.Native()?.GestureRecognizers.OfType<UIPanGestureRecognizer>().SingleOrDefault();
                    if (recognizer is null) return;

                    recognizer.ShouldBeRequiredToFailByGestureRecognizer(panGestureRecognizer);
                    panGestureRecognizer.Delegate = new PanningGestureRecognizerDelegate(Result, parentScrollView, recognizer);
                }
                catch { } // No logging is needed
            });
        }

        void HandlePinching()
        {
            var recognizer = new UIPinchGestureRecognizer(g =>
            {
                if (IsDead(out var view)) return;
                if (g.State == UIGestureRecognizerState.Changed)
                {
                    if (g.NumberOfTouches > 1)
                    {
                        var arg = new PinchedEventArgs(view, g.GetTouchPoint(0), g.GetTouchPoint(1), (float)g.Scale);
                        view.RaisePinching(arg);
                    }
                }

                g.Scale = 1; // Reset
            });

            Result.AddGestureRecognizer(recognizer);
        }

        void HandleRotating()
        {
            var rotateGesture = new UIRotationGestureRecognizer(g =>
            {
                if (IsDead(out var view)) return;
                if (g.State == UIGestureRecognizerState.Began || g.State == UIGestureRecognizerState.Changed)
                {
                    var degrees = ((float)g.Rotation).ToDegreeFromRadians();
                    var arg = new UserRotatingEventArgs(view, g.GetTouchPoint(0), g.GetTouchPoint(1), degrees);
                    view.RaiseUserRotating(arg);
                }

                g.Rotation = 0; // Reset
            });

            Result.AddGestureRecognizer(rotateGesture);
        }

        void HandleFirstTouch()
        {
            var touchGesture = new IOS.UIFirstTouchGestureRecognizer(point =>
             {
                 if (IsDead(out var view)) return;
                 var arg = new TouchEventArgs(view, new Point((float)point.X, (float)point.Y), 1);
                 view.RaiseTouched(arg);
             });

            Result.AddGestureRecognizer(touchGesture);
        }

        internal static UIGestureRecognizer AddHardwareBackGesture()
        {
            static async void RaiseHardwareBack() => await Nav.OnHardwareBack();

            return new UIScreenEdgePanGestureRecognizer(RaiseHardwareBack)
            {
                Edges = UIRectEdge.Left
            };
        }
    }

    class LongPressGestureRecognizerDelegate : UIGestureRecognizerDelegate
    {
        public LongPressGestureRecognizerDelegate() { }

        public override bool ShouldRecognizeSimultaneously(UIGestureRecognizer _, UIGestureRecognizer __) => true;

        public override bool ShouldBegin(UIGestureRecognizer recognizer) => true;
    }

    class PanningGestureRecognizerDelegate : UIGestureRecognizerDelegate
    {
        readonly UIView View;
        readonly ScrollView ContainerScrollView;
        readonly UIPanGestureRecognizer ScrollViewPanGesture;

        public PanningGestureRecognizerDelegate(UIView view = null, ScrollView containerScrollView = null, UIPanGestureRecognizer scrollViewPanGesture = null)
        {
            View = view;
            ContainerScrollView = containerScrollView;
            ScrollViewPanGesture = scrollViewPanGesture;
        }

        public override bool ShouldRecognizeSimultaneously(UIGestureRecognizer _, UIGestureRecognizer __) => true;

        public override bool ShouldBegin(UIGestureRecognizer recognizer)
        {
            if (recognizer is not UIPanGestureRecognizer panRecognizer) return true;
            if (ContainerScrollView.EnableScrolling == false) return true;

            var velocity = panRecognizer.VelocityInView(View);

            if (recognizer == ScrollViewPanGesture)
            {
                // For the vertical scrollview, if it's more vertical than
                // horizontal, return true; else false
                return Math.Abs(velocity.Y) > Math.Abs(velocity.X);
            }
            else
            {
                // For the horizontal pan view, if it's more horizontal than
                // vertical, return true; else false
                return Math.Abs(velocity.Y) < Math.Abs(velocity.X);
            }
        }
    }
}