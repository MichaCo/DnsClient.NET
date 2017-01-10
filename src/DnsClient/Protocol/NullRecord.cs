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
    public class NullRecord : DnsResourceRecord
    {
        public byte[] Anything { get; }

        public NullRecord(ResourceRecordInfo info, byte[] anything)
            : base(info)
        {
            Anything = anything;
        }

        public override string RecordToString()
        {
            return $"byte[{Anything.Length}]";
        }
    }
}