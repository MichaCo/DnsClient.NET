using System;

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
    [CLSCompliant(false)]
    public class RRSIGRecord : DnsResourceRecord
    {

        /// <summary>
        /// Gets the type of the RRset that is covered by this RRSIG record.
        /// </summary>
        public ushort Type { get; }

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
        public uint OriginalTtl { get; }

        /// <summary>
        /// Gets the expiration date of the signature
        /// </summary>
        public string SignatureExpiration { get; }

        /// <summary>
        /// Gets the inception date of the signature
        /// </summary>
        public string SignatureInception { get; }

        /// <summary>
        /// Gets the key tag value of the DNSKEY RR that validates this signature
        /// </summary>
        public ushort KeyTag { get; }

        /// <summary>
        /// Gets the owner name of the DNSKEY RR
        /// </summary>
        public DnsString SignersNameField { get; }

        /// <summary>
        /// Gets the cryptographic signature that covers the RRSIG RDATA(excluding the Signature field) and the RRset
        /// </summary>
        public string SignatureField { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RRSIGRecord"/> class
        /// </summary>
        /// <param name="info"></param>
        /// <param name="type"></param>
        /// <param name="algorithmNumber"></param>
        /// <param name="labels"></param>
        /// <param name="originalTtl"></param>
        /// <param name="signatureExpiration">Stored as YYYYMMDDHHmmSS</param>
        /// <param name="signatureInception">Stored as YYYYMMDDHHmmSS</param>
        /// <param name="keyTag"></param>
        /// <param name="signersNameField"></param>
        /// <param name="signatureField">Base64 encoded</param>
        public RRSIGRecord(ResourceRecordInfo info, ushort type, byte algorithmNumber, byte labels, uint originalTtl,
            uint signatureExpiration, uint signatureInception, ushort keyTag, DnsString signersNameField, string signatureField)
            : base(info)
        {
            Type = type;
            AlgorithmNumber = algorithmNumber;
            Labels = labels;
            OriginalTtl = originalTtl;
            SignatureExpiration = SignatureDateToDigFormat(signatureExpiration);
            SignatureInception = SignatureDateToDigFormat(signatureInception);
            KeyTag = keyTag;
            SignersNameField = signersNameField;
            SignatureField = signatureField;
        }

        /// <summary>
        /// Returns same values as dig
        /// </summary>
        /// <returns></returns>
        private protected override string RecordToString()
        {

            return string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8}", (ResourceRecordType)Type, AlgorithmNumber, Labels,
                OriginalTtl, SignatureExpiration, SignatureInception, KeyTag, SignersNameField, SignatureField);
        }

        private string SignatureDateToDigFormat(uint signatureDate)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(signatureDate).ToRrsigDateString();
        }
    }
}
