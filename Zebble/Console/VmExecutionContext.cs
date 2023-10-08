using System;
using System.Linq;
using Zebble.Mvvm.AutoUI;
using Olive;

namespace Zebble.Mvvm
{
    partial class VmExecutionContext
    {
        void RenderStack()
        {
            var stack = ViewModel.Stack
                .Select(v => v.GetType().GetProgrammingName(useGlobal: false, useNamespace: false))
                .Reverse().ToArray();

            if (stack.None()) return;

            Console.Write("Nav stack: ", ConsoleColor.DarkGray);
            foreach (var item in stack)
            {
                Console.Write(item, ConsoleColor.Gray);
                Console.Write(" > ", ConsoleColor.DarkGray);
            }

            Console.WriteLine();
        }

        internal void Render()
        {
            System.Console.Clear();
            Console.WriteLine();
            RenderStack();
            Console.WriteLine();

            Console.WriteLine(Title + " (Modal)".OnlyWhen(ViewModel.Modals.Any()), ConsoleColor.Cyan);
            Console.WriteLine("─────────────────────────────────────────────────────────────", ConsoleColor.Cyan);

            Root.Children.Except(x => x is InvokeNode).Do(x => x.RenderBlock());
            Console.WriteLine();
            Root.Children.OfType<InvokeNode>().Do(x => x.RenderBlock());
        }

        public void AwaitCommand()
        {
            while (true)
            {
                var choice = Console.AwaitCommand();
                if (choice.IsEmpty()) return;
                if (TryInvoke(choice)) return;
                else Console.ShowError("Invalid choice.");
            }
        }

        bool TryInvoke(string choice)
        {
            if (choice.Contains("=")) return TrySet(choice);

            var commands = Root.GetAllChildren().OfType<InvokeNode>().ToArray();

            var cmd = commands.FirstOrDefault(v => v.Index.ToString() == choice) ??
                      commands.FirstOrDefault(v => v.Label == choice) ??
                      commands.FirstOrDefault(v => v.NaturalLabel == choice);

            if (cmd != null)
            {
                cmd.Execute();
                return true;
            }

            return false;
        }

        static bool TrySet(string text)
        {
            var propertyName = text.Split('=').First().Trim();
            var value = text.Split('=').ExceptFirst().ToString("=").Trim();

            var owner = ViewModel.ActiveScreen;
            if (propertyName.Contains("."))
            {
                // TODO: Find the requested ViewModelNode. 
            }

            var bindables = owner.GetBindables();

            var bindable = bindables.FirstOrDefault(v => v.Name == propertyName)
                ?? bindables.FirstOrDefault(v => v.Name.Equals(propertyName, caseSensitive: false));

            if (bindable == null)
            {
                Console.ShowError("Not found: " + owner.GetType().GetProgrammingName() + " --> " + propertyName);
                return false;
            }

            try
            {
                var typedValue = value.To(bindable.Type);
                bindable.SetValue(typedValue);
                return true;
            }
            catch (Exception ex)
            {
                Console.ShowError(ex.Message);
                return false;
            }
        }
    }
}