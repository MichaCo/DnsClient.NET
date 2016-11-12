using System;

namespace DnsClient.Records
{
    /*
	3.3.9. MX RDATA format

		+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
		|                  PREFERENCE                   |
		+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
		/                   EXCHANGE                    /
		/                                               /
		+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

	where:

	PREFERENCE      A 16 bit integer which specifies the preference given to
					this RR among others at the same owner.  Lower values
					are preferred.

	EXCHANGE        A <domain-name> which specifies a host willing to act as
					a mail exchange for the owner name.

	MX records cause type A additional section processing for the host
	specified by EXCHANGE.  The use of MX RRs is explained in detail in
	[RFC-974].
	*/

    public class RecordMX : Record, IComparable
    {
        public ushort Preference { get; }

        public string Exchange { get; }

        public RecordMX(ResourceRecord resource, RecordReader recordReader)
            : base(resource)
        {
            Preference = recordReader.ReadUInt16();
            Exchange = recordReader.ReadDomainName();
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Preference, Exchange);
        }

        public int CompareTo(object objA)
        {
            RecordMX recordMX = objA as RecordMX;
            if (recordMX == null)
            {
                return -1;
            }
            else if (Preference > recordMX.Preference)
            {
                return 1;
            }
            else if (Preference < recordMX.Preference)
            {
                return -1;
            }

            // they are the same, now compare case insensitive names
            return string.Compare(Exchange, recordMX.Exchange, true);
        }
    }
}