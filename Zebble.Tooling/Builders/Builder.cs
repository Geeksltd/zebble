namespace Zebble.Tooling
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Runtime.CompilerServices;
    using Olive;

    abstract class Builder
    {
        const int TOTAL_LOG_WIDTH = 50;

        internal static bool ShouldLog;

        readonly DateTime _start = LocalTime.Now;
        readonly Dictionary<string, Action> _steps = new();

        public virtual List<KeyValuePair<string, string>> LogMessages { get; } = new List<KeyValuePair<string, string>>();

        protected abstract void AddTasks();

        protected void Add(Expression<Action> step)
        {
            if (!(step.Body is MethodCallExpression method))
                throw new FormatException("Passed expression isn't a method call.");

            var name = method.Method.Name;

            _steps.Add(name, () => method.Method.Invoke(this, new object[0]));
        }

        protected void Add(string key, Action step) => _steps.Add(key, step);

        protected void Log(string message, [CallerMemberName] string step = "")
            => LogMessages.Add(KeyValuePair.Create(step, message.Trim()));

        public void ExecuteTasks()
        {
            AddTasks();

            foreach (var step in _steps)
            {
                try
                {
                    Console.Write($"Running {step.Key}...");
                    step.Value();

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Done. {Math.Round(LocalTime.Now.Subtract(_start).TotalSeconds, 1)}s");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Failed: {ex}");
                    Console.ResetColor();
                    throw;
                }
            }
        }

        public int Execute()
        {
            try
            {
                ExecuteTasks();
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.ToLogString());
                Console.ResetColor();
                return -1;
            }
            finally
            {
                if (ShouldLog) PrintLog();
            }
        }

        public void PrintLog()
        {
            foreach (var item in LogMessages.Where(x => x.Value.HasValue()).GroupBy(x => x.Key))
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"-------Log: {item.Key.PadRight(TOTAL_LOG_WIDTH, '-')}");
                Console.ForegroundColor = ConsoleColor.DarkGray;

                foreach (var x in item)
                    Console.WriteLine(x.Value);
            }

            Console.ResetColor();
        }
    }
}