namespace DnsClient.Protocol
{
	public class RecordUID : RDataRecord
    {
        public RecordUID(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}