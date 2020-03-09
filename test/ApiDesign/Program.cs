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

            var x = client.QueryServer(new NameServer[] { IPAddress.Loopback }, "query", QueryType.A);
            client = new LookupClient(new LookupClientOptions(
               NameServer.Cloudflare, NameServer.Cloudflare2)
            {
                UseCache = true,
                ContinueOnDnsError = true,
                EnableAuditTrail = true,
                UseRandomNameServer = false,
                ExtendedDnsBufferSize = 4000,
                RequestDnsSecRecords = true,
                Recursion = true,
                MaximumCacheTimeout = TimeSpan.FromSeconds(5)
            });

            while (true)
            {
                var result = client.QueryServer(new[] { IPAddress.Loopback }, "mcnet.com", QueryType.ANY);
                Console.WriteLine(result.AuditTrail);
                ////Task.Delay(1003).GetAwaiter().GetResult();
                ////var result2 = client.Query("dnsclient.michaco.net", QueryType.A, queryOptions: new DnsQueryAndServerOptions()
                ////{
                ////    ContinueOnDnsError = true,
                ////    EnableAuditTrail = true
                ////});

                ////Console.WriteLine(result2.AuditTrail);
                if (result.HasError)
                {
                    Console.WriteLine("Error response: " + result.ErrorMessage);
                }
                WriteLongLine();
                Task.Delay(1300).GetAwaiter().GetResult();
            }
        }

        public static void WriteLongLine()
        {
            Console.WriteLine("----------------------------------------------------");
        }
    }
}