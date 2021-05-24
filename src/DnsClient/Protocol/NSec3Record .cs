using System;
using System.Collections.Generic;
using System.Linq;
using DnsClient.Internal;

namespace DnsClient.Protocol
{
    /* https://datatracker.ietf.org/doc/html/rfc5155#section-3.2
    3.2.  NSEC3 RDATA Wire Format

       The RDATA of the NSEC3 RR is as shown below:

                            1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
        0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
       +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
       |   Hash Alg.   |     Flags     |          Iterations           |
       +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
       |  Salt Length  |                     Salt                      /
       +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
       |  Hash Length  |             Next Hashed Owner Name            /
       +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
       /                         Type Bit Maps                         /
       +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

       Hash Algorithm is a single octet.

       Flags field is a single octet, the Opt-Out flag is the least
       significant bit, as shown below:

        0 1 2 3 4 5 6 7
       +-+-+-+-+-+-+-+-+
       |             |O|
       +-+-+-+-+-+-+-+-+

    */

    /// <summary>
    /// a <see cref="DnsResourceRecord"/> representing a NSEC3 record.
    /// </summary>
    /// <seealso href="https://datatracker.ietf.org/doc/html/rfc5155"/>
    public class NSec3Record : DnsResourceRecord
    {
        /// <summary>
        /// Gets the cryptographic hash algorithm used to construct the hash-value.
        /// </summary>
        public byte HashAlgorithm { get; }

        /// <summary>
        /// Gets the flags field value containing 8 one-bit flags that can be used to indicate different processing.
        /// All undefined flags must be zero.
        /// The only flag defined by this specification is the Opt-Out flag.
        /// </summary>
        public byte Flags { get; }

        /// <summary>
        /// Gets the number of additional times the hash function has been performed.
        /// </summary>
        public int Iterations { get; }

        /// <summary>
        /// Gets the salt field which is appended to the original owner name before hashing
        /// in order to defend against pre-calculated dictionary attacks.
        /// </summary>
        public byte[] Salt { get; }

        /// <summary>
        /// Gets the salt field which is appended to the original owner name before hashing
        /// in order to defend against pre-calculated dictionary attacks.
        /// </summary>
        public string SaltAsString { get; }

        /// <summary>
        /// Gets the name of the next hashed owner in hash order.
        /// This value is in binary format.
        /// </summary>
        public byte[] NextOwnersName { get; }

        /// <summary>
        /// Gets the name of the next hashed owner in hash order.
        /// This value is in binary format.
        /// </summary>
        public string NextOwnersNameAsString { get; }

        /// <summary>
        /// Gets the type bit maps field which identifies the RRSet types that exist at the original owner name of the NSEC3 RR.
        /// </summary>
        public IReadOnlyList<byte> TypeBitMapsRaw { get; }

        /// <summary>
        /// Gets the type bit maps field which identifies the RRSet types that exist at the original owner name of the NSEC3 RR.
        /// </summary>
        public IReadOnlyList<ResourceRecordType> TypeBitMaps { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NSec3Record"/> class
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="info"/>, <paramref name="nextOwnersName"/>, <paramref name="salt"/> or <paramref name="bitmap"/> is null.
        /// </exception>
        public NSec3Record(
            ResourceRecordInfo info,
            byte hashAlgorithm,
            byte flags,
            int iterations,
            byte[] salt,
            byte[] nextOwnersName,
            byte[] bitmap)
            : base(info)
        {
            HashAlgorithm = hashAlgorithm;
            Flags = flags;
            Iterations = iterations;
            Salt = salt ?? throw new ArgumentNullException(nameof(salt));
            TypeBitMapsRaw = bitmap ?? throw new ArgumentNullException(nameof(bitmap));

            SaltAsString = Salt.Length == 0 ? "-" : string.Join(string.Empty, Salt.Select(b => b.ToString("X2")));
            NextOwnersName = nextOwnersName ?? throw new ArgumentNullException(nameof(nextOwnersName));

            try
            {
                NextOwnersNameAsString = Base32Hex.ToBase32HexString(nextOwnersName);
            }
            catch
            {
                // Nothing - I'm just not trusting myself
            }

            TypeBitMaps = NSecRecord.ReadBitmap(bitmap).OrderBy(p => p).Select(p => (ResourceRecordType)p).ToArray();
        }

        private protected override string RecordToString()
        {
            return string.Format(
                "{0} {1} {2} {3} {4} {5}",
                HashAlgorithm,
                Flags,
                Iterations,
                SaltAsString,
                NextOwnersNameAsString,
                string.Join(" ", TypeBitMaps));
        }
    }
}
