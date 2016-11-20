using System;
using System.Net;

namespace DnsClient2.Protocol.Record
{
    public class ARecord : ResourceRecord
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