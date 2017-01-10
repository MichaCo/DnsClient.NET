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

    public class MrRecord : DnsResourceRecord
    {
        public DnsName NewName { get; }

        public MrRecord(ResourceRecordInfo info, DnsName name)
            : base(info)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            NewName = name;
        }

        public override string RecordToString()
        {
            return NewName;
        }
    }
}