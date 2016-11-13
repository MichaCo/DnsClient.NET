/*
 3.3.2. HINFO RDATA format

    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    /                      CPU                      /
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    /                       OS                      /
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

where:

CPU             A <character-string> which specifies the CPU type.

OS              A <character-string> which specifies the operating
                system type.

Standard values for CPU and OS can be found in [RFC-1010].

HINFO records are used to acquire general information about a host.  The
main use is for protocols such as FTP that can use special procedures
when talking between machines or operating systems of the same type.
 */

namespace DnsClient.Protocol
{
    public class RecordHINFO : Record
    {
        public string CPU { get; }
        public string OS { get; }

        public RecordHINFO(ResourceRecord resource, RecordReader recordReader)
            : base(resource)
        {
            CPU = recordReader.ReadString();
            OS = recordReader.ReadString();
        }

        public override string ToString()
        {
            return string.Format("CPU={0} OS={1}", CPU, OS);
        }
    }
}