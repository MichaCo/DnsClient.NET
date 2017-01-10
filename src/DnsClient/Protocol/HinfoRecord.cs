using System;
namespace DnsClient.Protocol
{
    /*
    https://tools.ietf.org/html/rfc1035#section-3.3.11:
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

    public class HInfoRecord : DnsResourceRecord
    {
        public string Cpu { get; }

        public string OS { get; }

        public HInfoRecord(ResourceRecordInfo info, string cpu, string os)
            : base(info)
        {
            Cpu = cpu;
            OS = os;
        }

        public override string RecordToString()
        {
            return $"\"{Cpu}\" \"{OS}\"";
        }
    }
}