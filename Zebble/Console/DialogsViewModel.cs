using System;
using System.Collections.Generic;
using System.Linq;
using Zebble.Mvvm.AutoUI;
using Olive;

namespace Zebble.Mvvm
{
    partial class DialogsViewModel
    {
        internal string LastToast;
        string SimulatedTap, ExpectText;

        void DoShowWaiting(bool block) => WriteLine($"({(block ? "wait" : "loading")}...)", ConsoleColor.DarkGray);

        void DoHideWaiting() { }

        void DoToast(string message)
        {
            LastToast = message;
            WriteLine(message, ConsoleColor.Cyan);
        }

        void DoAlertCore(string title, string message)
        {
            WriteLine();

            Console.WriteLine("▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄");
            System.Console.WriteLine("█ ");

            if (title.HasValue())
            {
                System.Console.Write("█ ");
                WriteLine(title.ToUpper(), ConsoleColor.White);
                WriteLine("█ ----------------------------------", ConsoleColor.DarkGray);
            }

            foreach (var line in message.Trim().ToLines())
                WriteLine("█ " + line);

            System.Console.WriteLine("█ ");
            Console.WriteLine("▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀");

            WriteLine();
        }

        T DoDecide<T>(string title, string message, KeyValuePair<string, T>[] buttons)
        {
            DoAlertCore(title, message);

            T result = default;

            var nodes = buttons.Select((b, i) => new InvokeNode(b.Key, () => result = b.Value, null) { Index = i + 1 }).ToArray();
            nodes.Do(x => x.Render());

            if (ExpectText.HasValue())
            {
                var choices = buttons.Select(x => x.Key).ToString(" ");

                var allText = $"{title} | {message} | {choices}";
                if (allText.Contains(ExpectText)) ExpectText = null;
                else ShowFailed($"Failed to find the text '{ExpectText}' in dialog.\nFound: {allText}");

                var picked = nodes.FirstOrDefault(v => v.Label == SimulatedTap);
                if (picked is null) ShowFailed($"Failed to tap '{SimulatedTap}' in dialog.\nOptions: {choices}");
                SimulatedTap = null;
                picked.Execute();
                return result;
            }

            while (true)
            {
                var choice = Console.AwaitCommand();
                if (choice.IsEmpty()) continue;

                var cmd = nodes.FirstOrDefault(v => v.Label == choice)
                    ?? nodes.FirstOrDefault(v => v.Index.ToString() == choice);
                if (cmd != null)
                {
                    cmd.Execute();
                    return result;
                }

                Console.ShowError("Invalid choice.");
            }

            throw new Exception("Impossible line");
        }

        string DoPrompt(string title, string description)
        {
            DoAlertCore(title, description);
            System.Console.Write(">");
            return System.Console.ReadLine().Trim();
        }

        static void ShowFailed(string text)
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine("=== TEST FAILED ===");
            System.Console.WriteLine(text);
            System.Console.ResetColor();
            throw new TestFailedException(text);
        }

        static void WriteLine(string text = "") => Console.WriteLine(text);
        static void WriteLine(string text, ConsoleColor color) => Console.WriteLine(text, color);

        /// <summary>
        /// (Automated testing only) Call this before calling Dialogs.Alert or Dialogs.Confirm, to mock the user answer.
        /// </summary>  
        public void ExpectAndTap(string text, string buttonToTap)
        {
            ExpectText = text;
            SimulatedTap = buttonToTap;
        }
    }
}