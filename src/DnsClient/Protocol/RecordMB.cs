/*
3.3.3. MB RDATA format (EXPERIMENTAL)

    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    /                   MADNAME                     /
    /                                               /
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

where:

MADNAME         A <domain-name> which specifies a host which has the
                specified mailbox.

MB records cause additional section processing which looks up an A type
RRs corresponding to MADNAME.
*/
namespace DnsClient.Protocol
{
    public class RecordMB : Record
    {
        public string DomainName { get; }

        public RecordMB(ResourceRecord resource, RecordReader recordReader)
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