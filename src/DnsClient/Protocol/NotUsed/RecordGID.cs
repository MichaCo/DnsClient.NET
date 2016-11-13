namespace DnsClient.Protocol
{
    public class RecordGID : RDataRecord
    {
        internal RecordGID(ResourceRecord resource, RecordReader recordReader)
            : base(resource, recordReader)
        {
        }

        public override string ToString()
        {
            return string.Format("not-used");
        }
    }
}