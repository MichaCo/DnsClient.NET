using System;
using System.Linq;

namespace DnsClient.Protocol
{
    /* https://datatracker.ietf.org/doc/html/rfc5155#section-4.2
    NSEC3PARAM RDATA Wire Format

       The RDATA of the NSEC3PARAM RR is as shown below:

                            1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
        0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
       +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
       |   Hash Alg.   |     Flags     |          Iterations           |
       +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
       |  Salt Length  |                     Salt                      /
       +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

       Hash Algorithm is a single octet.

       Flags field is a single octet.

       Iterations is represented as a 16-bit unsigned integer, with the most
       significant bit first.

       Salt Length is represented as an unsigned octet.  Salt Length
       represents the length of the following Salt field in octets.  If the
       value is zero, the Salt field is omitted.

       Salt, if present, is encoded as a sequence of binary octets.  The
       length of this field is determined by the preceding Salt Length
       field.
    */

    /// <summary>
    /// a <see cref="DnsResourceRecord"/> representing a NSEC3PARAM record.
    /// </summary>
    /// <seealso href="https://datatracker.ietf.org/doc/html/rfc5155#section-4"/>
    /// <see cref="ResourceRecordType.NSEC3PARAM"/>
    public class NSec3ParamRecord : DnsResourceRecord
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
        /// Initializes a new instance of the <see cref="NSec3ParamRecord"/> class
        /// </summary>
        /// <exception cref="ArgumentNullException">If <paramref name="info"/> or <paramref name="salt"/> is null.</exception>
        public NSec3ParamRecord(
            ResourceRecordInfo info,
            byte hashAlgorithm,
            byte flags,
            int iterations,
            byte[] salt)
            : base(info)
        {
            HashAlgorithm = hashAlgorithm;
            Flags = flags;
            Iterations = iterations;
            Salt = salt ?? throw new ArgumentNullException(nameof(salt));
            SaltAsString = Salt.Length == 0 ? "-" : string.Join(string.Empty, Salt.Select(b => b.ToString("X2")));
        }

        private protected override string RecordToString()
        {
            return string.Format(
                "{0} {1} {2} {3}",
                HashAlgorithm,
                Flags,
                Iterations,
                SaltAsString);
        }
    }
}
