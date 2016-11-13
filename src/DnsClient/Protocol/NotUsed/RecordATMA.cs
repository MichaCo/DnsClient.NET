namespace DnsClient.Protocol
{
	public class RecordATMA : RDataRecord
	{
        internal RecordATMA(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}