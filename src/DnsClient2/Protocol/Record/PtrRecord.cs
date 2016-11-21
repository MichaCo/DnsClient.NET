using System;
using System.Linq;

namespace DnsClient2.Protocol.Record
{
    /* RFC 1035 (https://tools.ietf.org/html/rfc1035#section-3.3.12)
    3.3.12. PTR RDATA format

        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        /                   PTRDNAME                    /
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

    PTRDNAME        A <domain-name> which points to some location in the
                domain name space.

    PTR records cause no additional section processing.  These RRs are used
    in special domains to point to some other location in the domain space.
    These records are simple data, and don't imply any special processing
    similar to that performed by CNAME, which identifies aliases.  See the
    description of the IN-ADDR.ARPA domain for an example.
    */
    public class PtrRecord : DnsResourceRecord
    {
        public string PtrDomainName { get; }

        internal PtrRecord(ResourceRecordInfo info, string ptrDName)
            : base(info)
        {
            if (string.IsNullOrWhiteSpace(ptrDName))
            {
                throw new ArgumentNullException(nameof(ptrDName));
            }

            PtrDomainName = ptrDName;
        }

        public override string RecordToString()
        {
            return PtrDomainName;
        }
    }
}