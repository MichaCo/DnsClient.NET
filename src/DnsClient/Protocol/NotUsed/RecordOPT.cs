namespace DnsClient.Protocol
{
	public class RecordOPT : RDataRecord
    {
        internal RecordOPT(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}