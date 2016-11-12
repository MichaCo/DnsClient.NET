namespace DnsClient.Records
{
	public class RecordEID : RDataRecord
    {
        public RecordEID(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}