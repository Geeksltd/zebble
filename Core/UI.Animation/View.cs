namespace Zebble
{
	using System;
	using System.Threading.Tasks;
	using Zebble.Services;

	partial class View
	{
		public Animation AnimationContext;

		public bool IsShown { get; internal set; }

		public Task<Animation> AddWithAnimation<T>(T child, Action<T> change) where T : View
			=> AddWithAnimation(child, null, change);

		public Task<Animation> AddWithAnimation<T>(T child, Action<T> initialState, Action<T> change) where T : View
			=> AddWithAnimation(child, initialState, Animation.Create(child, change));

		public async Task<Animation> AddWithAnimation<T>(T child, TimeSpan duration, Action<T> initialState, Action<T> change)
			where T : View
		{
			var result = Animation.Create(child, duration, change);
			await AddWithAnimation(child, initialState, result);
			return result;
		}

		public Task<Animation> AddWithAnimation<T>(T child, Animation animation) where T : View
			=> AddWithAnimation(child, null, animation);

		public async Task<Animation> AddWithAnimation<T>(T child, Action<T> initialState, Animation animation) where T : View
		{
			if (initialState != null)
				UIWorkBatch.RunSync(() => initialState(child));

			await Add(child, awaitNative: true);

			await child.WhenShown(() => child.StartAnimation(animation));

			return animation;
		}

		public Task<Animation> AddWithAnimation<T>(T child, CssRule rule) where T : View
			=> AddWithAnimation(child, null, rule);

		public Task<Animation> AddWithAnimation<T>(T child, CssRule initialRule, CssRule rule) where T : View
			=> AddWithAnimation(child, x => initialRule?.Apply(x), x => rule.Apply(x));

		public Task<Animation> AddWithAnimation<T>(T child, TimeSpan duration, CssRule rule) where T : View
			=> AddWithAnimation(child, duration, null, rule);

		public Task<Animation> AddWithAnimation<T>(T child, TimeSpan duration, CssRule initialRule, CssRule rule) where T : View
			=> AddWithAnimation(child, duration, x => initialRule?.Apply(x), x => rule.Apply(x));

		public Task<Animation> AddWithFadeIn<T>(T child) where T : View
		{
			float targetOpacity = 1;

			child.Rendered.Handle(() =>
			{
				targetOpacity = child.Opacity;
				child.Opacity = 0;
			});

			return AddWithAnimation(child, new Animation
			{
				Easing = AnimationEasing.Linear,
				Change = () => child.Style.Opacity(targetOpacity),
				Duration = Animation.FadeDuration
			});
		}

		public Animation GetRunningAnimation() => AnimationContext;
	}
}