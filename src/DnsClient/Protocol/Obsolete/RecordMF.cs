using System;
/*
 * 
3.3.5. MF RDATA format (Obsolete)

    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    /                   MADNAME                     /
    /                                               /
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

where:

MADNAME         A <domain-name> which specifies a host which has a mail
                agent for the domain which will accept mail for
                forwarding to the domain.

MF records cause additional section processing which looks up an A type
record corresponding to MADNAME.

MF is obsolete.  See the definition of MX and [RFC-974] for details ofw
the new scheme.  The recommended policy for dealing with MD RRs found in
a master file is to reject them, or to convert them to MX RRs with a
preference of 10. */
namespace DnsClient.Protocol
{
    [Obsolete("by RFC 973")]
    public class RecordMF : Record
    {
        public string DomainName { get; }

        public RecordMF(ResourceRecord resource, RecordReader recordReader)
            : base(resource)
        {
            DomainName = recordReader.ReadDomainName();
        }

        public override string ToString()
        {
            return DomainName;
        }
    }
}