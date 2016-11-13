#region Rfc info
/* http://www.ietf.org/rfc/rfc2535.txt
 * 
3.1 KEY RDATA format

   The RDATA for a KEY RR consists of flags, a protocol octet, the
   algorithm number octet, and the public key itself.  The format is as
   follows:
                        1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |             flags             |    protocol   |   algorithm   |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                                                               /
   /                          public key                           /
   /                                                               /
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-|

   The KEY RR is not intended for storage of certificates and a separate
   certificate RR has been developed for that purpose, defined in [RFC
   2538].

   The meaning of the KEY RR owner name, flags, and protocol octet are
   described in Sections 3.1.1 through 3.1.5 below.  The flags and
   algorithm must be examined before any data following the algorithm
   octet as they control the existence and format of any following data.
   The algorithm and public key fields are described in Section 3.2.
   The format of the public key is algorithm dependent.

   KEY RRs do not specify their validity period but their authenticating
   SIG RR(s) do as described in Section 4 below.

*/
#endregion

namespace DnsClient.Protocol
{
    public class RecordKEY : Record
    {
        public ushort Flags { get; }

        public byte Protocol { get; }

        public byte Algorithm { get; }

        public string PublicKey { get; }

        public RecordKEY(ResourceRecord resource, RecordReader recordReader)
            : base(resource)
        {
            Flags = recordReader.ReadUInt16();
            Protocol = recordReader.ReadByte();
            Algorithm = recordReader.ReadByte();
            PublicKey = recordReader.ReadString();
        }

        public override string ToString()
        {
            return string.Format(
                "{0} {1} {2} \"{3}\"",
                Flags,
                Protocol,
                Algorithm,
                PublicKey);
        }
    }
}