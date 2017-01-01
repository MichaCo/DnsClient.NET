using System;
using System.Net;

namespace DnsClient.Protocol
{
    /*
    3.4.1. A RDATA format

    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |                    ADDRESS                    |
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

    ADDRESS         A 32 bit Internet address.

    Hosts that have multiple Internet addresses will have multiple A
    records.
    *
    */

    /// <summary>
    /// A DNS resource record represending an IP address.
    /// Hosts that have multiple Internet addresses will have multiple A records.
    /// </summary>
    public class ARecord : AddressRecord
    {
        public ARecord(ResourceRecordInfo info, IPAddress address) : base(info, address)
        {
        }
    }
}