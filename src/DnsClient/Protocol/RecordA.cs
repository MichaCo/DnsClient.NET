using System;
using System.Net;
/*
3.4.1. A RDATA format

+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
|                    ADDRESS                    |
+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

where:

ADDRESS         A 32 bit Internet address.

Hosts that have multiple Internet addresses will have multiple A
records.
* 
*/
namespace DnsClient.Protocol
{
    public class RecordA : Record
    {
        public IPAddress Address;

        public RecordA(ResourceRecord resource, RecordReader recordReader)
            : base(resource)
        {
            IPAddress.TryParse(string.Format("{0}.{1}.{2}.{3}",
                recordReader.ReadByte(),
                recordReader.ReadByte(),
                recordReader.ReadByte(),
                recordReader.ReadByte()), out Address);
        }

        public override string ToString()
        {
            return Address.ToString();
        }

    }
}
