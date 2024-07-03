namespace Zebble.Services
{
    using System;
    using System.Threading.Tasks;
    using Zebble;

    /// <summary>
    /// Runs a specified action regularly in another thread at exactly the specified intervals.
    /// </summary>
    public class Timer
    {
        Action Action;
        readonly Func<Task> AsyncAction;
        readonly TimeSpan Interval;
        bool Stopped = true;
        readonly WaitingOption Option;
        public OnError ErrorAction { get; set; } = OnError.Throw;

        public enum WaitingOption
        {
            /// <summary>
            /// Run the action on a new thread exactly at the specified intervals, irrespective of how long it takes.
            /// </summary>
            Parallel,

            /// <summary>
            /// Run the action and wait for its completion, then wait for the interval duration.
            /// </summary>
            WaitForCompletion
        }

        public Timer(Action action, TimeSpan interval, WaitingOption option)
        {
            Action = action;
            Interval = interval;
            Option = option;
            Thread.Pool.RunOnNewThread(StartTicking);
        }

        public Timer(Func<Task> asyncAction, TimeSpan interval, WaitingOption option)
        {
            AsyncAction = asyncAction;
            Interval = interval;
            Option = option;
            Thread.Pool.RunOnNewThread(StartTicking);
        }

        public Timer Start() => this.Set(x => x.Stopped = false);

        public void Stop() => Stopped = true;

        public void Dispose()
        {
            Stopped = true;
            Action = null;
			GC.SuppressFinalize(this);
        }

        async Task StartTicking()
        {
            while (!Stopped)
            {
                switch (Option)
                {
                    case WaitingOption.Parallel:
                        if (Action != null) Thread.Pool.RunActionOnNewThread(Action);
                        if (AsyncAction != null) Thread.Pool.RunOnNewThread(AsyncAction);
                        break;
                    case WaitingOption.WaitForCompletion:
                        try
                        {
                            Action?.Invoke();
                            if (AsyncAction != null) await AsyncAction();
                        }
                        catch (Exception ex)
                        {
                            await ErrorAction.Apply(ex);
                        }

                        break;

                    default: throw new NotSupportedException();
                }

                if (!Stopped) await Task.Delay(Interval);
            }
        }
    }
}