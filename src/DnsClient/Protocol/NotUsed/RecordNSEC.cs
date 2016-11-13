namespace DnsClient.Protocol
{
	public class RecordNSEC : RDataRecord
    {
        internal RecordNSEC(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}