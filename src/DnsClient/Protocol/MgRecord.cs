using System;

namespace DnsClient.Protocol
{
    /*
    https://tools.ietf.org/html/rfc1035#section-3.3.6:
    3.3.6. MG RDATA format (EXPERIMENTAL)

        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        /                   MGMNAME                     /
        /                                               /
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

    MGMNAME         A <domain-name> which specifies a mailbox which is a
                    member of the mail group specified by the domain name.

    MG records cause no additional section processing.


     */

    public class MgRecord : DnsResourceRecord
    {
        public DnsString MgName { get; }

        public MgRecord(ResourceRecordInfo info, DnsString name)
            : base(info)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            MgName = name;
        }

        public override string RecordToString()
        {
            return MgName.Value;
        }
    }
}