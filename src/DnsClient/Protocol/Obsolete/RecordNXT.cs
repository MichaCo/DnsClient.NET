using System;
using System.Text;
/*
 * http://tools.ietf.org/rfc/rfc2065.txt
 * 
5.2 NXT RDATA Format

   The RDATA for an NXT RR consists simply of a domain name followed by
   a bit map.

   The type number for the NXT RR is 30.

                           1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
       0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |         next domain name                                      /
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |                    type bit map                               /
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

   The NXT RR type bit map is one bit per RR type present for the owner
   name similar to the WKS socket bit map.  The first bit represents RR
   type zero (an illegal type which should not be present.) A one bit
   indicates that at least one RR of that type is present for the owner
   name.  A zero indicates that no such RR is present.  All bits not
   specified because they are beyond the end of the bit map are assumed
   to be zero.  Note that bit 30, for NXT, will always be on so the
   minimum bit map length is actually four octets.  The NXT bit map
   should be printed as a list of RR type mnemonics or decimal numbers
   similar to the WKS RR.

   The domain name may be compressed with standard DNS name compression
   when being transmitted over the network.  The size of the bit map can
   be inferred from the RDLENGTH and the length of the next domain name.



 */
namespace DnsClient.Protocol
{
    [Obsolete]
    public class RecordNXT : Record
    {
        public string NextDomainName { get; }

        public byte[] Bitmap { get; }

        public RecordNXT(ResourceRecord resource, RecordReader recordReader)
            : base(resource)
        {
            ushort length = recordReader.ReadUInt16(-2);
            NextDomainName = recordReader.ReadDomainName();
            length -= (ushort)recordReader.Position;
            Bitmap = new byte[length];
            Bitmap = recordReader.ReadBytes(length);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int bitNr = 1; bitNr < (Bitmap.Length * 8); bitNr++)
            {
                if (IsSet(bitNr))
                {
                    sb.Append(" " + (TypeValue)bitNr);
                }
            }

            return string.Format("{0}{1}", NextDomainName, sb.ToString());
        }

        private bool IsSet(int bitNr)
        {
            int intByte = (int)(bitNr / 8);
            int intOffset = (bitNr % 8);
            byte b = Bitmap[intByte];
            int intTest = 1 << intOffset;
            if ((b & intTest) == 0)
            {
                return false;
            }

            return true;
        }
    }
}