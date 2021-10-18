using System.Collections.Generic;
using Zebble.Services;

namespace Zebble.Mvvm.AutoUI
{
    partial class Node : IHierarchy
    {
        public string Label;
        public Node Parent;
        public List<Node> Children = new List<Node>();

        public int Depth => 1 + (Parent?.Depth ?? 0);

        public Node(string label, Node parent)
        {
            Label = label;
            Parent = parent;
        }

        IHierarchy IHierarchy.GetParent() => Parent;

        public IEnumerable<IHierarchy> GetChildren() => Children;
    }
}
