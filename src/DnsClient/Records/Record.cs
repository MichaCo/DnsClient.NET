using System;

namespace DnsClient.Records
{
    public abstract class Record
    {
        /// <summary>
        /// The Resource Record this RDATA record belongs to
        /// </summary>
        public ResourceRecord ResourceRecord { get; }

        public Record(ResourceRecord resource)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            ResourceRecord = resource;
        }
    }
}
