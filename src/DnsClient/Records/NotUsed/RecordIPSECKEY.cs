namespace DnsClient.Records
{
	public class RecordIPSECKEY : RDataRecord
    {
        public RecordIPSECKEY(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}