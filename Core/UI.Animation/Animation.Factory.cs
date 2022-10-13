namespace Zebble
{
    using System;
    using System.ComponentModel;

    partial class Animation
    {
        public static Animation Create<TView>(TView view, Action<TView> change)
          => Create(view, DefaultDuration, change);

        public static Animation Create<TView>(TView view, TimeSpan duration, Action<TView> change)
            => new()
            { Duration = duration, Change = () => change(view) };

        [Obsolete("Use the other overload to provide the change as the last argument.", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Animation Create<TView>(TView view, Action<TView> change, TimeSpan duration)
            => new()
            { Duration = duration, Change = () => change(view) };

        [Obsolete("Use the other overload to provide the change as the last argument.", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Animation Create<TView>(TView view, Action<TView> change, TimeSpan duration, AnimationEasing easing)
            => Create(view, duration, easing, change);

        [Obsolete("Use the other overload to provide the change as the last argument.", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Animation Create<TView>(TView view, Action<TView> change, AnimationEasing easing)
            => Create(view, easing, change);

        public static Animation Create<TView>(TView view, TimeSpan duration, AnimationEasing easing, Action<TView> change)
            => new()
            { Duration = duration, Change = () => change(view), Easing = easing };

        public static Animation Create<TView>(TView view, AnimationEasing easing, Action<TView> change)
            => new()
            { Easing = easing, Change = () => change(view) };
    }
}