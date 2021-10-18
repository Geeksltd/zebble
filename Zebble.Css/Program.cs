namespace Zebble.Css
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using Zebble.Tooling;

    class Program
    {
        static void Main(string[] args)
        {
            switch (args.GetCommand())
            {
                case "generate":
                    new CssCompiler().Run();
                    break;

                case "watch":
                    var block = args.GetBoolOption("block");

                    if (block)
                    {
                        CloseExistingProcess();
                        new CssWatchService().Start();
                    }
                    else
                        Process.Start(new ProcessStartInfo("zebble-css")
                        {
                            Arguments = "watch --block",
                            WindowStyle = ProcessWindowStyle.Minimized,
                            UseShellExecute = true,
                            CreateNoWindow = true
                        });
                    break;

                default:
                    new Unknown().Execute();
                    break;
            }
        }

        static void CloseExistingProcess()
        {
            Console.WriteLine("Closing any currently running zebble-css process.");

            try
            {
                var req = WebRequest.Create("http://" + "localhost:19765/Zebble/Css?exit");
                req.Timeout = 100;
                req.GetResponse().Dispose();

                Console.WriteLine("Closed existing zebble-css process.");
            }
            catch
            {
                // No logging is needed.
            }
        }
    }
}