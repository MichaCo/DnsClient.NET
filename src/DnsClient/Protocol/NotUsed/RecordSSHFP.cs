namespace DnsClient.Protocol
{
	public class RecordSSHFP : RDataRecord
    {
        internal RecordSSHFP(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}