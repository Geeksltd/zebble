namespace Zebble
{
    public class HardwareBackEventArgs
    {
        public Page From { get; }

        public bool Cancel { get; set; }

        public HardwareBackEventArgs(Page from) => From = from;
    }
}