namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Olive;
    using Zebble.Services;

    public class TreeView : Stack
    {
        public float IndentSpace = 20;
        public readonly ConcurrentList<Node> RootNodes = new();

        public IEnumerable<Node> AllNodes => RootNodes.SelectMany(r => r.WithAllChildren().Cast<Node>());

        public async Task<TNode> AddNode<TNode>(TNode node) where TNode : Node
        {
            if (node is null) return null;

            if (node.Parent != null) throw new Exception("Root tree nodes should not have a parent.");

            RootNodes.Add(node);
            await node.GenerateView(this);

            return node;
        }

        public override void Dispose()
        {
            lock (RootNodes)
            {
                foreach (var n in RootNodes.ToArray())
                {
                    n?.Dispose();
                    RootNodes.Remove(n);
                }
            }

            base.Dispose();
        }

        public class Node : IHierarchy, IDisposable
        {
            Stack Row;
            bool isExpanded;
            public readonly ConcurrentList<Node> children = new();
            public Func<Node, View> ViewGenerator = n => new TextView { AutoFlash = false }.CssClass("tree-node").Text(n.Text).Wrap(value: false);
            public readonly AsyncEvent<TouchEventArgs> Tapped = new();
            public object Source { get; set; }

            public TextView ToggleIcon = new TextView().Hide().Text("▶").CssClass("toggle-icon");

            public Node() => ToggleIcon.On(x => x.Tapped, Toggle);

            /// <summary>Will create new node for this item and its descendents hierarchy.</summary> 
            public Node(IHierarchy source) : this()
            {
                Text = source.ToStringOrEmpty();
                Source = source;
                foreach (var child in source.GetChildren()) Add(new Node(child));
            }
            public Node(string text) : this() => Text = text;
            public Node(object source) : this()
            {
                Source = source;
                Source = source;
                Text = source.ToStringOrEmpty();

                if (source is IHierarchy hi) foreach (var child in hi.GetChildren()) Add(new Node(child));
                else if (source is View view) foreach (var child in view.AllChildren) Add(new Node(child));
            }
            public Node(string text, object source) : this() { Source = source; Text = text; }

            public Node Parent { get; private set; }
            public string Text { get; set; }
            public View View { get; internal set; }

            public bool IsExpanded => isExpanded;

            public async Task SetIsExpanded(bool value)
            {
                if (value == isExpanded) return;
                if (View != null) if (isExpanded) await Collapse();
                    else await Expand();
                isExpanded = value;
            }

            internal Stack ChildrenContainer;

            public int Depth => this.GetAllParents().Count();

            public IEnumerable<Node> Children => children;

            public void Remove(Node child)
            {
                child.Parent = null;
                child.Remove(child);
                if (children.None()) ToggleIcon.Hide();
            }

            public TNode Add<TNode>(TNode child) where TNode : Node
            {
                child.Parent = this;
                children.Add(child);
                ToggleIcon.Visible();

                return child;
            }

            public async Task Expand()
            {
                if (children.None()) return;
                if (Row is null) return;

                await UIWorkBatch.Run(async () =>
                 {
                     if (ChildrenContainer is null)
                     {
                         await Row.AddAfter(ChildrenContainer = new Stack());

                         // Render children:
                         foreach (var c in Children) await c.GenerateView(ChildrenContainer);
                     }
                     else await ChildrenContainer.IgnoredAsync(false);
                 });

                ToggleIcon.Text("▼");
            }

            internal async Task Collapse()
            {
                await SetIsExpanded(false);
                if (children.None()) return;
                if (ChildrenContainer is null) return;
                await ChildrenContainer.IgnoredAsync();

                ToggleIcon.Text("▶");
            }

            internal async Task GenerateView(View parent)
            {
                try
                {
                    if (View != null) return;

                    View = ViewGenerator?.Invoke(this);
                    if (View is null) return;

                    View.Tapped.Handle(Tapped.Raise);

                    Row = new Stack(RepeatDirection.Horizontal) { Id = "TreeNode:" + (Source as View)?.GetFullPath() ?? Source?.ToString() };

                    await Row.AddRange(new[] { ToggleIcon, View });

                    var left = (parent as TreeView ?? parent.FindParent<TreeView>())?.IndentSpace * Depth;
                    Row.Margin(left: left);

                    await parent.Add(Row);

                    if (IsExpanded && children.Any()) await Expand();
                }
                catch (Exception ex)
                {
                    Log.For(this).Error(ex, "Error in TreeNode.GenerateView.");
                }
            }

            public async Task Toggle()
            {
                if (ChildrenContainer?.ignored == false) await Collapse();
                else await Expand();
            }

            IHierarchy IHierarchy.GetParent() => Parent;

            IEnumerable<IHierarchy> IHierarchy.GetChildren() => Children;
            
            public void Dispose()
            {
                Tapped?.Dispose();
                View?.Dispose();
                if (children != null)
                {
                    lock (children)
                        foreach (var c in children)
                        {
                            c?.Dispose();
                            children.Remove(c);
                        }
                }
            }
        }
    }
}