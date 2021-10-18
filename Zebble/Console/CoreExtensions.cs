using System;
using System.Threading.Tasks;
using Olive;

namespace Zebble
{
    public static class CoreExtensions
    {
        public static Task Apply(this OnError strategy, string error) => Apply(strategy, new Exception(error));

        public static Task Apply(this OnError strategy, Exception error, string friendlyMessage = null)
        {
            if (error is null) return Task.CompletedTask;

            Log.For(typeof(CoreExtensions)).Error(error, friendlyMessage.WithSuffix(": " + error));

            switch (strategy)
            {
                case OnError.Ignore: break;
                case OnError.Throw: throw error;
                case OnError.Toast:
                    Mvvm.DialogViewModel.Current.Toast(friendlyMessage.Or(error.Message));
                    break;
                case OnError.Alert:
                    Mvvm.DialogViewModel.Current.Alert(friendlyMessage.Or(error.Message));
                    break;
                default: throw new NotSupportedException(strategy + " is not implemented.");
            }

            return Task.CompletedTask;
        }
    }
}
