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
        public string MgmName { get; }

        internal RecordMG(ResourceRecord resource, RecordReader recordReader)
            : base(resource)
        {
            MgmName = recordReader.ReadDomainName();
        }

        public override string ToString()
        {
            return MgmName;
        }
    }
}