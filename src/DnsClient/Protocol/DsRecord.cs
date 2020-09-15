using System;
using System.Collections.Generic;
using System.Linq;

namespace DnsClient.Protocol
{
    /* https://tools.ietf.org/html/rfc4034#section-5.1
       5.1.  DS RDATA Wire Format

        The RDATA for a DS RR consists of a 2 octet Key Tag field, a 1 octet
        Algorithm field, a 1 octet Digest Type field, and a Digest field.

           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
           |           Key Tag             |  Algorithm    |  Digest Type  |
           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
           /                                                               /
           /                            Digest                             /
           /                                                               /
           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    */

    /// <summary>
    /// a <see cref="DnsResourceRecord"/> representing a DS record.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc4034#section-5.1"/>
    public class DsRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets the key tag value of the <see cref="DnsKeyRecord"/> referred to by this record.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc4034#appendix-B">Key Tag Calculation</seealso>
        public int KeyTag { get; }

        /// <summary>
        /// Gets the algorithm of the <see cref="DnsKeyRecord"/> referred to by this record.
        /// </summary>
        public DnsSecurityAlgorithm Algorithm { get; }

        /// <summary>
        /// Gets the algorithm used to construct the digest.
        /// </summary>
        public byte DigestType { get; }

        /// <summary>
        /// Gets the digest of the <see cref="DnsKeyRecord"/> this record refers to.
        /// </summary>
        public IReadOnlyList<byte> Digest { get; }

        /// <summary>
        /// Gets the hexadecimal string representation of the <see cref="Digest"/>.
        /// </summary>
        public string DigestAsString { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DnsKeyRecord"/> class
        /// </summary>
        /// <exception cref="ArgumentNullException">If <paramref name="info"/> or <paramref name="digest"/> is null.</exception>
        public DsRecord(ResourceRecordInfo info, int keyTag, byte algorithm, byte digestType, byte[] digest)
            : base(info)
        {
            KeyTag = keyTag;
            Algorithm = (DnsSecurityAlgorithm)algorithm;
            DigestType = digestType;
            Digest = digest ?? throw new ArgumentNullException(nameof(digest));
            DigestAsString = string.Join(string.Empty, digest.Select(b => b.ToString("X2")));
        }

        private protected override string RecordToString()
        {
            return string.Format("{0} {1} {2} {3}", KeyTag, Algorithm, DigestType, DigestAsString);
        }
    }
}