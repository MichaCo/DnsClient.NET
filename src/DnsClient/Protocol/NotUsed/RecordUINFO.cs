namespace DnsClient.Protocol
{
	public class RecordUINFO : RDataRecord
    {
        public RecordUINFO(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}