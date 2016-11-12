namespace DnsClient.Records
{
	public class RecordNIMLOC : RDataRecord
    {
        public RecordNIMLOC(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}