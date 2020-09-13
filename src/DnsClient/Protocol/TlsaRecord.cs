using System;
using System.Collections.Generic;
using System.Linq;

namespace DnsClient.Protocol
{
    /* https://tools.ietf.org/html/rfc6698#Section2.1
      2.1 TLSA Data format

           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
           |  Cert. Usage  |   Selector    | Matching Type |               /
           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+               /
           /                                                               /
           /                 Certificate Association Data                  /
           /                                                               /
           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

        Certificate Usage: A one-octet value, called "certificate usage", specifies the provided
        association that will be used to match the certificate presented in the TLS handshake.

        Selector: A one-octet value, called "selector", specifies which part of the TLS
        certificate presented by the server will be matched against the
        association data.

        Matching Type: A one-octet value, called "matching type", specifies how the
        certificate association is presented.

        Certificate Association Data: This field specifies the "certificate association data" to be
        matched.  These bytes are either raw data (that is, the full
        certificate or its SubjectPublicKeyInfo, depending on the selector)
        for matching type 0, or the hash of the raw data for matching types 1
        and 2.  The data refers to the certificate in the association, not to
        the TLS ASN.1 Certificate object.
    */

    /// <summary>
    /// A <see cref="DnsResourceRecord"/> representing a TLSA record.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc6698#Section2.1"/>
    /// <seealso href="https://tools.ietf.org/html/rfc7671"/>
    /// <seealso href="https://en.wikipedia.org/wiki/DNS-based_Authentication_of_Named_Entities#TLSA_RR"/>
    public class TlsaRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets the <see cref="TlsaCertificateUsage"/>, which specifies the provided association
        /// that will be used to match the certificate presented in the TLS handshake.
        /// </summary>
        public TlsaCertificateUsage CertificateUsage { get; }

        /// <summary>
        /// Gets the <see cref="TlsaSelector"/>, which specifies which part of the TLS certificate
        /// presented by the server will be matched against the <see cref="CertificateAssociationData"/>.
        /// </summary>
        public TlsaSelector Selector { get; }

        /// <summary>
        /// Gets the <see cref="TlsaMatchingType"/>, which specifies how the <see cref="CertificateAssociationData"/> is presented.
        /// </summary>
        public TlsaMatchingType MatchingType { get; }

        /// <summary>
        /// Gets the "certificate association data" to be matched.
        /// </summary>
        public IReadOnlyList<byte> CertificateAssociationData { get; }

        /// <summary>
        /// Gets the string representation of the <see cref="CertificateAssociationData"/> in hexadecimal.
        /// </summary>
        public string CertificateAssociationDataAsString { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TlsaRecord"/> class.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="certificateUsage"></param>
        /// <param name="selector"></param>
        /// <param name="matchingType"></param>
        /// <param name="certificateAssociationData"></param>
        /// <exception cref="ArgumentNullException">If <paramref name="info"/> or <paramref name="certificateAssociationData"/> is null.</exception>
        public TlsaRecord(ResourceRecordInfo info, byte certificateUsage, byte selector, byte matchingType, byte[] certificateAssociationData)
            : base(info)
        {
            CertificateUsage = (TlsaCertificateUsage)certificateUsage;
            Selector = (TlsaSelector)selector;
            MatchingType = (TlsaMatchingType)matchingType;
            CertificateAssociationData = certificateAssociationData ?? throw new ArgumentNullException(nameof(certificateAssociationData));
            CertificateAssociationDataAsString = string.Join(string.Empty, certificateAssociationData.Select(b => b.ToString("X2")));
        }

        private protected override string RecordToString()
        {
            return string.Format("{0} {1} {2} {3}", CertificateUsage, Selector, MatchingType, CertificateAssociationDataAsString);
        }
    }

    /// <summary>
    /// The usage flag specifies the provided association that will be used to match the certificate presented in the TLS handshake.
    /// </summary>
    public enum TlsaCertificateUsage : byte
    {
        /// <summary>
        /// Certificate authority constraint.
        /// </summary>
        PKIXTA = 0,

        /// <summary>
        /// Service certificate constraint.
        /// </summary>
        PKIXEE = 1,

        /// <summary>
        /// Trust Anchor Assertion.
        /// </summary>
        DANETA = 2,

        /// <summary>
        /// Domain issued certificate.
        /// </summary>
        DANEEE = 3
    }

    /// <summary>
    /// Flag which specifies which part of the TLS certificate presented by the server will be matched against the association data.
    /// </summary>
    public enum TlsaSelector : byte
    {
        /// <summary>
        /// Select the entire certificate for matching.
        /// </summary>
        FullCertificate = 0,

        /// <summary>
        /// Select the public key for certificate matching.
        /// </summary>
        PublicKey = 1
    }

    /// <summary>
    /// Flag which specifies how the certificate association is presented.
    /// </summary>
    public enum TlsaMatchingType : byte
    {
        /// <summary>
        /// Exact match, the entire information selected is present in the certificate association data.
        /// </summary>
        ExactMatch = 0,

        /// <summary>
        /// SHA-256 hash of selected content.
        /// </summary>
        SHA256 = 1,

        /// <summary>
        /// SHA-512 hash of selected content.
        /// </summary>
        SHA512 = 2,
    }
}