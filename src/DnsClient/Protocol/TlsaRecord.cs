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

        where:

        CERTIFICATEU SAGE: octet that specifies how to verify the certificate

        SELECTOR: octet that specifies which part of the certificate should be checked

        MATCHING TYPE: octet that specifies how the certificate association is presented

        CERTIFICATE ASSOCIATION DATA: string of hexadecimal characters that specifies the
        actual data to be matched given the settings of the other fields

    Example:  _443._tcp.mailbox.org. TLSA
    */

    /// <summary>
    /// a <see cref="DnsResourceRecord"/> representing a TLSA record
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc7671"/>
    /// <seealso href="https://en.wikipedia.org/wiki/DNS-based_Authentication_of_Named_Entities#TLSA_RR"/>
    public class TlsaRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets the <see cref="TlsaCertificateUsage"/>
        /// </summary>
        public TlsaCertificateUsage CertificateUsage { get; }

        /// <summary>
        /// Gets the <see cref="TlsaCertificateUsage"/>
        /// </summary>
        public TlsaSelector Selector { get; }

        /// <summary>
        /// Gets the <see cref="TlsaMatchingType"/>
        /// </summary>
        public TlsaMatchingType MatchingType { get; }

        /// <summary>
        /// Gets string that specifies the actual data to be matched given the settings of the other fields
        /// </summary>
        public IReadOnlyList<byte> CertificateAssociationData { get; }

        /// <summary>
        /// Gets the string representation of the <see cref="CertificateAssociationData"/> in hexadecimal.
        /// </summary>
        public string CertificateAssociationDataAsString { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TlsaRecord"/> class
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

        /// <summary>
        /// Returns same values as dig
        /// </summary>
        /// <returns></returns>
        private protected override string RecordToString()
        {
            return string.Format("{0} {1} {2} {3}", CertificateUsage, Selector, MatchingType, CertificateAssociationDataAsString);
        }
    }

    /// <summary>
    /// Gets octet that specifies how to verify the certificate
    /// </summary>
    public enum TlsaCertificateUsage : byte
    {
        /// <summary>
        /// RR points to a trust anchor and PKIX validation is required (PKIX-TA)
        /// </summary>
        PKIXTA = 0,

        /// <summary>
        /// RR points to an end entity certificate, PKIX validation is required (PKIX-EE)
        /// </summary>
        PKIXEE = 1,

        /// <summary>
        /// RR points to a trust anchor, but PKIX validation is NOT required (DANE-TA)
        /// </summary>
        DANETA = 2,

        /// <summary>
        /// RR points to an end entity certificate, but PKIX validation is NOT required (DANE-EE)
        /// </summary>
        DANEEE = 3
    }

    /// <summary>
    /// Gets octet that specifies which part of the certificate should be checked
    /// </summary>
    public enum TlsaSelector : byte
    {
        /// <summary>
        /// Select the entire certificate for matching
        /// </summary>
        FullCertificate = 0,

        /// <summary>
        /// Select the public key for certificate matching
        /// </summary>
        PublicKey = 1
    }

    /// <summary>
    /// Gets octet that specifies how the certificate association is presented
    /// </summary>
    public enum TlsaMatchingType : byte
    {
        /// <summary>
        /// Value of 0: Exact match on selected content
        /// </summary>
        ExactMatch = 0,

        /// <summary>
        /// Value of 1: SHA-256 hash of selected content
        /// </summary>
        SHA256 = 1,

        /// <summary>
        /// Value of 2: SHA-512 hash of selected content
        /// </summary>
        SHA512 = 2,
    }
}