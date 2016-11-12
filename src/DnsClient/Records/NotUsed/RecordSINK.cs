namespace DnsClient.Records
{
	public class RecordSINK : RDataRecord
    {
        public RecordSINK(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}