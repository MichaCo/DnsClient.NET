using System;
using DnsClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiDesign
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var services = new ServiceCollection();
            services
                .AddLogging(c =>
                {
                    c.AddConsole();
                    c.SetMinimumLevel(LogLevel.Debug);
                })
                .AddOptions();

            services.AddSingleton(new LookupClientOptions(NameServer.GooglePublicDns2)
            {
            });

            services.AddSingleton<ILookupClient>(f =>
            {
                var o = f.GetRequiredService<LookupClientOptions>();
                return new LookupClient(o);
            });

            //services.AddSingleton<ILookupClient, LookupClient>();

            /* - -- - - - - - */
            var provider = services.BuildServiceProvider();

            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            DnsClient.Logging.LoggerFactory = new LoggerFactoryWrapper(loggerFactory);

            var logger = provider.GetRequiredService<ILogger<Program>>();
            var lookup = provider.GetRequiredService<ILookupClient>();

            logger.LogInformation($"Starting stuff...");

            var x = lookup.Query("google.com", QueryType.A);

            var r = lookup.QueryServer(new NameServer[] { NameServer.Cloudflare, NameServer.Cloudflare2 }, new DnsQuestion("google.com", QueryType.A));
            
            Console.ReadKey();
        }
    }

    ////var provider = services.BuildServiceProvider();
    ////var factory = provider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
    ////DnsClient.Logging.LoggerFactory = new LoggerFactoryWrapper(factory);

    internal class LoggerFactoryWrapper : DnsClient.Internal.ILoggerFactory
    {
        private readonly Microsoft.Extensions.Logging.ILoggerFactory _microsoftLoggerFactory;

        public LoggerFactoryWrapper(Microsoft.Extensions.Logging.ILoggerFactory microsoftLoggerFactory)
        {
            _microsoftLoggerFactory = microsoftLoggerFactory ?? throw new ArgumentNullException(nameof(microsoftLoggerFactory));
        }

        public DnsClient.Internal.ILogger CreateLogger(string categoryName)
        {
            return new DnsLogger(_microsoftLoggerFactory.CreateLogger(categoryName));
        }

        private class DnsLogger : DnsClient.Internal.ILogger
        {
            private readonly Microsoft.Extensions.Logging.ILogger _logger;

            public DnsLogger(Microsoft.Extensions.Logging.ILogger logger)
            {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public bool IsEnabled(DnsClient.Internal.LogLevel logLevel)
            {
                return _logger.IsEnabled((Microsoft.Extensions.Logging.LogLevel)logLevel);
            }

            public void Log(DnsClient.Internal.LogLevel logLevel, int eventId, Exception exception, string message, params object[] args)
            {
                _logger.Log((Microsoft.Extensions.Logging.LogLevel)logLevel, eventId, exception, message, args);
            }
        }
    }
}