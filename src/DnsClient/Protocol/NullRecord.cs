using System;
namespace DnsClient.Protocol
{
    /*
    https://tools.ietf.org/html/rfc1035#section-3.3.8:
    3.3.8. MR RDATA format (EXPERIMENTAL)

        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        /                   NEWNAME                     /
        /                                               /
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

    NEWNAME         A <domain-name> which specifies a mailbox which is the
                    proper rename of the specified mailbox.

    MR records cause no additional section processing.  The main use for MR
    is as a forwarding entry for a user who has moved to a different
    mailbox.
     */

    /// <summary>
    /// Experimental RR, not sure if the implementation is actually correct either (not tested).
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.8">RFC 1035</seealso>
    public class NullRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets any data stored in this record.
        /// </summary>
        /// <value>
        /// The byte array.
        /// </value>
        public byte[] Anything { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NullRecord" /> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="anything">Anything.</param>
        /// <exception cref="System.ArgumentNullException">If <paramref name="info"/> or <paramref name="anything"/> is null.</exception>
        public NullRecord(ResourceRecordInfo info, byte[] anything)
            : base(info)
        {
            Anything = anything ?? throw new ArgumentNullException(nameof(anything));
        }

        /// <inheritdoc />
        public override string RecordToString()
        {
            return $"byte[{Anything.Length}]";
        }
    }
}