using System;
using System.Linq;
using System.Threading.Tasks;
using Zebble.Mvvm.AutoUI;
using Olive;

namespace Zebble.Mvvm
{
    class AutoPage : Page
    {
        static readonly Color WhiteSmoke = "#f3f3f3";
        readonly ViewModel Target;
        readonly Stack Body = new Stack().Id("Body").Set(x => x.ClipChildren = false);
        readonly TextView Header = Text("").Padding(10, top: 35).Background("#333").TextColor(Colors.White).Font(size: 20);

        public AutoPage() : this(ViewModel.ActiveScreen) { }

        public AutoPage(ViewModel target)
        {
            Target = target;
            Header.Text = target.GetType().GetProgrammingName(useGlobal: false, useNamespace: false, useNamespaceForParams: false).ToLiteralFromPascalCase();
            Height.BindTo(Root.Height);
        }

        static TextView Text(string text) => new TextView(text).Padding(3).TextAlignment(Alignment.Middle);

        public override async Task OnInitializing()
        {
            await base.OnInitializing();
            var stack = await Add(new Stack().Height(100.Percent()));
            stack.ClipChildren = false;
            await stack.Add(Header);
            var scroller = await stack.Add(new ScrollView().Height(100.Percent()));
            await scroller.Add(Body);

            var tree = new VmExecutionContext(Target).Generate();
            await AddElements(Body, tree);
        }

        async Task<Stack> Render(ValueNode node)
        {
            var stack = new Stack(RepeatDirection.Horizontal).Padding(5).Background(WhiteSmoke).Height(35);
            stack.ClipChildren = false;
            await stack.Add(Text(node.Label.OrEmpty().ToLiteralFromPascalCase() + ":").TextColor("#aaa").TextAlignment(Alignment.Right));
            await stack.Add(Text(node.ValueString.Or("---")).TextAlignment(Alignment.Left));

            // await Body.Add(stack);
            return stack;
        }

        Button Render(InvokeNode node)
        {
            return new Button().Text(node.NaturalLabel.TrimStart("Tap ")).Background(Colors.LightGray)
                .Margin(10).On(x => x.Tapped, () => node.Execute());
        }

        async Task AddElements(Stack stack, ViewModelNode node)
        {
            foreach (var item in node.Children.OfType<ValueNode>())
                await stack.Add(await Render(item));

            foreach (var item in node.Children.OfType<InvokeNode>())
                await stack.Add(Render(item));

            foreach (var item in node.Children.OfType<ViewModelNode>())
                await Render(stack, item);
        }

        async Task<Stack> Render(Stack container, ViewModelNode node)
        {
            var stack = await container.Add(new Stack().Background(WhiteSmoke).Margin(top: 10, bottom: 5).Padding(5));
            stack.ClipChildren = false;

            if (!node.Label.StartsWith("Items["))
                await stack.Add(Text(node.Label).Font(bold: true));

            await AddElements(stack, node);
            return stack;
        }
    }
}