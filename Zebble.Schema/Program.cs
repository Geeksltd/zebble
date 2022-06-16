namespace Zebble.Schema
{
    using System.Diagnostics;
    using System.Globalization;
    using Zebble.Tooling;

    class Program
    {
        static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("en-US");

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