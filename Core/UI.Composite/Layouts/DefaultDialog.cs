namespace Zebble
{
    using System.Threading.Tasks;

    abstract class DefaultDialog : Stack
    {
        public readonly TextView Title = new();
        internal protected View Content { get; private set; }
        public readonly Stack ButtonsRow = new(RepeatDirection.Horizontal);
        public readonly AsyncEvent Displayed = new();

        public bool ScrollContent { get; set; } = true;

        public override async Task OnInitializing()
        {
            await base.OnInitializing();

            await Add(Title);

            if (ScrollContent) await Add(Content = new ScrollView());
            else await Add(Content = new Canvas());

            await Add(ButtonsRow);
        }
    }
}