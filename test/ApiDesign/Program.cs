using System;
using System.Linq;
using System.Net;
using DnsClient2;
using DnsClient2.Protocol.Record;

namespace ApiDesign
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var lookup = new LookupClient();
            lookup.Timeout = System.Threading.Timeout.InfiniteTimeSpan;

            try
            {
                var rResult = lookup.QueryReverseAsync(IPAddress.Parse("192.168.178.23")).Result;
                var answer = rResult.Answers.OfType<PtrRecord>().FirstOrDefault();
                Console.WriteLine(answer?.ToString(-32));

                if (answer != null)
                {
                    //var result = lookup.QueryAsync("google.com", 255).GetAwaiter().GetResult();
                    var mResult = lookup.QueryAsync(answer.PtrDomainName, 1, 1).Result;
                    Console.WriteLine(mResult.Answers.FirstOrDefault()?.ToString(-32));
                }

                var gResult = lookup.QueryAsync("google.com", 257).GetAwaiter().GetResult();
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
    }
}