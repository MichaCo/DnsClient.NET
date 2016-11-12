namespace DnsClient.Records
{
	public class RecordUnknown : RDataRecord
    {
        public RecordUnknown(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("Unkown record.");
        }
    }
}