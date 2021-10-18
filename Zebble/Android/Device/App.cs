namespace Zebble.Device
{
    using System.Threading.Tasks;

    partial class App
    {
        static Task DoStop()
        {
            UIRuntime.CurrentActivity.Finish();
            return Task.CompletedTask;
        }
    }
}