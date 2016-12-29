using System;
using System.Linq;
using System.Net;
using Microsoft.Extensions.CommandLineUtils;

namespace DigApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //var queryResult = Interop.DNSQueryer.QueryDNSForRecordTypeSpecificNameServers(
            //                "consul.service.consul", new[] { new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8600) } , Interop.DNSQueryer.DnsRecordTypes.DNS_TYPE_A);

            //var result = Interop.DNSQueryer.QueryDNSForRecordTypeSpecificNameServers(
            //    "google.com",
            //    new[] { new IPEndPoint(IPAddress.Parse("127.0.0.1"), 53) },
            //    Interop.DNSQueryer.DnsRecordTypes.DNS_TYPE_A);

            //Console.WriteLine(result.Length + "Results");
            //if (result.Length > 0)
            //{
            //    foreach (var val in result)
            //    {
            //        foreach (var kv in val)
            //        {
            //            Console.WriteLine(kv.Key + "=" + kv.Value);
            //        }
            //    }
            //}
            //return;

            var app = new CommandLineApplication(throwOnUnexpectedArg: true);

            var perfApplication = app.Command("perf", (perfApp) => new PerfCommand(perfApp, args), throwOnUnexpectedArg: true);

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