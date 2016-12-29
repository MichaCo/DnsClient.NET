using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DnsClient;
using Microsoft.Extensions.CommandLineUtils;

namespace DigApp
{
    public class DigCommand : DnsCommand
    {
        private static readonly int s_printOffset = -32;

        public static string OS
        {
            get
            {
                return RuntimeInformation.OSDescription.Trim();
            }
        }

        public static string Version
        {
            get
            {
                return typeof(DigCommand).GetTypeInfo().Assembly.GetName().Version.ToString();
            }
        }

        public CommandArgument DomainArg { get; }

        public CommandArgument QClassArg { get; }

        public CommandArgument QTypeArg { get; }

        public CommandOption ReversArg { get; }

        public DigCommand(CommandLineApplication app, string[] originalArgs) : base(app, originalArgs)
        {
            DomainArg = app.Argument("domain", "domain name", false);
            QTypeArg = app.Argument("q-type", "QType", false);
            QClassArg = app.Argument("q-class", "QClass", false);
            ReversArg = app.Option("-x", "Reverse lookup shortcut", CommandOptionType.NoValue);
        }

        protected override async Task<int> Execute()
        {
            string useDomain = string.IsNullOrWhiteSpace(DomainArg.Value) ? "." : DomainArg.Value;
            QueryType useQType = 0;
            QueryClass useQClass = 0;

            if (!string.IsNullOrWhiteSpace(QClassArg.Value))
            {
                // q class != null => 3 params (domain already set above).
                Enum.TryParse(QClassArg.Value, true, out useQClass);
                Enum.TryParse(QTypeArg.Value, true, out useQType);
            }
            else
            {
                // q class is null => 2 params only
                // test if no domain is specified and first param is either qtype or qclass
                if (Enum.TryParse(DomainArg.Value, true, out useQType))
                {
                    useDomain = ".";
                    Enum.TryParse(QTypeArg.Value, true, out useQClass);
                }
                else if (Enum.TryParse(DomainArg.Value, true, out useQClass))
                {
                    useDomain = ".";
                }
                else if (!Enum.TryParse(QTypeArg.Value, true, out useQType))
                {
                    // could be q class as second and no QType
                    Enum.TryParse(QTypeArg.Value, true, out useQClass);
                }
            }

            if (ReversArg.HasValue())
            {
                useQType = QueryType.PTR;
                useQClass = QueryClass.IN;
                IPAddress ip;
                if (!IPAddress.TryParse(useDomain, out ip))
                {
                    Console.WriteLine(";; WARNING: recursion requested but not available");
                    return 1;
                }

                useDomain = LookupClient.GetArpaName(ip);
            }

            if (useQType == 0)
            {
                if (string.IsNullOrWhiteSpace(useDomain) || useDomain == ".")
                {
                    useQType = QueryType.NS;
                }
                else
                {
                    useQType = QueryType.A;
                }
            }

            // finally running the command
            var lookup = GetDnsLookup();
            lookup.EnableAuditTrail = true;

            var swatch = Stopwatch.StartNew();

            var result = useQClass == 0 ?
                await lookup.QueryAsync(useDomain, useQType) :
                await lookup.QueryAsync(useDomain, useQType, useQClass);

            var elapsed = swatch.ElapsedMilliseconds;

            // Printing infomrational stuff
            var useServers = lookup.NameServers;
            
            Console.WriteLine();
            Console.WriteLine($"; <<>> MiniDiG {Version} {OS} <<>> {string.Join(" ", OriginalArgs)}");

            Console.WriteLine(result.AuditTrail);
            
            return 0;
        }
    }
}