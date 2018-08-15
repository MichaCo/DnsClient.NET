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
            var client = new LookupClient();
            client.EnableAuditTrail = true;

            while (true)
            {
                Task.Delay(1000).GetAwaiter().GetResult();

                var result = client.Query("googlecom", QueryType.ANY);
                Console.WriteLine(result.AuditTrail);
                if (result.HasError)
                {
                    Console.WriteLine("Error response: " + result.ErrorMessage);
                }
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
            }
        }

        public static void WriteLongLine()
        {
            Console.WriteLine("----------------------------------------------------");
        }
    }
}