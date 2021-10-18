namespace MSBuildWatcher
{
    using System;
    using System.ServiceProcess;

    static class Program
    {
        // NOTE: To install this:
        // - Open a command prompt as ADMIN
        // - Go to the bin\Debug folder
        // - RUN > C:\Windows\Microsoft.NET\Framework\v4.0.30319\installutil.exe MSBuildWatcher.exe
        // - Start > Services
        // - Find "Zebble MSbuildWatcher"
        // - Right click and select "Properties"
        // - Change Startup type to "Automatic"
        // - Click "Start"
        // - Go to Recovery tab
        // - Change all drop downs to Restart the Service

        static void Main()
        {
            var service = new WatcherService();

            if (Environment.UserInteractive)
            {
                service.Start();
                Console.WriteLine("Press any key to stop...");
                Console.ReadKey(intercept: true);
                Console.WriteLine("Stopping service...");
                service.Stop();
                Console.WriteLine("Service stopped.");
            }
            else
            {
                ServiceBase.Run(new[] { service });
            }
        }
    }
}