using System;
using System.Collections.Generic;
using System.Text;

namespace DnsClient.Protocol
{
    /*
    https://tools.ietf.org/html/rfc1035#section-3.3.10:
    3.3.10. NULL RDATA format (EXPERIMENTAL)

        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        /                  <anything>                   /
        /                                               /
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    Anything at all may be in the RDATA field so long as it is 65535 octets
    or less.
     */

    /// <summary>
    /// Experimental RR, not sure if the implementation is actually correct either (not tested).
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.10">RFC 1035</seealso>
    public class UnknownRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets any data stored in this record.
        /// </summary>
        /// <value>
        /// The byte array.
        /// </value>
        public IReadOnlyList<byte> Data { get; }

        /// <summary>
        /// Gets the unknown bytes as Base64 string.
        /// </summary>
        public string DataAsString { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownRecord" /> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="data">The raw data.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="info"/> or <paramref name="data"/> is null.</exception>
        public UnknownRecord(ResourceRecordInfo info, byte[] data)
            : base(info)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            DataAsString = Convert.ToBase64String(data);
        }

        private protected override string RecordToString()
        {
            return DataAsString;
        }
    }
}
