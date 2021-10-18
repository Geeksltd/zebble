using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Olive;

namespace Zebble.Mvvm.AutoUI
{
    partial class ViewModelNode : Node
    {
        public ViewModel ViewModel;

        public ViewModelNode(string label, ViewModel vm, Node parent, List<ViewModel> crawled = null) : base(label, parent)
        {
            crawled = crawled ?? new List<ViewModel>();
            ViewModel = vm;

            Children.AddRange(
                vm.GetBindables().Except(x => x.Name == "Source" && IsFramework(x.Member))
                .Select(b => new ValueNode(b.Name, b.ReadValue(), this)));

            foreach (var b in vm.GetNestedViewModels())
            {
                if (crawled.Contains(b.Value)) continue;
                Children.Add(new ViewModelNode(b.Key, b.Value, this, crawled));
            }

            Children.AddRange(GetCallableMethods().Select(x => new InvokeNode(x, vm, this)));
        }

        internal MethodInfo[] GetCallableMethods()
        {
            return ViewModel.GetType()
                      .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                      .Except(x => x.IsSpecialName)
                      .Except(x => x.IsPrivate)
                      .Where(v => v.GetParameters().None())
                      .Except(IsFramework)
                      .ToArray();

            // TODO: Allow simple parameters
        }

        static string[] Disallowed;

        static bool IsFramework(MemberInfo member)
        {
            if (Disallowed == null)
                Disallowed = typeof(ViewModel).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                .Except(x => x.IsSpecialName)
                .Select(v => v.Name)
                .ToArray();

            if (member.DeclaringType.Namespace.StartsWithAny("Zebble.Mvvm", "System")) return true;
            if (member.Name.IsAnyOf(Disallowed)) return true;
            return false;
        }
    }
}