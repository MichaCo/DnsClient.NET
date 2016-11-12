namespace DnsClient.Records
{
	public class RecordOPT : RDataRecord
    {
        public RecordOPT(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}