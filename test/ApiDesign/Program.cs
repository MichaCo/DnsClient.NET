using System.Net;
using DnsClient2;

namespace ApiDesign
{
    public class Program
    {
        
        public static void Main(string[] args)
        {
            var n = new DnsName("lala.com");

            var lookup = new LookupClient(new DnsEndPoint("127.0.0.1", 8600));
            lookup.Timeout = System.Threading.Timeout.InfiniteTimeSpan;

            //var result = lookup.QueryAsync("google.com", 255).GetAwaiter().GetResult();
            var mResult = lookup.QueryAsync("consul.service.consul", 33, 1).Result;
        }
    }
}