/*
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
namespace DnsClient.Protocol
{
    public class RecordMG : Record
    {
        public string DomainName { get; }

        public RecordMG(ResourceRecord resource, RecordReader recordReader)
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