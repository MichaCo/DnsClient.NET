using System;
using System.Linq;

namespace DnsClient.Protocol
{
    /* https://tools.ietf.org/html/rfc6698#section-2.1
    2.1.  TLSA RDATA Wire Format

    The RDATA for a TLSA RR consists of a one-octet certificate usage
    field, a one-octet selector field, a one-octet matching type field,
    and the certificate association data field.

                            1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
        0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        |  Cert. Usage  |   Selector    | Matching Type |               /
        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+               /
        /                                                               /
        /                 Certificate Association Data                  /
        /                                                               /
        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

    */

    /// <summary>
    /// A <see cref="DnsResourceRecord"/> representing a TLSA record.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc6698#section-2.1">RFC 6698</seealso>
    [CLSCompliant(false)]
    public class TlsaRecord : DnsResourceRecord
    {
        /// <summary>
        /// A one-octet value, called "certificate usage", specifies the provided
        /// association that will be used to match the certificate presented in
        /// the TLS handshake.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc6698#section-2.1.1">RFC 6698</seealso>
        public byte CertificateUsage { get; }

        /// <summary>
        /// A one-octet value, called "selector", specifies which part of the TLS
        /// certificate presented by the server will be matched against the
        /// association data.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc6698#section-2.1.2">RFC 6698</seealso>
        /// <seealso href="https://tools.ietf.org/html/rfc5280">RFC 5280</seealso>
        /// <seealso href="https://tools.ietf.org/html/rfc6376">RFC 6376</seealso>
        public byte Selector { get; }

        /// <summary>
        /// A one-octet value, called "matching type", specifies how the
        /// certificate association is presented.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc6698#section-2.1.3">RFC 6698</seealso>
        /// <seealso href="https://tools.ietf.org/html/rfc6234">RFC 6234</seealso>
        public byte MatchingType { get; }

        /// <summary>
        /// This field specifies the "certificate association data" to be
        /// matched.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc6698#section-2.1.4">RFC 6698</seealso>
        public byte[] CertificateAssociationData { get; }

        /// <summary>
        /// This field specifies the "certificate association data" to be
        /// matched. Represented as hex-encoded lowercase string.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc6698#section-2.1.4">RFC 6698</seealso>
        public string CertificateAssociationDataAsString { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TlsaRecord"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="certificateUsage">The certification usage.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="matchingType">The matching type.</param>
        /// <param name="certificateAssociationData">Certificate association data.</param>
        /// <exception cref="System.ArgumentNullException">If <paramref name="certificateAssociationData"/> or <paramref name="info"/> is null.</exception>
        public TlsaRecord(ResourceRecordInfo info, byte certificateUsage, byte selector, byte matchingType, byte[] certificateAssociationData)
            : base(info)
        {
            CertificateUsage = certificateUsage;
            Selector = selector;
            MatchingType = matchingType;
            CertificateAssociationData = certificateAssociationData ?? throw new ArgumentNullException(nameof(certificateAssociationData));
            CertificateAssociationDataAsString = string.Join(string.Empty, CertificateAssociationData.Select(b => b.ToString("x2")));
        }

        private protected override string RecordToString()
        {
            return string.Format("{0} {1} {2} {3}", 
                CertificateUsage,
                Selector,
                MatchingType,
                CertificateAssociationDataAsString);
        }
    }
}