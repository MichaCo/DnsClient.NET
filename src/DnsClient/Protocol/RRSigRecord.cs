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

        TYPE COVERED: 2 octet field that identifies the type of the RRset that is
        covered by this RRSIG record, in network byte order

        ALGORITHM NUMBER FIELD: 1 octet field that identifies the cryptographic algorithm
        used to create the signature

        LABELS: 1 octet field that specifies the number of labels in the original RRSIG
        RR owner name

        ORIGINAL TTL: 4 octet field that specifies the TTL of the covered RRset as it
        appears in the authoritative zone, in network byte order

        SIGNATURE EXPIRATION: 4 octet field that specifies the expiration date of the
        signature in the form of a 32-bit unsigned number of seconds elapsed
        since 1 January 1970 00:00:00 UTC, ignoring leap seconds, in network
        byte order

        SIGNATURE INCEPTION: 4 octet field that specifies the inception date of the
        signature in the form of a 32-bit unsigned number of seconds elapsed
        since 1 January 1970 00:00:00 UTC, ignoring leap seconds, in network
        byte order

        KEY TAG: 2 octet field that contains the key tag value of the DNSKEY RR that
        validates this signature, in network byte order

        SIGNER'S NAME FIELD: identifies the owner name of the DNSKEY
        RR that a validator is supposed to use to validate this signature. SIZE UNKNOWN

        SIGNATURE FIELD: ontains the cryptographic signature that covers
        the RRSIG RDATA (excluding the Signature field) and the RRset
        specified by the RRSIG owner name, RRSIG class, and RRSIG Type
        Covered field.  The format of this field depends on the algorithm in
        use SIZE UNKNOWN
    */

    /// <summary>
    /// a <see cref="DnsResourceRecord"/> representing a TLSA record
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc4033"/>
    /// <seealso href="https://tools.ietf.org/html/rfc4034"/>
    /// <seealso href="https://tools.ietf.org/html/rfc4035"/>
    public class RRSigRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets the type of the RRset that is covered by this RRSIG record.
        /// </summary>
        public ResourceRecordType CoveredType { get; }

        /// <summary>
        /// Gets cryptographic algorithm used to create the signature
        /// </summary>
        public byte AlgorithmNumber { get; }

        /// <summary>
        /// Gets number of labels in the original RRSIG RR owner name
        /// </summary>
        public byte Labels { get; }

        /// <summary>
        /// Gets TTL of the covered RRset as it appears in the authoritative zone
        /// </summary>
        public long OriginalTtl { get; }

        /// <summary>
        /// Gets the expiration date of the signature
        /// </summary>
        public DateTimeOffset SignatureExpiration { get; }

        /// <summary>
        /// Gets the inception date of the signature
        /// </summary>
        public DateTimeOffset SignatureInception { get; }

        /// <summary>
        /// Gets the key tag value of the DNSKEY RR that validates this signature
        /// </summary>
        public int KeyTag { get; }

        /// <summary>
        /// Gets the owner name of the DNSKEY RR
        /// </summary>
        public DnsString SignersName { get; }

        /// <summary>
        /// Gets the cryptographic signature that covers the RRSIG RDATA(excluding the Signature field) and the RRset
        /// </summary>
        public IReadOnlyList<byte> Signature { get; }

        /// <summary>
        /// Gets the Base64 string representation of the <see cref="Signature"/>.
        /// </summary>
        public string SignatureAsString { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RRSigRecord"/> class
        /// </summary>
        /// <param name="info"></param>
        /// <param name="coveredType"></param>
        /// <param name="algorithmNumber"></param>
        /// <param name="labels"></param>
        /// <param name="originalTtl"></param>
        /// <param name="signatureExpiration">Stored as YYYYMMDDHHmmSS</param>
        /// <param name="signatureInception">Stored as YYYYMMDDHHmmSS</param>
        /// <param name="keyTag"></param>
        /// <param name="signersName"></param>
        /// <param name="signature">Base64 encoded</param>
        /// <exception cref="ArgumentNullException">If <paramref name="info"/>, <paramref name="signersName"/> or <paramref name="signature"/> is null.</exception>
        public RRSigRecord(
            ResourceRecordInfo info,
            int coveredType,
            byte algorithmNumber,
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
            AlgorithmNumber = algorithmNumber;
            Labels = labels;
            OriginalTtl = originalTtl;
            SignatureExpiration = FromUnixTimeSeconds(signatureExpiration);
            SignatureInception = FromUnixTimeSeconds(signatureInception);
            KeyTag = keyTag;
            SignersName = signersName ?? throw new ArgumentNullException(nameof(signersName));
            Signature = signature ?? throw new ArgumentNullException(nameof(signature));
            SignatureAsString = Convert.ToBase64String(signature);
        }

        /// <summary>
        /// Returns same values as dig
        /// </summary>
        /// <returns></returns>
        private protected override string RecordToString()
        {
            return string.Format(
                "{0} {1} {2} {3} {4} {5} {6} {7} {8}",
                CoveredType,
                AlgorithmNumber,
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