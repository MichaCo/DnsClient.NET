using System;

namespace DnsClient.Protocol
{
    /// <summary>
    /// https://datatracker.ietf.org/doc/html/rfc7344#section-3.2
    /// The wire and presentation format of the CDNSKEY ("Child DNSKEY") resource record is identical to the DNSKEY record.
    /// </summary>
    public class CdnsKeyRecord : DnsKeyRecord
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CdnsKeyRecord"/> class. The record is identical to <see cref="DnsKeyRecord"/>
        /// </summary>
        /// <param name="info"></param>
        /// <param name="flags"></param>
        /// <param name="protocol"></param>
        /// <param name="algorithm"></param>
        /// <param name="publicKey"></param>
        /// <exception cref="ArgumentNullException">If <paramref name="info"/> or <paramref name="publicKey"/> is null.</exception>
        public CdnsKeyRecord(ResourceRecordInfo info, int flags, byte protocol, byte algorithm, byte[] publicKey) : base(info, flags, protocol, algorithm, publicKey)
        {
        }
    }
}
