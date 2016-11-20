namespace DnsClient2.Protocol.Record
{
    public class EmptyRecord : DnsResourceRecord
    {
        public EmptyRecord(ResourceRecordInfo info) : base(info)
        {
        }

        public override string RecordToString()
        {
            return string.Empty;
        }
    }
}