using System;
using Zebble.Device;
using System.IO;
using Zebble.Mvvm;
using Olive;

namespace Zebble
{
    public class Console
    {
        // [Obsolete("Instead of this, call UIRuntime.Initialize<Program>()", error: true)]
        public static void Configure()
        {
            IO.Root = AppDomain.CurrentDomain.GetBaseDirectory().GetOrCreateSubDirectory("AppFiles");
            IO.Cache = IO.Root.GetOrCreateSubDirectory("GlobalCache");
            CopyFiles();
        }

        static void CopyFiles()
        {
            IO.Root.Delete(recursive: true);
            IO.Root.EnsureExists();
            AppDomain.CurrentDomain.GetBaseDirectory().Parent.Parent.Parent.Parent.Parent
               .GetSubDirectory($"App.UI{Path.DirectorySeparatorChar}Resources").ExistsOrThrow().CopyTo(IO.Root.FullName);

            IO.Root.GetFile("Installation.Token").WriteAllText(Guid.NewGuid().ToString());
        }

        public static void Start(string[] args)
        {
            while (true)
            {
                var parser = new VmExecutionContext(ViewModel.ActiveScreen);
                parser.Render();
                parser.AwaitCommand();
            }
        }

        internal static void WriteLine(string text = "") => WriteLine(text, ConsoleColor.Gray);
        internal static void ShowError(string text) => WriteLine(text, ConsoleColor.Red);
        internal static void WriteLine(string text, ConsoleColor color, ConsoleColor? background = null)
        {
            System.Console.ForegroundColor = color;
            if (background.HasValue)
                System.Console.BackgroundColor = background.Value;
            System.Console.Write(text);
            System.Console.ResetColor();
            System.Console.WriteLine();
        }

        internal static string AwaitCommand()
        {
            System.Console.WriteLine();
            System.Console.Write("> ");
            return System.Console.ReadLine().Trim();
        }

        internal static void Write(string text, ConsoleColor color)
        {
            System.Console.ForegroundColor = color;
            System.Console.Write(text);
            System.Console.ResetColor();
        }
    }
}
