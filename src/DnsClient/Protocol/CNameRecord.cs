using System;

namespace DnsClient.Protocol
{
    /*
    https://tools.ietf.org/html/rfc1035#section-3.3.1:
    3.3.1. CNAME RDATA format

        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        /                     CNAME                     /
        /                                               /
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

    CNAME           A <domain-name> which specifies the canonical or primary
                    name for the owner.  The owner name is an alias.

    CNAME RRs cause no additional section processing, but name servers may
    choose to restart the query at the canonical name in certain cases.  See
    the description of name server logic in [RFC-1034] for details.

     */

    public class CNameRecord : DnsResourceRecord
    {
        public DnsName CanonicalName { get; }

        public CNameRecord(ResourceRecordInfo info, DnsName name)
            : base(info)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            CanonicalName = name;
        }

        public override string RecordToString()
        {
            return CanonicalName;
        }
    }
}