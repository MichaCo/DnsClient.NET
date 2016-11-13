namespace DnsClient.Protocol
{
	public class RecordNSEC3 : RDataRecord
    {
        internal RecordNSEC3(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}