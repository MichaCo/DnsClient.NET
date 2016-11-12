namespace DnsClient.Records
{
	public class RecordA6 : RDataRecord
	{
		public RecordA6(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
		}

		public override string ToString()
		{
			return string.Format("not-used");
		}
	}
}