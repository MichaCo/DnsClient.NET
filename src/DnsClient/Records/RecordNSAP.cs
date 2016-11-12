using System;
using System.Text;
/*
 * http://tools.ietf.org/rfc/rfc1348.txt  
 * http://tools.ietf.org/html/rfc1706
 * 
 *	          |--------------|
              | <-- IDP -->  |
              |--------------|-------------------------------------|
              | AFI |  IDI   |            <-- DSP -->              |
              |-----|--------|-------------------------------------|
              | 47  |  0005  | DFI | AA |Rsvd | RD |Area | ID |Sel |
              |-----|--------|-----|----|-----|----|-----|----|----|
       octets |  1  |   2    |  1  | 3  |  2  | 2  |  2  | 6  | 1  |
              |-----|--------|-----|----|-----|----|-----|----|----|

                    IDP    Initial Domain Part
                    AFI    Authority and Format Identifier
                    IDI    Initial Domain Identifier
                    DSP    Domain Specific Part
                    DFI    DSP Format Identifier
                    AA     Administrative Authority
                    Rsvd   Reserved
                    RD     Routing Domain Identifier
                    Area   Area Identifier
                    ID     System Identifier
                    SEL    NSAP Selector

                  Figure 1: GOSIP Version 2 NSAP structure.


 */

namespace DnsClient.Records
{
    public class RecordNSAP : Record
    {
        public ushort Length { get; }

        public byte[] NSAPAddress { get; }

        public RecordNSAP(ResourceRecord resource, RecordReader recordReader)
            : base(resource)
        {
            Length = recordReader.ReadUInt16();
            NSAPAddress = recordReader.ReadBytes(Length);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} ", Length);
            for (int intI = 0; intI < NSAPAddress.Length; intI++)
            {
                sb.AppendFormat("{0:X00}", NSAPAddress[intI]);
            }

            return sb.ToString();
        }

        public string ToGOSIPV2()
        {
            return string.Format(
                "{0:X}.{1:X}.{2:X}.{3:X}.{4:X}.{5:X}.{6:X}{7:X}.{8:X}",
                NSAPAddress[0],                         // AFI
                NSAPAddress[1] << 8 | NSAPAddress[2],   // IDI
                NSAPAddress[3],                         // DFI
                NSAPAddress[4] << 16 | NSAPAddress[5] << 8 | NSAPAddress[6], // AA
                NSAPAddress[7] << 8 | NSAPAddress[8],   // Rsvd
                NSAPAddress[9] << 8 | NSAPAddress[10],// RD
                NSAPAddress[11] << 8 | NSAPAddress[12],// Area
                NSAPAddress[13] << 16 | NSAPAddress[14] << 8 | NSAPAddress[15], // ID-High
                NSAPAddress[16] << 16 | NSAPAddress[17] << 8 | NSAPAddress[18], // ID-Low
                NSAPAddress[19]);
        }
    }
}