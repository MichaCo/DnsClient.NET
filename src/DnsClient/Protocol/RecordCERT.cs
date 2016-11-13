using System;
/*

 CERT RR
 *                     1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
   0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |             type              |             key tag           |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |   algorithm   |                                               /
   +---------------+            certificate or CRL                 /
   /                                                               /
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-|
 */

namespace DnsClient.Protocol
{
    public class RecordCERT : Record
    {
        public ushort Type { get; }

        public ushort KeyTag { get; }  //Format

        public byte Algorithm { get; }

        public string PublicKey { get; }

        public byte[] RawKey { get; }

        public RecordCERT(ResourceRecord resource, RecordReader recordReader)
            : base(resource)
        {
            // re-read length
            ushort length = recordReader.ReadUInt16(-2);
            Type = recordReader.ReadUInt16();
            KeyTag = recordReader.ReadUInt16();
            Algorithm = recordReader.ReadByte();
            var len = length - 5;
            RawKey = recordReader.ReadBytes(len);
            PublicKey = Convert.ToBase64String(RawKey);
        }

        public override string ToString()
        {
            return PublicKey;
        }
    }
}