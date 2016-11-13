#region Rfc info
/*
 * http://www.ietf.org/rfc/rfc2535.txt
 * 4.1 SIG RDATA Format

   The RDATA portion of a SIG RR is as shown below.  The integrity of
   the RDATA information is protected by the signature field.

                           1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
       0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |        type covered           |  algorithm    |     labels    |
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |                         original TTL                          |
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |                      signature expiration                     |
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |                      signature inception                      |
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |            key  tag           |                               |
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+         signer's name         +
      |                                                               /
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-/
      /                                                               /
      /                            signature                          /
      /                                                               /
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+


*/
#endregion

namespace DnsClient.Protocol
{
    public class RecordSIG : Record
    {
        public ushort TypeCovered { get; }

        public byte Algorithm { get; }

        public byte Labels { get; }

        public uint OriginalTTL { get; }

        public uint SignatureExpiration { get; }

        public uint SignatureInception { get; }

        public ushort KeyTag { get; }

        public string SignersName { get; }

        public string Signature { get; }

        public RecordSIG(ResourceRecord resource, RecordReader recordReader)
            : base(resource)
        {
            TypeCovered = recordReader.ReadUInt16();
            Algorithm = recordReader.ReadByte();
            Labels = recordReader.ReadByte();
            OriginalTTL = recordReader.ReadUInt32();
            SignatureExpiration = recordReader.ReadUInt32();
            SignatureInception = recordReader.ReadUInt32();
            KeyTag = recordReader.ReadUInt16();
            SignersName = recordReader.ReadDomainName();
            Signature = recordReader.ReadString();
        }

        public override string ToString()
        {
            return string.Format(
                "{0} {1} {2} {3} {4} {5} {6} {7} \"{8}\"",
                TypeCovered,
                Algorithm,
                Labels,
                OriginalTTL,
                SignatureExpiration,
                SignatureInception,
                KeyTag,
                SignersName,
                Signature);
        }
    }
}