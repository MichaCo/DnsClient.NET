using System;
/*
 * http://www.ietf.org/rfc/rfc2845.txt
 * 
 * Field Name       Data Type      Notes
      --------------------------------------------------------------
      Algorithm Name   domain-name    Name of the algorithm
                                      in domain name syntax.
      Time Signed      u_int48_t      seconds since 1-Jan-70 UTC.
      Fudge            u_int16_t      seconds of error permitted
                                      in Time Signed.
      MAC Size         u_int16_t      number of octets in MAC.
      MAC              octet stream   defined by Algorithm Name.
      Original ID      u_int16_t      original message ID
      Error            u_int16_t      expanded RCODE covering
                                      TSIG processing.
      Other Len        u_int16_t      length, in octets, of
                                      Other Data.
      Other Data       octet stream   empty unless Error == BADTIME

 */

namespace DnsClient.Records
{
    public class RecordTSIG : Record
    {
        public string AlgorithmName { get; }

        public long TimeSigned { get; }

        public ushort Fudge { get; }

        public ushort MacSize { get; }

        public byte[] MAC { get; }

        public ushort OriginalId { get; }

        public ushort Error { get; }

        public ushort OtherLength { get; }

        public byte[] OtherData { get; }

        public RecordTSIG(ResourceRecord resource, RecordReader recordReader)
            : base(resource)
        {
            AlgorithmName = recordReader.ReadDomainName();
            TimeSigned = recordReader.ReadUInt32() << 32 | recordReader.ReadUInt32();
            Fudge = recordReader.ReadUInt16();
            MacSize = recordReader.ReadUInt16();
            MAC = recordReader.ReadBytes(MacSize);
            OriginalId = recordReader.ReadUInt16();
            Error = recordReader.ReadUInt16();
            OtherLength = recordReader.ReadUInt16();
            OtherData = recordReader.ReadBytes(OtherLength);
        }

        public override string ToString()
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dateTime = dateTime.AddSeconds(TimeSigned);
            string printDate = dateTime.ToString() + " " + dateTime.ToString();

            return string.Format(
                "{0} {1} {2} {3} {4}",
                AlgorithmName,
                printDate,
                Fudge,
                OriginalId,
                Error);
        }
    }
}