using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DnsClient
{
    public static class StaticLog
    {
        public static long ByteArrayAllocations;
        public static long ByteArrayReleases;
        public static long ResolveQueryCount;
        public static long ResolveQueryTries;
        public static long CreatedUdpClients;
    }
}
