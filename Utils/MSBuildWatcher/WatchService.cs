namespace MSBuildWatcher
{
    using System;
    using System.Diagnostics;
    using System.ServiceProcess;
    using System.Timers;

    public partial class WatcherService : ServiceBase
    {
        static TimeSpan MAX_ALLOWED_RUN = TimeSpan.FromMinutes(5);

        Timer ExpirationCheckerTimer = null;

        public WatcherService() => InitializeComponent();

        public void Start() => OnStart(new string[0]);

        protected override void OnStart(string[] args)
        {
            ExpirationCheckerTimer = new Timer
            {
                Interval = 5000,
                Enabled = true
            };

            ExpirationCheckerTimer.Elapsed += ExpirationCheckerTimer_Elapsed;
        }

        void ExpirationCheckerTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var processes = Process.GetProcessesByName("msbuild");

                Console.WriteLine("MSBuild processes: " + (processes.Length == 0 ? "None" : ""));

                foreach (var process in processes)
                {
                    var age = DateTime.Now.Subtract(process.StartTime);

                    Console.WriteLine("MSBuild process " + process.Id + " has been running for " + age.Seconds + " seconds. Time to die....");

                    if (age < MAX_ALLOWED_RUN) continue;
                    process.Kill();
                    Console.WriteLine("Killed " + process.Id + " successfully");
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        void Log(string description)
        {
            try { Console.WriteLine(description); }
            catch { }
        }

        protected override void OnStop() => ExpirationCheckerTimer.Enabled = false;
    }
}
