namespace Zebble
{
	using System;
	using System.Linq;
	using System.Threading.Tasks;
	using Services;
	using Olive;

	public static class AnimationExtensions
	{
		public static Animation StartAnimation<T>(this T @this, Action<Animation> config) where T : View
		{
			return @this.StartAnimation(null, config);
		}

		public static Animation StartAnimation<T>(this T @this, Action<T> change, Action<Animation> config = null) where T : View
		{
			return @this.StartAnimation(Animation.DefaultDuration, change, config);
		}

		public static Animation StartAnimation<T>(this T @this, TimeSpan duration, Action<T> change, Action<Animation> config = null) where T : View
		{
			var result = new Animation { Change = () => change?.Invoke(@this), Duration = duration };
			config?.Invoke(result);
			@this.Animate(result).GetAwaiter();
			return result;
		}

		public static Animation StartAnimation<T>(this T @this, Animation animation) where T : View
		{
			@this.Animate(animation).GetAwaiter();
			return animation;
		}

		public static async Task Animate<T>(this T @this, Animation animation) where T : View
		{
			if (@this is null) return;

			IdleUITasks.ReportAction(animation.Duration);
			await @this.WhenShown(() => animation.Start(@this));
		}

		public static async Task Animate<T>(this T @this, params Animation[] steps) where T : View
		{
			if (@this is null || steps.None()) return;

			if (steps.Any(x => x.IsStarted))
				throw new Exception("Animations provided to a multi-step animation should not be started already.");

			var nextSteps = steps.ExceptFirst().ToArray();

			if (nextSteps.Any())
				steps[0].OnCompleted(() => @this.Animate(nextSteps));

			await @this.Animate(steps[0]);
		}

		public static Task Animate<T>(this T @this, TimeSpan duration, Action<T> change) where T : View
			=> @this.Animate(Animation.Create(@this, duration, change));

		public static Task Animate<T>(this T @this, AnimationEasing easing, Action<T> change) where T : View
			=> @this.Animate(Animation.Create(@this, easing, change));

		public static Task Animate<T>(this T @this, TimeSpan duration, AnimationEasing easing, Action<T> change) where T : View
		{
			return @this.Animate(new Animation
			{
				Easing = easing,
				Duration = duration,
				Change = () => change(@this)
			});
		}

		public static Task Animate<T>(this T @this, TimeSpan duration, Action<T> initialState, Action<T> change) where T : View
		{
			if (@this is null) return Task.CompletedTask;

			initialState?.Invoke(@this);
			return @this.Animate(Animation.Create(@this, duration, change));
		}

		public static Task Animate<T>(this T @this, TimeSpan duration, AnimationEasing easing, Action<T> initialState, Action<T> change) where T : View
		{
			if (@this is null) return Task.CompletedTask;

			initialState?.Invoke(@this);
			return @this.Animate(Animation.Create(@this, duration, easing, change));
		}

		public static Task Animate<T>(this T @this, Action<T> change) where T : View
			=> @this.Animate(Animation.Create(@this, change));

		public static Task Animate<T>(this T @this, CssRule rule) where T : View
			=> @this.Animate(x => rule.Apply(x));

		public static Task Animate<T>(this T @this, TimeSpan duration, CssRule rule) where T : View
			=> @this.Animate(duration, x => rule.Apply(x));

		public static Task Animate<T>(this T @this, AnimationEasing easing, CssRule rule) where T : View
			=> @this.Animate(easing, x => rule.Apply(x));

		public static Task Animate<T>(this T @this, TimeSpan duration, AnimationEasing easing, CssRule rule) where T : View
			=> @this.Animate(duration, easing, x => rule.Apply(x));

		public static Animation Duration(this Animation ani, TimeSpan value) => ani.Set(x => x.Duration = value);

		public static Animation Delay(this Animation ani, TimeSpan value) => ani.Set(x => x.Delay = value);

		public static Animation Easing(this Animation ani, AnimationEasing easing) => ani.Set(x => x.Easing = easing);

		public static Animation Easing(this Animation ani, AnimationEasing easing, EasingFactor factor)
			=> ani.Easing(easing).Set(x => x.EasingFactor = factor);

		public static Animation Change(this Animation ani, Action change) => ani.Set(x => x.Change = change);
	}
}