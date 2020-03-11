using System;
using System.Collections.Generic;
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
    /// A <see cref="DnsResourceRecord"/> represending a DANE TLSA record.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc6698#section-2.1">RFC 6698</seealso>
    public class TlsaRecord : DnsResourceRecord
    {
        /// <summary>
        /// The provided association that will be used to match the certificate presented in the TLS handshake
        /// </summary>
        public TlsaCertificateUsageType CertificateUsage { get; }

        /// <summary>
        /// Specifies which part of the TLS certificate presented by the server will be matched against the association data
        /// </summary>
        public TlsaSelectorType Selector { get; }

        /// <summary>
        /// Specifies how the certificate association is presented
        /// </summary>
        public TlsaMatchingType MatchingType { get; }

        /// <summary>
        /// These bytes are either raw data (that is, the full certificate or its SubjectPublicKeyInfo, depending on the selector)
        /// for matching type 0, or the hash of the raw data for matching types 1 and 2.
        /// The data refers to the certificate in the association, not to the TLS ASN.1 Certificate object.
        /// </summary>
        public IReadOnlyList<byte> CertificateAssociationData { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TlsaRecord"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="certificateUsage">The certificate usage.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="matchingType">The matching type.</param>
        /// <param name="certificateAssociationData">The certificate association data.</param>
        /// <exception cref="System.ArgumentNullException">If <paramref name="certificateAssociationData"/> or <paramref name="info"/> is null.</exception>
        public TlsaRecord(ResourceRecordInfo info, TlsaCertificateUsageType certificateUsage, TlsaSelectorType selector,
            TlsaMatchingType matchingType, IReadOnlyList<byte> certificateAssociationData)
            : base(info)
        {
            CertificateUsage = certificateUsage;
            Selector = selector;
            MatchingType = matchingType;
            CertificateAssociationData = certificateAssociationData ?? throw new ArgumentNullException(nameof(certificateAssociationData));
        }

        private protected override string RecordToString()
        {
            return string.Format("{0} {1} {2} {3}", (byte)CertificateUsage, (byte)Selector, (byte)MatchingType,
                string.Concat(CertificateAssociationData.Select(b => b.ToString("X2"))));
        }
    }

    /// <summary>
    /// The provided association that will be used to match the certificate presented in the TLS handshake
    /// </summary>
    /// <seealso href="https://www.iana.org/assignments/dane-parameters/dane-parameters.xhtml#certificate-usages"/>
    public enum TlsaCertificateUsageType : byte
    {
        /// <summary>
        /// [PKIX-TA]
        /// The TLS certificate presented by server must pass PKIX path validation with a trusted CA
        /// The chain of trust must contain the CA certificate specified in the record
        /// </summary>
        CaConstraint,

        /// <summary>
        /// [PKIX-EE]
        /// The TLS certificate presented by server must pass PKIX path validation with a trusted CA
        /// This certificate must match the end-entity certificate specified in the record
        /// </summary>
        ServiceCertificateConstraint,

        /// <summary>
        /// [DANE-TA]
        /// The TLS certificate presented by server must have the CA certificate specified in the record as trust anchor
        /// </summary>
        TrustAnchorAssertion,

        /// <summary>
        /// [DANE-EE]
        /// The TLS certificate presented by server must match the end-entity certificate specified in the record
        /// </summary>
        DomainIssuedCertificate
    }

    /// <summary>
    /// Specifies which part of the TLS certificate presented by the server will be matched against the association data
    /// </summary>
    /// <seealso href="https://www.iana.org/assignments/dane-parameters/dane-parameters.xhtml#selectors">IANA registry</seealso>
    public enum TlsaSelectorType : byte
    {
        /// <summary>
        /// [Cert]
        /// The Certificate binary structure as defined in RFC5280
        /// </summary>
        FullCertificate,

        /// <summary>
        /// [SPKI]
        /// DER-encoded binary structure as defined in RFC5280
        /// </summary>
        SubjectPublicKeyInfo
    }

    /// <summary>
    /// Specifies how the certificate association is presented
    /// </summary>
    /// <seealso href="https://www.iana.org/assignments/dane-parameters/dane-parameters.xhtml#matching-types">IANA registry</seealso>
    public enum TlsaMatchingType : byte
    {
        /// <summary>
        /// [Full]
        /// Exact match on selected content
        /// </summary>
        Exact,

        /// <summary>
        /// [SHA2-256]
        /// SHA-256 hash of selected content
        /// </summary>
        Sha265,

        /// <summary>
        /// [SHA2-512]
        /// SHA-512 hash of selected content
        /// </summary>
        Sha512
    }
}