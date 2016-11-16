using System;
using Microsoft.Extensions.CommandLineUtils;

namespace DigApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var client = new DnsClient2.DnsLookupClient(new DnsClient2.DnsUdpMessageInvoker(), DnsClient2.NameServer.ResolveNameServers());

            client.QueryAsync(new DnsClient2.DnsRequestMessage())

            var app = new CommandLineApplication(throwOnUnexpectedArg: false);

            var perfApplication = app.Command("perf", (perfApp) => new PerfCommand(perfApp, args), false);

            var defaultCommand = new DigCommand(app, args);

            app.Execute(args);
        }
    }
}