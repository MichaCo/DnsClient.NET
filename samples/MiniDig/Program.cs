// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace DigApp
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            DnsClient.Tracing.Source.Switch.Level = SourceLevels.Warning;
            DnsClient.Tracing.Source.Listeners.Add(new ConsoleTraceListener());

            using var app = new CommandLineApplication();
            app.UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.Throw;

            try
            {
                var perApp = app.Command("perf", (perfApp) => _ = new PerfCommand(perfApp, args));
                perApp.UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.Throw;
                var randomApp = app.Command("random", (randApp) => _ = new RandomCommand(randApp, args));
                randomApp.UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.Throw;

                // Command must initialize so that it adds the configuration.
                _ = new DigCommand(app, args);

                return await app.ExecuteAsync(args).ConfigureAwait(false);
            }
            catch (UnrecognizedCommandParsingException ex)
            {
                Console.WriteLine(ex.Message);
                app.ShowHelp();
            }
            catch (CommandParsingException ex)
            {
                Console.WriteLine(ex.Message);
                app.ShowHelp();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 500;
            }

            return -1;
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
