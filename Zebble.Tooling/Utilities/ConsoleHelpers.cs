namespace Zebble.Tooling
{
    using System;
    using Olive;

    class ConsoleHelpers
    {
        public static void Error(object message, bool wait = false)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: " + message?.ToStringOrEmpty());
            Console.ResetColor();
            if (wait) Console.Read();
        }

        public static void Progress(string action) => Console.WriteLine(action);
    }
}