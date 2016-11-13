using System;
/*
3.3.10. NULL RDATA format (EXPERIMENTAL)

    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    /                  <anything>                   /
    /                                               /
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

Anything at all may be in the RDATA field so long as it is 65535 octets
or less.

NULL records cause no additional section processing.  NULL RRs are not
allowed in master files.  NULLs are used as placeholders in some
experimental extensions of the DNS.
*/
namespace DnsClient.Protocol
{
    public class RecordNULL : Record
    {
        public byte[] Anything { get; }

        public RecordNULL(ResourceRecord resource, RecordReader recordReader)
            : base(resource)
        {
            recordReader.Position -= 2;
            // re-read length
            ushort len = recordReader.ReadUInt16();
            Anything = new byte[len];
            Anything = recordReader.ReadBytes(len);
        }

        public override string ToString()
        {
            return string.Format("...binary data... ({0}) bytes", Anything.Length);
        }
    }
}