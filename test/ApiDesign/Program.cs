using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DnsClient2;

namespace ApiDesign
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var lookup = new LookupClient();
            lookup.Timeout = System.Threading.Timeout.InfiniteTimeSpan;

            var result = lookup.QueryAsync("google.com", 255).GetAwaiter().GetResult();
        }
    }
}
