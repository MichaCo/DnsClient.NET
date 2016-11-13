namespace DnsClient.Protocol
{
	public class RecordSINK : RDataRecord
    {
        internal RecordSINK(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}