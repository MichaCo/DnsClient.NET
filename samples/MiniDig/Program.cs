using System;
using System.Diagnostics;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace DigApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            DnsClient.Tracing.Source.Switch.Level = SourceLevels.Verbose;
            DnsClient.Tracing.Source.Listeners.Add(new ConsoleTraceListener());

            var app = new CommandLineApplication(throwOnUnexpectedArg: true);

            app.Command("perf", (perfApp) => new PerfCommand(perfApp, args), throwOnUnexpectedArg: true);
            app.Command("random", (randApp) => new RandomCommand(randApp, args), throwOnUnexpectedArg: true);

            var defaultCommand = new DigCommand(app, args);

            try
            {
                await app.ExecuteAsync(args).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    /* Example code to hook Microsoft.Extensions.Logging up with DnsClient */

    ////var services = new ServiceCollection();
    ////services.AddLogging(o =>
    ////{
    ////    o.AddConsole();
    ////    o.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    ////});

    ////var provider = services.BuildServiceProvider();
    ////var factory = provider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
    ////DnsClient.Logging.LoggerFactory = new LoggerFactoryWrapper(factory);

    ////internal class LoggerFactoryWrapper : DnsClient.Internal.ILoggerFactory
    ////{
    ////    private readonly Microsoft.Extensions.Logging.ILoggerFactory _microsoftLoggerFactory;

    ////    public LoggerFactoryWrapper(Microsoft.Extensions.Logging.ILoggerFactory microsoftLoggerFactory)
    ////    {
    ////        _microsoftLoggerFactory = microsoftLoggerFactory ?? throw new ArgumentNullException(nameof(microsoftLoggerFactory));
    ////    }

    ////    public DnsClient.Internal.ILogger CreateLogger(string categoryName)
    ////    {
    ////        return new DnsLogger(_microsoftLoggerFactory.CreateLogger(categoryName));
    ////    }

    ////    private class DnsLogger : DnsClient.Internal.ILogger
    ////    {
    ////        private readonly Microsoft.Extensions.Logging.ILogger _logger;

    ////        public DnsLogger(Microsoft.Extensions.Logging.ILogger logger)
    ////        {
    ////            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    ////        }

    ////        public bool IsEnabled(DnsClient.Internal.LogLevel logLevel)
    ////        {
    ////            return _logger.IsEnabled((Microsoft.Extensions.Logging.LogLevel)logLevel);
    ////        }

    ////        public void Log(DnsClient.Internal.LogLevel logLevel, int eventId, Exception exception, string message, params object[] args)
    ////        {
    ////            _logger.Log((Microsoft.Extensions.Logging.LogLevel)logLevel, eventId, exception, message, args);
    ////        }
    ////    }
    ////}
}