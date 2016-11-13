using System;
/*
 * http://tools.ietf.org/rfc/rfc2230.txt
 * 
 * 3.1 KX RDATA format

   The KX DNS record has the following RDATA format:

    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |                  PREFERENCE                   |
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    /                   EXCHANGER                   /
    /                                               /
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

   where:

   PREFERENCE      A 16 bit non-negative integer which specifies the
                   preference given to this RR among other KX records
                   at the same owner.  Lower values are preferred.

   EXCHANGER       A <domain-name> which specifies a host willing to
                   act as a mail exchange for the owner name.

   KX records MUST cause type A additional section processing for the
   host specified by EXCHANGER.  In the event that the host processing
   the DNS transaction supports IPv6, KX records MUST also cause type
   AAAA additional section processing.

   The KX RDATA field MUST NOT be compressed.

 */
namespace DnsClient.Protocol
{
    public class RecordKX : Record, IComparable
    {
        public ushort Preference { get; }

        public string Exchanger { get; }

        internal RecordKX(ResourceRecord resource, RecordReader recordReader)
            : base(resource)
        {
            Preference = recordReader.ReadUInt16();
            Exchanger = recordReader.ReadDomainName();
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Preference, Exchanger);
        }

        public int CompareTo(object objA)
        {
            RecordKX recordKX = objA as RecordKX;
            if (recordKX == null)
            {
                return -1;
            }
            else if (Preference > recordKX.Preference)
            {
                return 1;
            }
            else if (Preference < recordKX.Preference)
            {
                return -1;
            }

            // they are the same, now compare case insensitive names
            return string.Compare(Exchanger, recordKX.Exchanger, true);
        }
    }
}