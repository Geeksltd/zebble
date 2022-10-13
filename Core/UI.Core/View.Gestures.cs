namespace Zebble
{
    using System;
    using System.Threading.Tasks;
    using Services;
    using Olive;

    partial class View
    {
        bool ShouldRaiseGesturesOnUIThread;

        /// <summary>
        /// Raised as soon as the user touches this view.
        /// </summary>
        public readonly AsyncEvent<TouchEventArgs> Touched = new(ConcurrentEventRaisePolicy.Ignore);

        /// <summary>
        /// Raised after the user lifts the touch, if it didn't move more than 6px and didn't take more than 200ms.
        /// </summary>
        public readonly AsyncEvent<TouchEventArgs> Tapped = new(ConcurrentEventRaisePolicy.Ignore);

        /// <summary>
        /// Raised when the user holds this view for one second or longer, without moving more than 6px.
        /// </summary>
        public readonly AsyncEvent<TouchEventArgs> LongPressed = new();

        /// <summary>
        /// Raised when the touch quickly pans to one side for at least 10px and a maximum of 500ms.
        /// Avoid this in most scenarios. Use a combination of Panning and PanFinished instead.
        /// </summary>
        public readonly AsyncEvent<SwipedEventArgs> Swiped = new(ConcurrentEventRaisePolicy.Queue);

        /// <summary>
        /// Raised for every tiny movement (usually around a pixel) of the touch while panning.
        /// </summary>
        public readonly AsyncEvent<PannedEventArgs> Panning = new(ConcurrentEventRaisePolicy.Queue);

        /// <summary>
        /// Raised when after a whole process of panning, the touch device (e.g. mouse or finger) is released.
        /// </summary>
        public readonly AsyncEvent<PannedEventArgs> PanFinished = new();

        public readonly AsyncEvent<PinchedEventArgs> Pinching = new(ConcurrentEventRaisePolicy.Queue);

        public readonly AsyncEvent<UserRotatingEventArgs> UserRotating = new(ConcurrentEventRaisePolicy.Queue);

        /// <summary>Will raise the tapped event with the location of 0,0.</summary>
        public virtual void RaiseTapped() => RaiseTapped(new TouchEventArgs(this, new Point(), 1));

        internal bool CannotHandle(IAsyncEvent @event) => !(enabled && @event.IsHandled());

        public virtual void RaiseTapped(TouchEventArgs arg)
        {
            if (CannotHandle(Tapped))
                parent?.RaiseTapped(new TouchEventArgs(parent, arg.Point.OnParentOf(this), arg.Touches));
            else
                RaiseGestureEvent(() => Tapped.Raise(arg));
        }

        public virtual void RaiseTouched(TouchEventArgs arg)
        {
            if (CannotHandle(Touched))
                parent?.RaiseTouched(new TouchEventArgs(parent, arg.Point.OnParentOf(this), arg.Touches));
            else
                RaiseGestureEvent(() => Touched.Raise(arg));
        }

        public virtual void RaiseLongPressed(TouchEventArgs args)
        {
            if (CannotHandle(LongPressed))
                parent?.RaiseLongPressed(new TouchEventArgs(parent, args.Point.OnParentOf(this), args.Touches));
            else
                RaiseGestureEvent(() => LongPressed.Raise(args));
        }

        public virtual void RaiseSwipped(SwipedEventArgs args)
        {
            if (CannotHandle(Swiped))
                parent?.RaiseSwipped(args);
            else
                RaiseGestureEvent(() => Swiped.Raise(args));
        }

        public virtual void RaisePanning(PannedEventArgs arg)
        {
            if (CannotHandle(Panning))
            {
                arg = new PannedEventArgs(parent, arg.From.OnParentOf(this), arg.To.OnParentOf(this),
                    arg.Velocity, arg.Touches);
                parent?.RaisePanning(arg);
            }
            else
            {
                RaiseGestureEvent(() => Panning.Raise(arg));
            }
        }

        public virtual void RaisePanFinished(PannedEventArgs arg)
        {
            if (CannotHandle(PanFinished))
            {
                arg = new PannedEventArgs(parent, arg.From.OnParentOf(this), arg.To.OnParentOf(this),
                    arg.Velocity, arg.Touches);
                parent?.RaisePanFinished(arg);
            }
            else
                RaiseGestureEvent(() => PanFinished.Raise(arg));
        }

        public virtual void RaisePinching(PinchedEventArgs arg)
        {
            if (CannotHandle(Pinching))
                parent?.RaisePinching(new PinchedEventArgs(parent,
                    arg.Touch1.OnParentOf(this),
                    arg.Touch2.OnParentOf(this),
                    arg.ChangeScale));
            else
                RaiseGestureEvent(() => Pinching.Raise(arg));
        }

        public virtual void RaiseUserRotating(UserRotatingEventArgs arg)
        {
            if (CannotHandle(UserRotating))
                parent?.RaiseUserRotating(new UserRotatingEventArgs(parent,
                       arg.Touch1.OnParentOf(this),
                       arg.Touch2.OnParentOf(this),
                       arg.Degrees));
            else
                RaiseGestureEvent(() => UserRotating.Raise(arg));
        }

        public void RaiseGesturesOnUIThread(bool value = true) => ShouldRaiseGesturesOnUIThread = value;

        void RaiseGestureEvent(Func<Task> invoker)
        {
            if (IsDisposing || Nav.IsNavigating) return;
            IdleUITasks.ReportGesture();

            if (ShouldRaiseGesturesOnUIThread) DoRun();
            else Thread.Pool.RunAction(DoRun);

            async void DoRun()
            {
                try
                {
                    await invoker();
                }
                catch (Exception ex)
                {
                    if (UIRuntime.IsDebuggerAttached) throw;
                    else
                    {
                        await Alert.Show("Error",
                            $"Unfortunately there is an unexpected error.{Environment.NewLine}" +
                            $"You may contact our support team with the following technical info to help rectify this problem:{Environment.NewLine}{ex.ToFullMessage()}");
                    }
                }
            }
        }

        protected virtual void BlockGestures(bool shouldBlock = true)
        {
            if (!shouldBlock) return;

            Tapped.Handle(() => { });
            Swiped.Handle(() => { });
            LongPressed.Handle(() => { });
            Panning.Handle(() => { });
        }
    }
}