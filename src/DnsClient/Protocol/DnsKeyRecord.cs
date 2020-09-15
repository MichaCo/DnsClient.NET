using System;
using System.Collections.Generic;
using System.Linq;

namespace DnsClient.Protocol
{
    /* https://tools.ietf.org/html/rfc4034#section-2
        2.1.  DNSKEY RDATA Wire Format

           The RDATA for a DNSKEY RR consists of a 2 octet Flags Field, a 1
           octet Protocol Field, a 1 octet Algorithm Field, and the Public Key
           Field.

                                1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
            0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
           |              Flags            |    Protocol   |   Algorithm   |
           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
           /                                                               /
           /                            Public Key                         /
           /                                                               /
           +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

        Flag: Bit 7 of the Flags field is the Zone Key flag.  If bit 7 has value 1,
        then the DNSKEY record holds a DNS zone key, and the DNSKEY RR's
        owner name MUST be the name of a zone.  If bit 7 has value 0, then
        the DNSKEY record holds some other type of DNS public key and MUST
        NOT be used to verify RRSIGs that cover RRsets.

        Protocol: The Protocol Field MUST have value 3, and the DNSKEY RR MUST be
        treated as invalid during signature verification if it is found to be
        some value other than 3.

        Algorithm: The Algorithm field identifies the public key's cryptographic
        algorithm and determines the format of the Public Key field.

        Public Key: The Public Key Field holds the public key material. The format
        depends on the algorithm of the key being stored and is described in
        separate documents.
    */

    /// <summary>
    /// a <see cref="DnsResourceRecord"/> representing a DnsKey record.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc4034#section-2"/>
    public class DnsKeyRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets the DNSKEY's flags value.
        /// </summary>
        public int Flags { get; }

        /// <summary>
        /// Gets the DNSKEY's protocol value.
        /// The value must be 3, and the <see cref="DnsKeyRecord"/> MUST be treated as invalid
        /// during signature verification if it is found to be some value other than 3.
        /// </summary>
        public byte Protocol { get; }

        /// <summary>
        /// Gets the <see cref="PublicKey"/>'s cryptographic algorithm and determines the format of the <see cref="PublicKey"/>.
        /// </summary>
        public DnsSecurityAlgorithm Algorithm { get; }

        /// <summary>
        /// Gets the public key material.
        /// The format depends on the <see cref="Algorithm"/> of the key being stored.
        /// </summary>
        public IReadOnlyList<byte> PublicKey { get; }

        /// <summary>
        /// Gets the base64 string representation of the <see cref="PublicKey"/>.
        /// </summary>
        public string PublicKeyAsString { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DnsKeyRecord"/> class
        /// </summary>
        /// <param name="info"></param>
        /// <param name="flags"></param>
        /// <param name="protocol"></param>
        /// <param name="algorithm"></param>
        /// <param name="publicKey"></param>
        /// <exception cref="ArgumentNullException">If <paramref name="info"/> or <paramref name="publicKey"/> is null.</exception>
        public DnsKeyRecord(ResourceRecordInfo info, int flags, byte protocol, byte algorithm, byte[] publicKey)
            : base(info)
        {
            Flags = flags;
            Protocol = protocol;
            Algorithm = (DnsSecurityAlgorithm)algorithm;
            PublicKey = publicKey ?? throw new ArgumentNullException(nameof(publicKey));
            PublicKeyAsString = Convert.ToBase64String(publicKey);
        }

        private protected override string RecordToString()
        {
            return string.Format("{0} {1} {2} {3}", Flags, Protocol, Algorithm, PublicKeyAsString);
        }
    }
}