using System;
using System.Net;
using Microsoft.Extensions.CommandLineUtils;

namespace DigApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ////var result = Interop.DNSQueryer.QueryDNSForRecordTypeSpecificNameServers(
            ////    "google.com", 
            ////    new[] { IPAddress.Parse("8.8.8.8") }, 
            ////    Interop.DNSQueryer.DnsRecordTypes.DNS_TYPE_A);

            ////Console.WriteLine(result.Length);
            ////return;

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