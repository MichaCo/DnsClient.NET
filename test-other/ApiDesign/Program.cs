using System;
using System.Net;
using System.Threading.Tasks;
using DnsClient;

namespace ApiDesign
{
    public class Program
    {
        public static async Task Main()
        {
            var i = 0;

            var client = new LookupClient(new LookupClientOptions()
            {
                UseCache = false
            });

            while (true)
            {
                var response = await client.QueryAsync("google.com", QueryType.A).ConfigureAwait(false);
                response = client.Query("google.com", QueryType.A);
                i++;

                if (i % 100 == 0)
                {
                    var mem = GC.GetTotalMemory(true);
                    Console.WriteLine($"Ran 100x - mem {mem}");
                }

                if (i % 1000 == 0)
                {
                    Console.WriteLine("ran 1000 times.");
                }
            }
        }
    }
}
