namespace Zebble
{
    public class NavigationEventArgs
    {
        public Page From { get; }
        public Page To { get; }

        public NavigationEventArgs(Page from, Page to)
        {
            From = from;
            To = to;
        }
    }
}