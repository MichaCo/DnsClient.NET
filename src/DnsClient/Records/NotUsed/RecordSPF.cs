namespace DnsClient.Records
{
	public class RecordSPF : RDataRecord
    {
        public RecordSPF(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}