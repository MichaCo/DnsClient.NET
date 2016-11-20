using System;
using System.Net;

namespace DnsClient2.Protocol.Record
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
    public class ARecord : DnsResourceRecord
    {
        public IPAddress Address { get; }

        public ARecord(ResourceRecordInfo info, IPAddress address) : base(info)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            Address = address;
        }

        public override string RecordToString()
        {
            return Address.ToString();
        }
    }
}