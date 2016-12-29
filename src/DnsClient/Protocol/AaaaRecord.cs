using System;
using System.Net;

namespace DnsClient.Protocol
{
    /* https://tools.ietf.org/html/rfc3596#section-2.2
    2.2 AAAA data format

    A 128 bit IPv6 address is encoded in the data portion of an AAAA
    resource record in network byte order (high-order byte first).
    */
    /// <summary>
    /// A 128 bit IPv6 address is encoded in the data portion of an AAAA
    /// resource record in network byte order(high-order byte first).
    /// </summary>
    public class AaaaRecord : DnsResourceRecord
    {
        public IPAddress Address { get; }

        public AaaaRecord(ResourceRecordInfo info, IPAddress address) : base(info)
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