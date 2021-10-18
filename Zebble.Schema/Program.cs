namespace Zebble.Schema
{
    using System.Diagnostics;
    using Zebble.Tooling;

    class Program
    {
        static void Main(string[] args)
        {
            var block = args.GetBoolOption("block");

            if (block) SchemaGenerator.Run();
            else
                Process.Start(new ProcessStartInfo("zebble-schema")
                {
                    Arguments = "--block",
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = true,
                    CreateNoWindow = true
                });
        }
    }
}