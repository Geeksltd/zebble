using System;
using System.Linq;
using Zebble.Mvvm.AutoUI;
using Olive;

namespace Zebble.Mvvm
{
    internal partial class VmExecutionContext
    {
        ViewModelNode Root;

        public string Title => ViewModel.ActiveScreen.GetType().Name.ToLiteralFromPascalCase();

        public VmExecutionContext(ViewModel target)
        {
            Root = new ViewModelNode("", target, null);

            if (ViewModel.Stack.Any())
                Root.Children.Add(new InvokeNode("Back", () => ViewModel.Back(), Root));

            Root.Children.Do(x => x.Parent = null);

            Root.WithAllChildren().OfType<InvokeNode>().ToList().Do((n, i) => n.Index = i + 1);
        }

        internal ViewModelNode Generate() => Root;
    }
}