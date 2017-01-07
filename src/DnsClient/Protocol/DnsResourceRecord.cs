using System;

namespace DnsClient.Protocol
{
    public abstract class DnsResourceRecord : ResourceRecordInfo
    {
        internal DnsResourceRecord(ResourceRecordInfo info)
            : base(info.DomainName, info.RecordType, info.RecordClass, info.TimeToLive, info.RawDataLength)
        {
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return ToString(0);
        }
        
        /// <summary>
        /// Same as <c>ToString</c> but offsets the <see cref="ResourceRecordInfo.DomainName"/> 
        /// by <paramref name="offset"/>.
        /// Set the offset to -32 for example to make it print nicely in consols.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns>A string representing this instance.</returns>
        public virtual string ToString(int offset = 0)
        {
            return string.Format("{0," + offset + "}{1} \t{2} \t{3} \t{4}",
                DomainName,
                TimeToLive,
                RecordClass,
                RecordType,
                RecordToString());
        }

        /// <summary>
        /// Returns the actual record's value only and not the full object representation.
        /// <see cref="ToString(int)"/> uses this to compose the full string value of this instance.
        /// </summary>
        /// <returns>A string representing this record.</returns>
        public abstract string RecordToString();
    }

    public class ResourceRecordInfo
    {
        /// <summary>
        /// The domain name used to query.
        /// </summary>
        public QueryName DomainName { get; }

        /// <summary>
        /// Specifies type of resource record.
        /// </summary>
        public ResourceRecordType RecordType { get; }

        /// <summary>
        /// Specifies type class of resource record, mostly IN but can be CS, CH or HS .
        /// </summary>
        public QueryClass RecordClass { get; }

        /// <summary>
        /// The TTL value for the record set by the server.
        /// </summary>
        public int TimeToLive { get; protected set; }

        /// <summary>
        /// Gets the number of bytes for this resource record stored in RDATA
        /// </summary>
        public int RawDataLength { get; }

        public ResourceRecordInfo(QueryName queryName, ResourceRecordType recordType, QueryClass recordClass, int ttl, int length)
        {
            if (queryName == null)
            {
                throw new ArgumentNullException(nameof(queryName));
            }

            DomainName = queryName;
            RecordType = recordType;
            RecordClass = recordClass;
            TimeToLive = ttl;
            RawDataLength = length;
        }
    }
}