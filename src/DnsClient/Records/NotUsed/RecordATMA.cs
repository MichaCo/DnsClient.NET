namespace DnsClient.Records
{
	public class RecordATMA : RDataRecord
	{
        public RecordATMA(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}