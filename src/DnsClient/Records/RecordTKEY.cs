/*
 * http://tools.ietf.org/rfc/rfc2930.txt
 * 
2. The TKEY Resource Record

   The TKEY resource record (RR) has the structure given below.  Its RR
   type code is 249.

      Field       Type         Comment
      -----       ----         -------
       Algorithm:   domain
       Inception:   u_int32_t
       Expiration:  u_int32_t
       Mode:        u_int16_t
       Error:       u_int16_t
       Key Size:    u_int16_t
       Key Data:    octet-stream
       Other Size:  u_int16_t
       Other Data:  octet-stream  undefined by this specification

 */

namespace DnsClient.Records
{
    public class RecordTKEY : Record
	{
		public string Algorithm { get; }

		public uint Inception { get; }

        public uint Expiration { get; }

        public ushort Mode { get; }

        public ushort Error { get; }

        public ushort KeySize { get; }

        public byte[] KeyData { get; }

        public ushort OtherSize { get; }

        public byte[] OtherData { get; }

        public RecordTKEY(ResourceRecord resource, RecordReader recordReader)
            : base(resource)
        {
			Algorithm = recordReader.ReadDomainName();
			Inception = recordReader.ReadUInt32();
			Expiration = recordReader.ReadUInt32();
			Mode = recordReader.ReadUInt16();
			Error = recordReader.ReadUInt16();
			KeySize = recordReader.ReadUInt16();
			KeyData = recordReader.ReadBytes(KeySize);
			OtherSize = recordReader.ReadUInt16();
			OtherData = recordReader.ReadBytes(OtherSize);
		}

		public override string ToString()
		{
			return string.Format(
                "{0} {1} {2} {3} {4}",
				Algorithm,
				Inception,
				Expiration,
				Mode,
				Error);
		}
	}
}