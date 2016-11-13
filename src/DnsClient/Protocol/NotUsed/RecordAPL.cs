namespace DnsClient.Protocol
{
	public class RecordAPL : RDataRecord
	{
        internal RecordAPL(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}