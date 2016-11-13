namespace DnsClient.Protocol
{
	public class RecordIPSECKEY : RDataRecord
    {
        internal RecordIPSECKEY(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}