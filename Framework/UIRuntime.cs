namespace Zebble
{
    using System;
    using System.Reflection;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Olive;

    public partial class UIRuntime
    {
        static bool? isProfilingPerformance;
        internal static bool IsDebuggerAttached;
        public static float SwipeThreshold = 30;

        public static Assembly AppAssembly { get; internal set; }

        static UIRuntime() => IsDebuggerAttached = System.Diagnostics.Debugger.IsAttached;

        public static string GetInstallationToken() => Device.IO.File("Installation.Token").ReadAllText();

        internal static bool IsProfilingPerformance
        {
            get
            {
                if (isProfilingPerformance is null)
                    isProfilingPerformance = Config.Get("Profiling.Performance", defaultValue: false);
                return isProfilingPerformance.Value;
            }
        }

        public static void Initialize<TAnyAppType>(string appName, Action<IHostBuilder> config = null) where TAnyAppType : class
        {
            StartUp.ApplicationName = appName;
            AppAssembly = typeof(TAnyAppType).Assembly;

            var builder = CreateHostBuilder<TAnyAppType>()
                        .ConfigureHostConfiguration(x => x.AddZebbleConfiguration())
                        .ConfigureServices(x => x.AddHttpClient())
                        .ConfigureLogging(x => x.ClearProviders().AddZebbleLogging());

#if IOS || ANDROID
            builder.UseContentRoot(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
#endif

            config?.Invoke(builder);
            var host = builder.Build();

            Context.Initialize(host.Services, scopeServiceProvider: null);
            Log.Init(Context.Current.GetService<ILoggerFactory>());
        }
        static IHostBuilder CreateHostBuilder<T>() where T : class
        {
#if !UWP
            return XamarinHost.CreateDefaultBuilder<T>();
#else
return Host.CreateDefaultBuilder();
#endif
        }
    }
    
}