namespace Zebble
{
    partial class Thread
    {
        public static readonly UIThread UI = new();

        public static BaseThread Current() => UI.IsRunning() ? UI : (BaseThread)Pool;
    }
}