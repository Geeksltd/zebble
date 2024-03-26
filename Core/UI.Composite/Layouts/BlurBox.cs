namespace Zebble
{
    public class BlurBox : Stack
    {
        public readonly AsyncEvent BlurredChanged = new();

        bool blurred = true;
        public bool Blurred
        {
            get => blurred;
            set
            {
                if (blurred == value) return;
                blurred = value;
                BlurredChanged.Raise();
            }
        }
    }
}