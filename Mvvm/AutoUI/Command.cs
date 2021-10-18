using System;
using System.Reflection;
using Olive;

namespace Zebble.Mvvm.AutoUI
{
    partial class InvokeNode : Node
    {
        public int Index;
        Action Action;

        public string NaturalLabel => Label.ToLiteralFromPascalCase().CapitaliseFirstLetters();

        public InvokeNode(string name, Action action, Node parent) : base(name, parent)
        {
            Action = action;
        }

        public InvokeNode(MethodInfo method, object target, Node parent) : base(method.Name, parent)
        {
            Action = () => method?.Invoke(target, new object[0]);
        }

        public void Execute() => Action?.Invoke();
    }
}