namespace Zebble
{
    using Foundation;
    using System;
    using System.Linq;
    using UIKit;
    using Olive;

    partial class Renderer
    {
        const int TAP_MOVE_THRESHHOLD = 10;

        PannedEventArgs PreviousRefrence;

        void AddGestures()
        {
            if (View is null) return;

            UITapGestureRecognizer tapRecognizer = null;

            if (!(View is TextInput))
            {
                if (View.Touched.IsHandled()) HandleFirstTouch();

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
            { MinimumPressDuration = 0, Delegate = new GestureRecognizerDelegate() };
            longPressGestureRecognizer.ShouldBeRequiredToFailByGestureRecognizer(tapRecognizer);
            Result.AddGestureRecognizer(longPressGestureRecognizer);
        }

        void HandlePanning()
        {
            var secoundPoint = new Point();
            var panGestureRecognizer = new UIPanGestureRecognizer(g =>
            {
                if (IsDead(out var view)) return;

                var nativePoint = g.LocationInView(Result.Superview);
                Point firstPoint;

                var nativeVelocity = g.VelocityInView(g.View);
                var velocityPoint = new Point((float)nativeVelocity.X, (float)nativeVelocity.Y);
                var touches = (int)g.NumberOfTouches;

                if (g.State == UIGestureRecognizerState.Began)
                {
                    secoundPoint = new Point((float)nativePoint.X, (float)nativePoint.Y);
                }

                if (g.State == UIGestureRecognizerState.Changed)
                {
                    firstPoint = secoundPoint;
                    secoundPoint = new Point((float)nativePoint.X, (float)nativePoint.Y);

                    var arg = new PannedEventArgs(view, firstPoint, secoundPoint, velocityPoint, touches) { PreviousEvent = PreviousRefrence };
                    PreviousRefrence = arg;
                    view.RaisePanning(arg);

                    g.SetTranslation(CoreGraphics.CGPoint.Empty, Result);
                }

                if (g.State == UIGestureRecognizerState.Ended)
                {
                    firstPoint = secoundPoint;
                    secoundPoint = new Point((float)nativePoint.X, (float)nativePoint.Y);

                    var arg = new PannedEventArgs(view, firstPoint, secoundPoint, velocityPoint, touches) { PreviousEvent = PreviousRefrence };
                    PreviousRefrence = arg;
                    view.RaisePanFinished(arg);
                }
            });

            var parentScrollView = View.FindParent<ScrollView>();
            if (parentScrollView != null)
            {
                var scrollViewNative = parentScrollView.Native();

                if (scrollViewNative is null)
                    parentScrollView.Rendered.Event += () => attachToScroller();
                else attachToScroller();

                void attachToScroller()
                {
                    var recognizer = scrollViewNative?.GestureRecognizers.SingleOrDefault(gr => (gr as UIPanGestureRecognizer) != null);
                    if (recognizer != null)
                    {
                        recognizer.ShouldBeRequiredToFailByGestureRecognizer(panGestureRecognizer);
                        panGestureRecognizer.Delegate = new GestureRecognizerDelegate(Result, parentScrollView, recognizer as UIPanGestureRecognizer);
                    }
                }
            }
            Result.AddGestureRecognizer(panGestureRecognizer);
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
            async void raiseHardwareBack()
            {
                await Nav.OnHardwareBack();
            }

            return new UIScreenEdgePanGestureRecognizer(raiseHardwareBack) { Edges = UIRectEdge.Left };
        }
    }

    class GestureRecognizerDelegate : UIGestureRecognizerDelegate
    {
        readonly UIView View;
        readonly UIPanGestureRecognizer ScrollViewPanGesture;
        readonly ScrollView ContainerScrollView;

        public GestureRecognizerDelegate(UIView view = null, ScrollView containerScrollView = null, UIPanGestureRecognizer scrollViewPanGesture = null)
        {
            View = view;
            ContainerScrollView = containerScrollView;
            ScrollViewPanGesture = scrollViewPanGesture;
        }

        public override bool ShouldRecognizeSimultaneously(UIGestureRecognizer gestureRecognizer, UIGestureRecognizer otherGestureRecognizer)
        {
            return true;
        }

        public override bool ShouldBegin(UIGestureRecognizer recognizer)
        {
            if (recognizer as UIPanGestureRecognizer is null) return true;
            if (ContainerScrollView?.EnableScrolling != true) return true;

            var panGestureRecognizer = (UIPanGestureRecognizer)recognizer;
            var velocity = panGestureRecognizer.VelocityInView(View);

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