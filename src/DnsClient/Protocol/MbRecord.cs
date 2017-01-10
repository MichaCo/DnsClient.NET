using System;

namespace DnsClient.Protocol
{
    /*
    https://tools.ietf.org/html/rfc1035#section-3.3.3:
    3.3.3. MB RDATA format (EXPERIMENTAL)

        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        /                   MADNAME                     /
        /                                               /
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

    MADNAME         A <domain-name> which specifies a host which has the
                    specified mailbox.

     */

    public class MbRecord : DnsResourceRecord
    {
        public DnsName MadName { get; }

        public MbRecord(ResourceRecordInfo info, DnsName name)
            : base(info)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            MadName = name;
        }

        public override string RecordToString()
        {
            return MadName;
        }
    }
}