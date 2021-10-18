using System;
using Olive;

namespace Zebble.Mvvm.AutoUI
{
    partial class Node
    {
        public virtual void Render() => System.Console.Write("".PadRight(Depth * 3));

        public virtual void RenderBlock()
        {
            Render();
            Children.Do(x => x.RenderBlock());
        }

        public override string ToString() => Label;
    }

    partial class ValueNode
    {
        public override void Render()
        {
            base.Render();
            System.Console.Write($"{Label}: ");

            if (Value is Exception)
                Console.WriteLine(ValueString, ConsoleColor.Red);
            else if (Value is null)
                Console.WriteLine("null", ConsoleColor.Magenta);
            else
                Console.WriteLine(ValueString, ConsoleColor.White);
        }
    }

    partial class InvokeNode
    {
        public override void Render()
        {
            base.Render();
            Console.WriteLine((Index + ".").PadRight(3) + NaturalLabel, ConsoleColor.Yellow);
        }
    }

    partial class ViewModelNode
    {
        public override void Render()
        {
            base.Render();

            var type = ViewModel.GetType().GetProgrammingName(false, false, false);

            Console.Write(Label, ConsoleColor.Cyan);
            System.Console.WriteLine($" ({type})", ConsoleColor.DarkGreen);
        }

        public override void RenderBlock()
        {
            base.RenderBlock();
            Console.WriteLine();
        }
    }
}