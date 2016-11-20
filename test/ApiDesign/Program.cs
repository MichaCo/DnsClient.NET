using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DnsClient2;
using Microsoft.Extensions.Logging;

namespace ApiDesign
{
    public class Program
    {
        private static ILogger logger;
        private static ILoggerFactory loggerFactory;

        public static void Main(string[] args)
        {
            loggerFactory = new LoggerFactory()
                .AddConsole(LogLevel.Trace)
                .AddDebug(LogLevel.Trace);

            logger = loggerFactory.CreateLogger("testing");

            var lookup = new LookupClient(new DnsEndPoint("127.0.0.1", 8600));
            lookup.Timeout = System.Threading.Timeout.InfiniteTimeSpan;

            //var result = lookup.QueryAsync("google.com", 255).GetAwaiter().GetResult();
            var mResult = lookup.QueryAsync("consul.service.consul", 33, 1).Result;
        }
    }
}
