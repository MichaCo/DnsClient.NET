using System;
using System.Collections.Generic;

namespace DnsClient.Protocol
{
    /* https://tools.ietf.org/html/rfc4034#section3.1
      3.1 RRSIG RDATA Wire Format

           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
           |        Type Covered           |  Algorithm    |     Labels    |
           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
           |                         Original TTL                          |
           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
           |                      Signature Expiration                     |
           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
           |                      Signature Inception                      |
           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
           |            Key Tag            |                               /
           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+         Signer's Name         /
           /                                                               /
           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
           /                                                               /
           /                            Signature                          /
           /                                                               /
           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

        Type Covered: The Type Covered field identifies the type of the RRset that is
        covered by this RRSIG record.

        Algorithm Number: The Algorithm Number field identifies the cryptographic algorithm
        used to create the signature.

        Labels: The Labels field specifies the number of labels in the original RRSIG
        RR owner name.

        Original TTL: The Original TTL field specifies the TTL of the covered RRset as it
        appears in the authoritative zone.

        Signature Expiration and Inception: The Signature Expiration and Inception fields specify a validity
        period for the signature.

        Key Tag: The Key Tag field contains the key tag value of the DNSKEY RR that
        validates this signature, in network byte order.

        Signer's Name: The Signer's Name field value identifies the owner name of the DNSKEY
        RR that a validator is supposed to use to validate this signature.

        Signature Field: The Signature field contains the cryptographic signature that covers
        the RRSIG RDATA (excluding the Signature field) and the RRset
        specified by the RRSIG owner name, RRSIG class, and RRSIG Type
        Covered field.  The format of this field depends on the algorithm in
        use, and these formats are described in separate companion documents.
    */

    /// <summary>
    /// A <see cref="DnsResourceRecord"/> representing a RRSIG record.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc4033"/>
    /// <seealso href="https://tools.ietf.org/html/rfc4034"/>
    /// <seealso href="https://tools.ietf.org/html/rfc4035"/>
    public class RRSigRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets the type of the RRset that is covered by this <see cref="RRSigRecord"/>.
        /// </summary>
        public ResourceRecordType CoveredType { get; }

        /// <summary>
        /// Gets the cryptographic algorithm used to create the <see cref="Signature"/>.
        /// </summary>
        public DnsSecurityAlgorithm Algorithm { get; }

        /// <summary>
        /// Gets the number of labels in the original <see cref="RRSigRecord"/> RR owner name.
        /// </summary>
        public byte Labels { get; }

        /// <summary>
        /// Gets the TTL of the covered RRset as it appears in the authoritative zone.
        /// </summary>
        public long OriginalTtl { get; }

        /// <summary>
        /// Gets the expiration date of the <see cref="Signature"/>.
        /// This record MUST NOT be used for authentication prior to the <see cref="SignatureInception"/>
        /// and MUST NOT be used for authentication after the <see cref="SignatureExpiration"/>.
        /// </summary>
        public DateTimeOffset SignatureExpiration { get; }

        /// <summary>
        /// Gets the inception date of the <see cref="Signature"/>.
        /// This record MUST NOT be used for authentication prior to the <see cref="SignatureInception"/>
        /// and MUST NOT be used for authentication after the <see cref="SignatureExpiration"/>.
        /// </summary>
        public DateTimeOffset SignatureInception { get; }

        /// <summary>
        /// Gets the key tag value of the <see cref="DnsKeyRecord"/> that validates this <see cref="Signature"/>.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc4034#appendix-B">Key Tag Calculation</seealso>
        public int KeyTag { get; }

        /// <summary>
        /// Gets the value which identifies the owner name of the <see cref="DnsKeyRecord"/>
        /// that a validator is supposed to use to validate this <see cref="Signature"/>.
        /// </summary>
        public DnsString SignersName { get; }

        /// <summary>
        /// Gets the cryptographic signature that covers the RRSIG RDATA (excluding the Signature field)
        /// and the RRset specified by the RRSIG owner name, RRSIG class, and RRSIG Type Covered field.
        /// The format of this field depends on the algorithm in use.
        /// </summary>
        public IReadOnlyList<byte> Signature { get; }

        /// <summary>
        /// Gets the base64 string representation of the <see cref="Signature"/>.
        /// </summary>
        public string SignatureAsString { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RRSigRecord"/> class.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="coveredType"></param>
        /// <param name="algorithm"></param>
        /// <param name="labels"></param>
        /// <param name="originalTtl"></param>
        /// <param name="signatureExpiration"></param>
        /// <param name="signatureInception"></param>
        /// <param name="keyTag"></param>
        /// <param name="signersName"></param>
        /// <param name="signature"></param>
        /// <exception cref="ArgumentNullException">If <paramref name="info"/>, <paramref name="signersName"/> or <paramref name="signature"/> is null.</exception>
        public RRSigRecord(
            ResourceRecordInfo info,
            int coveredType,
            byte algorithm,
            byte labels,
            long originalTtl,
            long signatureExpiration,
            long signatureInception,
            int keyTag,
            DnsString signersName,
            byte[] signature)
            : base(info)
        {
            CoveredType = (ResourceRecordType)coveredType;
            Algorithm = (DnsSecurityAlgorithm)algorithm;
            Labels = labels;
            OriginalTtl = originalTtl;
            SignatureExpiration = FromUnixTimeSeconds(signatureExpiration);
            SignatureInception = FromUnixTimeSeconds(signatureInception);
            KeyTag = keyTag;
            SignersName = signersName ?? throw new ArgumentNullException(nameof(signersName));
            Signature = signature ?? throw new ArgumentNullException(nameof(signature));
            SignatureAsString = Convert.ToBase64String(signature);
        }

        private protected override string RecordToString()
        {
            return string.Format(
                "{0} {1} {2} {3} {4} {5} {6} {7} {8}",
                CoveredType,
                Algorithm,
                Labels,
                OriginalTtl,
                SignatureExpiration.ToString("yyyyMMddHHmmss"),
                SignatureInception.ToString("yyyyMMddHHmmss"),
                KeyTag,
                SignersName,
                SignatureAsString);
        }

        // DateTimeOffset does have that method build in .NET47+ but not .NET45 which we will support. TODO: delete this when we drop support for .NET 4.5
        private static DateTimeOffset FromUnixTimeSeconds(long seconds)
        {
            long ticks = seconds * TimeSpan.TicksPerSecond + new DateTime(1970, 1, 1, 0, 0, 0).Ticks;
            return new DateTimeOffset(ticks, TimeSpan.Zero);
        }
    }
}