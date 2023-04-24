namespace Zebble.Services
{
    using System;
    using System.Collections.Concurrent;
    using Olive;

    public class IdleUITasks
    {
        static readonly TimeSpan IDLE_TIME = 300.Milliseconds();
        static readonly TimeSpan INTERVALS = 300.Milliseconds();
        static readonly ConcurrentQueue<Tuple<string, Action>> OutstandingActions = new();
        static DateTime LatestUIAction = DateTime.UtcNow;
        static readonly Timer ActionTimer;

        public static DateTime LatestTouch { get; private set; }

        static IdleUITasks()
        {
            ActionTimer = new Timer((Action)RunIdleTasks, INTERVALS, Timer.WaitingOption.WaitForCompletion).Start();
        }

        internal static void SetBusyFor(TimeSpan period)
        {
            if (period > 1.Seconds()) return; // Long running animations don't count
            LatestUIAction = LatestUIAction.Max(DateTime.UtcNow.Add(period));
        }

        internal static void ReportAction(TimeSpan duration) => ReportAction(DateTime.UtcNow.Add(duration));

        internal static void ReportAction() => ReportAction(DateTime.UtcNow);

        internal static void ReportGesture()
        {
            LatestTouch = DateTime.UtcNow;
            ReportAction();
        }

        internal static void ReportAction(DateTime utcTime)
        {
            if (LatestUIAction < utcTime) LatestUIAction = utcTime;
        }

        static void RunIdleTasks()
        {
            while (SeemsIdle())
                if (!RunOneAction()) break;
        }

        /// <summary>
        /// Determines if it's been 300ms since the last gesture action or the last rendering.
        /// </summary>
        public static bool SeemsIdle() => LatestUIAction < DateTime.UtcNow.Subtract(IDLE_TIME);

        static bool RunOneAction()
        {
            if (!OutstandingActions.TryDequeue(out var action)) return false;

            try { action.Item2(); }
            catch (Exception ex) { Log.For<IdleUITasks>().Error(ex, $"Idle action '{action.Item1}' failed."); }

            return true;
        }

        /// <summary>
        ///  Schedules an action to run when the UI thread appears free.
        /// </summary>
        public static void Run(string description, Action idleAction)
        {
            if (idleAction is null) return;
            OutstandingActions.Enqueue(Tuple.Create(description, idleAction));
        }
    }
}