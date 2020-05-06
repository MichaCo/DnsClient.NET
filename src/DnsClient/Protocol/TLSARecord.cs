using System;

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

    */

    /// <summary>
    /// a <see cref="DnsResourceRecord"/> representing a TLSA record
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc6698"/>
    /// <seealso href="https://en.wikipedia.org/wiki/DNS-based_Authentication_of_Named_Entities#TLSA_RR"/>
    [CLSCompliant(false)]
    public class TLSARecord : DnsResourceRecord
    {

        /// <summary>
        /// Gets octet that specifies how to verify the certificate
        /// Value of 0: RR points to a trust anchor and PKIX validation is required
        /// Value of 1: RR points to an end entity certificate, PKIX validation is required
        /// Value of 2: RR points to a trust anchor, but PKIX validation is NOT required
        /// Value of 3: RR points to an end entity certificate, but PKIX validation is NOT required
        /// </summary>
        public byte CertificateUsage { get; }

        /// <summary>
        /// Gets octet that specifies which part of the certificate should be checked
        /// Value of 0: Select the entire certificate for matching
        /// Value of 1: Select the public key for certificate matching
        /// </summary>
        public byte Selector { get; }

        /// <summary>
        /// Gets octet that specifies how the certificate association is presented
        /// Value of 0: Exact match on selected content
        /// Value of 1: SHA-256 hash of selected content
        /// Value of 2: SHA-512 hash of selected content 
        /// </summary>
        public byte MatchingType { get; }

        /// <summary>
        /// Gets string that specifies the actual data to be matched given the settings of the other fields
        /// </summary>
        public string CertificateAssociationData { get; }
        

        /// <summary>
        /// Initializes a new instance of the <see cref="TLSARecord"/> class
        /// </summary>
        /// <param name="info"></param>
        /// <param name="certificateUsage"></param>
        /// <param name="selector"></param>
        /// <param name="matchingType"></param>
        /// <param name="certificateAssociationData"></param>
        public TLSARecord(ResourceRecordInfo info, byte certificateUsage, byte selector, byte matchingType, string certificateAssociationData)
            : base(info)
        {
            CertificateUsage = certificateUsage;
            Selector = selector;
            MatchingType = matchingType;
            CertificateAssociationData = certificateAssociationData;
        }

        /// <summary>
        /// Returns same values as dig
        /// </summary>
        /// <returns></returns>
        private protected override string RecordToString()
        {
            return string.Format("{0} {1} {2} {3}", CertificateUsage, Selector, MatchingType, CertificateAssociationData);
        }
    }
}
