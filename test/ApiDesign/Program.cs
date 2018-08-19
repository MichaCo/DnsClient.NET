using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DnsClient;
using DnsClient.Protocol;

namespace ApiDesign
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // version a
            var client = new LookupClient(IPAddress.Loopback);

            // version b
            client = new LookupClient(new LookupClientOptions(
                NameServer.GooglePublicDns, NameServer.GooglePublicDns2, NameServer.GooglePublicDns2IPv6, NameServer.GooglePublicDnsIPv6)
            {
                UseCache = true,
                ContinueOnDnsError = true,
                EnableAuditTrail = true,
                UseRandomNameServer = false
            });

            while (true)
            {
                var result = client.Query("google.com", QueryType.ANY);
                Console.WriteLine(result.AuditTrail);
                if (result.HasError)
                {
                    Console.WriteLine("Error response: " + result.ErrorMessage);
                }
                Console.WriteLine();
                Task.Delay(1000).GetAwaiter().GetResult();
            }
        }

        public static void WriteLongLine()
        {
            Console.WriteLine("----------------------------------------------------");
        }
    }
}