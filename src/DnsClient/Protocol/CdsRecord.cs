using System;

namespace DnsClient.Protocol
{
    /// <summary>
    /// https://datatracker.ietf.org/doc/html/rfc7344#section-3.1
    /// The wire and presentation format of the Child DS (CDS) resource record is identical to the DS record [RFC4034]
    /// </summary>
    public class CdsRecord : DsRecord
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CdsRecord"/> class. The record is identical to <see cref="DsRecord"/>
        /// </summary>
        /// <param name="info"></param>
        /// <param name="keyTag"></param>
        /// <param name="algorithm"></param>
        /// <param name="digestType"></param>
        /// <param name="digest"></param>
        /// <exception cref="ArgumentNullException">If <paramref name="info"/> or <paramref name="digest"/> is null.</exception>
        public CdsRecord(ResourceRecordInfo info, int keyTag, byte algorithm, byte digestType, byte[] digest) : base(info, keyTag, algorithm, digestType, digest)
        {
        }
    }
}
