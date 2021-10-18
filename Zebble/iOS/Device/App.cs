namespace Zebble.Device
{
    using System.Threading.Tasks;

    partial class App
    {
        static Task DoStop()
        {
            Foundation.NSThread.Exit();
            return Task.CompletedTask;
        }
    }
}