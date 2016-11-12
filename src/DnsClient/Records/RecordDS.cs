using System;
using System.Text;
/*
 * http://tools.ietf.org/rfc/rfc3658.txt
 * 
2.4.  Wire Format of the DS record

   The DS (type=43) record contains these fields: key tag, algorithm,
   digest type, and the digest of a public key KEY record that is
   allowed and/or used to sign the child's apex KEY RRset.  Other keys
   MAY sign the child's apex KEY RRset.

                        1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |           key tag             |  algorithm    |  Digest type  |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                digest  (length depends on type)               |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                (SHA-1 digest is 20 bytes)                     |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                                                               |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-|
   |                                                               |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-|
   |                                                               |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

 */

namespace DnsClient.Records
{
    public class RecordDS : Record
    {
        public ushort KeyTag { get; }

        public byte Algorithm { get; }

        public byte DigestType { get; }

        public byte[] Digest { get; }

        public RecordDS(ResourceRecord resource, RecordReader recordReader)
            : base(resource)
        {
            ushort length = recordReader.ReadUInt16(-2);
            KeyTag = recordReader.ReadUInt16();
            Algorithm = recordReader.ReadByte();
            DigestType = recordReader.ReadByte();
            length -= 4;
            Digest = new byte[length];
            Digest = recordReader.ReadBytes(length);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int index = 0; index < Digest.Length; index++)
            {
                sb.AppendFormat("{0:x2}", Digest[index]);
            }

            return string.Format(
                "{0} {1} {2} {3}",
                KeyTag,
                Algorithm,
                DigestType,
                sb.ToString());
        }
    }
}