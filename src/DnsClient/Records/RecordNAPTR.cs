/*
 * http://www.faqs.org/rfcs/rfc2915.html
 * 
 8. DNS Packet Format

         The packet format for the NAPTR record is:

                                          1  1  1  1  1  1
            0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
          +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
          |                     ORDER                     |
          +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
          |                   PREFERENCE                  |
          +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
          /                     FLAGS                     /
          +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
          /                   SERVICES                    /
          +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
          /                    REGEXP                     /
          +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
          /                  REPLACEMENT                  /
          /                                               /
          +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

   FLAGS A <character-string> which contains various flags.

   SERVICES A <character-string> which contains protocol and service
      identifiers.

   REGEXP A <character-string> which contains a regular expression.

   REPLACEMENT A <domain-name> which specifies the new value in the
      case where the regular expression is a simple replacement
      operation.

   <character-string> and <domain-name> as used here are defined in
   RFC1035 [1].

 */

namespace DnsClient.Records
{
    public class RecordNAPTR : Record
    {
        public ushort Order { get; }

        public ushort Preferences { get; }

        public string Flags { get; }

        public string Services { get; }

        public string RegularExpression { get; }

        public string Replacement { get; }

        public RecordNAPTR(ResourceRecord resource, RecordReader recordReader)
            : base(resource)
        {
            Order = recordReader.ReadUInt16();
            Preferences = recordReader.ReadUInt16();
            Flags = recordReader.ReadString();
            Services = recordReader.ReadString();
            RegularExpression = recordReader.ReadString();
            Replacement = recordReader.ReadDomainName();
        }

        public override string ToString()
        {
            return string.Format(
                "{0} {1} \"{2}\" \"{3}\" \"{4}\" {5}",
                Order,
                Preferences,
                Flags,
                Services,
                RegularExpression,
                Replacement);
        }
    }
}