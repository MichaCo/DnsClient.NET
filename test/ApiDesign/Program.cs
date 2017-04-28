using System;
using System.Linq;
using System.Net;
using System.Threading;
using DnsClient;
using DnsClient.Protocol;

namespace ApiDesign
{
    public class Program
    {
        public static void PrintHostEntry(string hostOrIp)
        {
            var lookup = new LookupClient();
            IPHostEntry hostEntry = lookup.GetHostEntry(hostOrIp);
            Console.WriteLine(hostEntry.HostName);
            foreach (var ip in hostEntry.AddressList)
            {
                Console.WriteLine(ip);
            }
            foreach (var alias in hostEntry.Aliases)
            {
                Console.WriteLine(alias);
            }
        }

        public static void Main(string[] args)
        {
            var client = new LookupClient();
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.EnableAuditTrail = true;

            PrintHostEntry("localhost");

            var nsServers = client.Query("google.com", QueryType.NS).Answers.NsRecords();

            foreach (var server in nsServers)
            {
                PrintHostEntry(server.NSDName);
            }

            try
            {
                var address = IPAddress.Parse("216.239.32.10");
                var result = client.QueryReverseAsync(address).Result;
                Console.WriteLine($"Reverse query for arpa: {address.GetArpaName()}");

                Console.WriteLine(result.AuditTrail);

                WriteLongLine();
                var answer = result.Answers.OfType<PtrRecord>().FirstOrDefault();
                Console.WriteLine(answer?.ToString(-32));
                WriteLongLine();

                if (answer != null)
                {
                    var mResult = client.QueryAsync(answer.PtrDomainName.Value, QueryType.A, QueryClass.IN).Result;

                    Console.WriteLine(mResult.AuditTrail);
                    WriteLongLine();
                }

                var gResult = client.QueryAsync("google.com", QueryType.ANY).GetAwaiter().GetResult();

                Console.WriteLine(gResult.AuditTrail);
                WriteLongLine();
                Console.WriteLine(gResult.Answers.FirstOrDefault()?.ToString(-32));

                WriteLongLine();
                ////Console.WriteLine("Service Lookup");
                ////var consul = new LookupClient(IPAddress.Parse("127.0.0.1"), 8600);
                ////var services = consul.ResolveServiceAsync("service.consul", "redis").Result;

                ////foreach (var service in services)
                ////{
                ////    Console.WriteLine($"Found service {service.HostName} at {string.Join(", ", service.AddressList.Select(p => p.ToString() + ":" + service.Port))}");
                ////}
            }
            catch (DnsResponseException ex)
            {
                Console.WriteLine(ex);
            }
            catch (AggregateException agg)
            {
                agg.Handle(e =>
                {
                    if (e is DnsResponseException)
                    {
                        Console.WriteLine(e);
                        return true;
                    }

                    return false;
                });
            }

            Console.ReadKey();
        }

        public static void WriteLongLine()
        {
            Console.WriteLine("----------------------------------------------------");
        }
    }
}