namespace Zebble
{
    using System.Threading.Tasks;

    public abstract class Dialog : Stack
    {
        public readonly TextView Title = new TextView().Id("Title");
        internal protected View Content { get; private set; }
        public readonly Stack ButtonsRow = new Stack(RepeatDirection.Horizontal) { Id = "ButtonsRow" };
        public readonly AsyncEvent Displayed = new AsyncEvent();

        public bool ScrollContent { get; set; } = true;
        public bool ButtonsAtTop { get; set; }

        public override async Task OnInitializing()
        {
            await base.OnInitializing();

            await Add(Title);

            if (ScrollContent) await Add(Content = new ScrollView());
            else await Add(Content = new Canvas().Id("DialogContent"));

            if (ButtonsAtTop) await AddAt(0, ButtonsRow);
            else await Add(ButtonsRow);
        }
    }
}