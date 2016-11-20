using System;

namespace DnsClient2.Protocol
{
    public abstract class ResourceRecord : ResourceRecordInfo
    {
        public ResourceRecord(ResourceRecordInfo info)
            : base(info.QueryName, info.RecordType, info.RecordClass, info.TimeToLive, info.Data)
        {
        }

        /// </inheritdocs>
        public override string ToString()
        {
            return ToString(0);
        }

        public virtual string ToString(int offset =0)
        {
            return string.Format("{0," + offset + "} {1} \t{2} \t{3} \t{4}",
                QueryName,
                TimeToLive,
                RecordClass,
                RecordType,
                RecordToString());
        }

        public abstract string RecordToString();
    }

    public class ResourceRecordInfo
    {
        /// <summary>
        /// The query name.
        /// </summary>
        public DnsName QueryName { get; }

        /// <summary>
        /// Specifies type of resource record.
        /// </summary>
        public ushort RecordType { get; }

        /// <summary>
        /// Specifies type class of resource record, mostly IN but can be CS, CH or HS .
        /// </summary>
        public ushort RecordClass { get; }

        /// <summary>
        /// Raw record rdata.
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        /// The TTL value for the record set by the server.
        /// </summary>
        public uint TimeToLive { get; }

        public ResourceRecordInfo(DnsName queryName, ushort recordType, ushort recordClass, uint ttl, byte[] data)
        {
            if (queryName == null)
            {
                throw new ArgumentNullException(nameof(queryName));
            }
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            QueryName = queryName;
            RecordType = recordType;
            RecordClass = recordClass;
            TimeToLive = ttl;
            Data = data;
        }
    }
}