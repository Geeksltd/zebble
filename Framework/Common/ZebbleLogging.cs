namespace Zebble
{
    using System;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Zebble.Device;
    using Olive;
    using Microsoft.Extensions.Logging.Configuration;

    class ZebbleLoggerProvider : ILoggerProvider
    {
        readonly IOptions<ZebbleLoggerOptions> Options;

        public ZebbleLoggerProvider(IOptions<ZebbleLoggerOptions> options) => Options = options;

        public ILogger CreateLogger(string categoryName) => new ZebbleLogger(Options);

        public void Dispose() { }

        class ZebbleLogger : ILogger<string>
        {
            readonly Action<ExceptionLog> OnError;

            public ZebbleLogger(IOptions<ZebbleLoggerOptions> options) => OnError = options.Value.OnError;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel)) return;

                var message = formatter(state, exception);
                if (message.IsEmpty()) return;

#if UWP || IOS
                System.Diagnostics.Debug.WriteLine(message);
#elif ANDROID
                Android.Util.Log.Info("app", message);
#else
                Console.WriteLine(message);
#endif

                if (UIRuntime.IsDebuggerAttached) return;

                try { OnError?.Invoke(new ExceptionLog(exception, null, null, -1)); }
                catch
                {
                    // No logging needed.
                }
            }

            public bool IsEnabled(LogLevel logLevel) => UIRuntime.IsDebuggerAttached || logLevel >= LogLevel.Warning;

            public IDisposable BeginScope<TState>(TState state) => default;
        }
    }

    public static class LoggingBuilderExtensions
    {
        public static ILoggingBuilder AddZebbleLogging(this ILoggingBuilder builder, Action<ZebbleLoggerOptions> configure = null)
        {
            builder.AddConfiguration();

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ZebbleLoggerProvider>());

            LoggerProviderOptions.RegisterProviderOptions<ZebbleLoggerOptions, ZebbleLoggerOptions>(builder.Services);

            if (configure != null)
                builder.Services.Configure(configure);

            return builder;
        }
    }

    public class ZebbleLoggerOptions
    {
        public Action<ExceptionLog> OnError { get; set; }
    }
}
