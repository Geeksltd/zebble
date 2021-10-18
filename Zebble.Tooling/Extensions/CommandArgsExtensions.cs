namespace Zebble.Tooling
{
    using System.Linq;
    using Olive;

    static class CommandArgsExtensions
    {
        public static string GetCommand(this string[] args) => args.ElementAtOrDefault(0)?.ToLower();

        public static bool GetBoolOption(this string[] args, string name) => args?.Contains($"--{name}") ?? default;

        public static string GetStringOption(this string[] args, string name, string @default = default)
        {
            if (args == null) return @default;

            var index = args.IndexOf(x => x == $"--{name}");

            if (index != -1)
                return args.ElementAtOrDefault(index + 1) ?? @default;

            return @default;
        }
    }
}