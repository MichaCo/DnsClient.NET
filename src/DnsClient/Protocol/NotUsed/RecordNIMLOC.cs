namespace DnsClient.Protocol
{
	public class RecordNIMLOC : RDataRecord
    {
        internal RecordNIMLOC(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}