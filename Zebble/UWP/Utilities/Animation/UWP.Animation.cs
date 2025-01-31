namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Olive;
    using Windows.UI.Xaml.Media.Animation;
    using xaml = Windows.UI.Xaml;

    partial class Animation
    {
        protected readonly static ConcurrentList<Animation> RunningAnimations = new();

        internal Storyboard Storyboard;
        internal xaml.FrameworkElement Target;
        internal readonly List<ZebblePropertyAnimator> Timelines = new();

        internal void AddTimeline<TValue>(Func<TValue> fromProvider, Func<TValue> toProvider, xaml.FrameworkElement target, xaml.DependencyObject animatable, string propertyPath)
        {
            Target = target;
            var from = fromProvider();
            var to = toProvider();

            if (from.ToString() == to.ToString()) return;

            Timeline result = null;

            if (typeof(TValue) == typeof(double))
            {
                if (double.IsNaN((double)(object)from)) from = (TValue)(object)0.0;
                if (double.IsNaN((double)(object)to)) to = (TValue)(object)0.0;

                result = new DoubleAnimation
                {
                    BeginTime = Delay,
                    From = (double)(object)from,
                    To = (double)(object)to,
                    EasingFunction = this.RenderEasing(),
                    EnableDependentAnimation = true
                };
            }
            else if (typeof(TValue) == typeof(Color))
            {
                result = new ColorAnimation
                {
                    BeginTime = Delay,
                    From = (from as Color).Render(),
                    To = (to as Color).Render(),
                    EnableDependentAnimation = true
                };
            }

            if (Repeats != 0)
                result.RepeatBehavior = Repeats < 0 ? RepeatBehavior.Forever : new RepeatBehavior(Repeats);

            result.Duration = Duration;

            var timeline = new ZebblePropertyAnimator<TValue>
            {
                Timeline = result,
                PropertyPath = propertyPath,
                Target = animatable,
                ToProvider = toProvider,
                FromProvider = fromProvider,
                From = from,
                To = to,
            };

            // Prevent duplicates
            lock (Timelines)
            {
                if (Storyboard != null)
                    Log.For(this).Error("Animation is already started when adding a timeline!");

                Timelines.RemoveWhere(x => (x as ZebblePropertyAnimator<TValue>)?.PropertyPath == propertyPath);
                Timelines.Insert(0, timeline);
            }
        }

        internal void Apply(View target)
        {
            lock (Timelines)
            {
                if (Timelines.None())
                {
                    OnNativeStarted();
                    OnNativeCompleted(empty: true);
                    return;
                }

                lock (RunningAnimations)
                {
                    RunningAnimations.RemoveWhere(v => v.IsCompleted);
                    RunningAnimations.Add(this);
                }

                Storyboard = new Storyboard();
                lock (Timelines)
                {
                    Storyboard.Children.AddRange(Timelines.Select(x => x.Timeline));
                    Timelines.Clear();
                }

                Storyboard.Begin();
                OnNativeStarted();

                Storyboard.Completed += (s, ev) =>
                {
                    Storyboard = null;
                    OnNativeCompleted();
                };
            }
        }

        internal static Animation GetCurrent(xaml.FrameworkElement target, string propertyPath)
        {
            lock (RunningAnimations)
                return RunningAnimations.FirstOrDefault(x => x.Target == target && x.Timelines.Any(t => t.PropertyPath == propertyPath));
        }

        internal static TValue GetCurrentlyRunningAnimationValue<TValue>(xaml.FrameworkElement target, string propertyPath)
        {
            var ani = GetCurrent(target, propertyPath);
            if (ani is null) return default;

            lock (ani.Timelines)
            {
                if (ani.Timelines.FirstOrDefault(t => t.PropertyPath == propertyPath) is ZebblePropertyAnimator<TValue> timeline)
                    return timeline.ToProvider();
            }

            return default;
        }

        internal static Task StopCurrentAnimation(xaml.FrameworkElement target, string propertyPath)
        {
            var alreadyRunning = GetCurrent(target, propertyPath);

            if (alreadyRunning != null)
            {
                alreadyRunning.Storyboard.SkipToFill();
                alreadyRunning.Storyboard.Stop();
            }

            return Task.CompletedTask;
        }
    }
}