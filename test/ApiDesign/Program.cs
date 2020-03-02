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
                NameServer.Cloudflare, NameServer.Cloudflare2)
            {
                UseCache = true,
                ContinueOnDnsError = true,
                EnableAuditTrail = true,
                UseRandomNameServer = false
            });

            var x = client.QueryServer(new NameServer[] { IPAddress.Loopback }, "query", QueryType.A);

            while (true)
            {
                var result = client.Query("google.com", QueryType.A);
                var result2 = client.Query("google.com", QueryType.A, queryOptions: new DnsQueryAndServerOptions()
                {
                    ContinueOnDnsError = true
                });

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