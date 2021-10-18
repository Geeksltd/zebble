namespace Zebble
{
    public partial class View
    {
        public bool IsDisposing { get; private set; }
    }

    public partial class Canvas : View { }

    public partial class Page : Canvas { }
}