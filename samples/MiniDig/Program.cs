using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace DigApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            var loggerFactory = new LoggerFactory();

            var app = new CommandLineApplication(throwOnUnexpectedArg: true);

            app.Command("perf", (perfApp) => new PerfCommand(perfApp, loggerFactory, args), throwOnUnexpectedArg: true);
            app.Command("random", (randApp) => new RandomCommand(randApp, loggerFactory, args), throwOnUnexpectedArg: true);

            var defaultCommand = new DigCommand(app, loggerFactory, args);

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
}