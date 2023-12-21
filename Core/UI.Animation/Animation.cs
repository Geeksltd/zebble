namespace Zebble
{
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Services;
    using Olive;

    public partial class Animation
    {
        internal static bool SkipAll;

        Animation ExistingAnimationOnView;

        public static TimeSpan DefaultDuration = 350.Milliseconds();
        public static TimeSpan DefaultListItemSlideDuration = 300.Milliseconds();
        public static TimeSpan DefaultSwitchDuration = 150.Milliseconds();
        public static TimeSpan FadeDuration = 150.Milliseconds();
        public static TimeSpan DropDuration = 300.Milliseconds();
        public static TimeSpan FlashDuration = 200.Milliseconds();
        public static TimeSpan OneFrame = (1000 / 60 /*FPS*/).Milliseconds();

        public readonly AsyncLock StartedLock = new();
        readonly TaskCompletionSource<bool> TaskSource = new();

        public TimeSpan Duration { get; set; } = DefaultDuration;
        public TimeSpan Delay { get; set; } = TimeSpan.Zero;
        public Action Change { get; set; }
        public AnimationEasing Easing { get; set; } = AnimationEasing.EaseOut;

        [Obsolete("Instead of setting this directly, use the Easing(...) extension method.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public EasingFactor EasingFactor { get; set; } = EasingFactor.Quadratic;

        /// <summary>Specifies the number of bounces (used only for EaseInBounceOut).</summary>
        public int Bounces { get; set; } = 1;

        /// <summary>
        /// Specifies how bouncy the bounce animation is.
        /// Low values of this property result in bounces with little loss of height between bounces (more bouncy).
        /// High values result in dampened bounces(less bouncy).
        /// </summary>
        public int Bounciness { get; set; } = 2;

        /// <summary>If set to -1, it will repeat forever.</summary>
        public int Repeats { get; set; } = 1;

        public Task Task => TaskSource.Task;

        public bool IsStarted, IsCompleted;
        readonly AsyncEvent NativeStarted = new();
        readonly TaskCompletionSource<bool> NativeCompleted = new();
        public readonly AsyncEvent completed = new();

        [Obsolete("Use OnCompleted() instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public readonly AsyncEvent Completed;

        public Animation() => Completed = completed;

        internal async void OnNativeStart(Action handler)
        {
            if (handler == null) return;
            if (IsStarted)
            {
                handler.Invoke();
                return;
            }

            var isStarted = false;
            using (await StartedLock.Lock())
            {
                if (IsStarted) isStarted = true;
                else NativeStarted.Event += handler;
            }

            if (isStarted) handler.Invoke();
        }

        /// <summary>
        /// Stops this running animation.
        /// </summary>
        public async Task Cancel(View target)
        {
            if (IsCompleted) return;
            else IsCompleted = true;

            using (await target.DomLock.Lock())
                RemoveFromView(target);

            TaskSource?.TrySetResult(result: true);
            completed.SignalRaiseOn(Thread.Pool);
            NativeStarted?.Dispose();
            completed?.ClearHandlers();
        }

        void RemoveFromView(View target)
        {
            var existingAnimation = target.AnimationContext;
            if (existingAnimation is null) return;

            if (existingAnimation.ExistingAnimationOnView == this)
            {
                // Sub animation still running.
                return;
            }

            if (ExistingAnimationOnView?.IsCompleted == false)
            {
                // My parent animation is still running.
                target.AnimationContext = ExistingAnimationOnView;
                return;
            }

            target.AnimationContext = null;
        }

        public Animation OnCompleted(Action handler) => OnCompleted(() => { handler(); return Task.CompletedTask; });

        public Animation OnCompleted(Func<Task> handler)
        {
            if (IsCompleted) UIWorkBatch.Run(handler).GetAwaiter();
            else completed.Handle(() => UIWorkBatch.Run(handler));
            return this;
        }

        internal async Task Start(View target)
        {
            if (IsStarted) throw new InvalidOperationException();
            if (target.IsDisposing) return;

            IdleUITasks.SetBusyFor(Duration);

            ExistingAnimationOnView = target.AnimationContext;

            target.AnimationContext = this;

            using (await StartedLock.Lock())
            {
                Thread.UI.RunAction(() =>
                {
                    UIWorkBatch.Current?.Flush();
                    UIWorkBatch.RunSync(Change);
                    UIWorkBatch.Current?.Flush();

                    target.AnimationContext = null;
                    Apply(target);
                });

                IdleUITasks.SetBusyFor(Duration);
            }

            if (Repeats < 0) return;

            await NativeCompleted.Task.ContinueWith(async t =>
            {
                if (t.IsCompleted) await Cancel(target);
                else throw t.Exception;
            });
        }

        void OnNativeStarted()
        {
            IsStarted = true;
            NativeStarted.SignalRaiseOn(Thread.Pool);
        }

        void OnNativeCompleted(bool empty = false) => NativeCompleted?.TrySetResult(!empty);

        /// <summary>
        /// If it repeats forever, returns Zero. Otherwise returns the duration multiplied by repeats.
        /// </summary>
        public TimeSpan GetTimeToCompletion() => (Repeats <= 0) ? TimeSpan.Zero : Duration.Multiply(Repeats);
    }
}