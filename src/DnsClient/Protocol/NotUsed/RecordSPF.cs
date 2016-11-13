namespace DnsClient.Protocol
{
	public class RecordSPF : RDataRecord
    {
        internal RecordSPF(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}