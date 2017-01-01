using System;
using System.Linq;
using System.Net;

namespace DnsClient.Protocol
{
    public class AddressRecord : DnsResourceRecord
    {
        public IPAddress Address { get; }

        public AddressRecord(ResourceRecordInfo info, IPAddress address)
            : base(info)
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