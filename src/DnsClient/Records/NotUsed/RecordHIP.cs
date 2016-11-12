namespace DnsClient.Records
{
	public class RecordHIP : RDataRecord
    {
        public RecordHIP(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}