namespace DnsClient.Protocol
{
	public class RecordUnknown : RDataRecord
    {
        internal RecordUnknown(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("Unkown record.");
        }
    }
}