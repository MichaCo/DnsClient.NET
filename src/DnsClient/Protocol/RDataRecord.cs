namespace DnsClient.Protocol
{
	public abstract class RDataRecord : Record
	{
		public byte[] RData { get; }

        internal RDataRecord(ResourceRecord resource, RecordReader recordReader)
            : base(resource)
        {
			// re-read length
			ushort length = recordReader.ReadUInt16(-2);
			RData = recordReader.ReadBytes(length);
		}

		public override string ToString()
		{
			return string.Format("not-used");
		}
	}
}