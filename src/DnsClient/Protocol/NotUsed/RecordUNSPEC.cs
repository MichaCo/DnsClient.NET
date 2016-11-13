namespace DnsClient.Protocol
{
	public class RecordUNSPEC : RDataRecord
    {
        public RecordUNSPEC(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}