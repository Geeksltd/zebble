using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zebble.Services
{
    public class PeriodicTask
    {
        static async Task Run(Action action, TimeSpan period, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(period, cancellationToken);
                if (!cancellationToken.IsCancellationRequested) action();
            }
        }

        public static Task Run(Action action, TimeSpan period) => Run(action, period, CancellationToken.None);
    }
}