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
        public static void Main(string[] args)
        {
            var lookup = new LookupClient();
            lookup.Timeout = Timeout.InfiniteTimeSpan;
            lookup.EnableAuditTrail = true;

            try
            {
                var result = lookup.QueryReverseAsync(IPAddress.Parse("216.239.32.10")).Result;

                Console.WriteLine(result.AuditTrail);

                WriteLongLine();
                var answer = result.Answers.OfType<PtrRecord>().FirstOrDefault();
                Console.WriteLine(answer?.ToString(-32));
                WriteLongLine();

                if (answer != null)
                {
                    var mResult = lookup.QueryAsync(answer.PtrDomainName, QueryType.A, QueryClass.IN).Result;

                    Console.WriteLine(mResult.AuditTrail);
                    WriteLongLine();
                }

                var gResult = lookup.QueryAsync("google.com", QueryType.ANY).GetAwaiter().GetResult();

                Console.WriteLine(gResult.AuditTrail);
                WriteLongLine();
                Console.WriteLine(gResult.Answers.FirstOrDefault()?.ToString(-32));
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