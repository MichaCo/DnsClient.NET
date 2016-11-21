using System;
using Microsoft.Extensions.CommandLineUtils;

namespace DigApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false);

            var perfApplication = app.Command("perf", (perfApp) => new PerfCommand(perfApp, args), false);

            var defaultCommand = new DigCommand(app, args);

            try
            {
                app.Execute(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}