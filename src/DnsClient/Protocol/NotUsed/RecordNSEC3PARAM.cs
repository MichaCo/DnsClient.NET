namespace DnsClient.Protocol
{
	public class  RecordNSEC3PARAM : RDataRecord
    {
        public RecordNSEC3PARAM(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}