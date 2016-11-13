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
        public string MadName { get; }

        internal RecordMB(ResourceRecord resource, RecordReader recordReader)
            : base(resource)
        {
            MadName = recordReader.ReadDomainName();
        }

        public override string ToString()
        {
            return MadName;
        }
    }
}