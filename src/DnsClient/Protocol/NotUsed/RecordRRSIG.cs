namespace DnsClient.Protocol
{
	public class RecordRRSIG : RDataRecord
    {
        public RecordRRSIG(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}