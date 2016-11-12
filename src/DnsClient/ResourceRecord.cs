using System;
using DnsClient.Records;

namespace DnsClient
{
    /*** RFC ***
	3.2. RR definitions

	3.2.1. Format

	All RRs have the same top level format shown below:

										1  1  1  1  1  1
		  0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
		+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
		|                                               |
		/                                               /
		/                      NAME                     /
		|                                               |
		+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
		|                      TYPE                     |
		+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
		|                     CLASS                     |
		+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
		|                      TTL                      |
		|                                               |
		+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
		|                   RDLENGTH                    |
		+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--|
		/                     RDATA                     /
		/                                               /
		+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+


	where:

	NAME            an owner name, i.e., the name of the node to which this
					resource record pertains.

	TYPE            two octets containing one of the RR TYPE codes.

	CLASS           two octets containing one of the RR CLASS codes.

	TTL             a 32 bit signed integer that specifies the time interval
					that the resource record may be cached before the source
					of the information should again be consulted.  Zero
					values are interpreted to mean that the RR can only be
					used for the transaction in progress, and should not be
					cached.  For example, SOA records are always distributed
					with a zero TTL to prohibit caching.  Zero values can
					also be used for extremely volatile data.

	RDLENGTH        an unsigned 16 bit integer that specifies the length in
					octets of the RDATA field.

	RDATA           a variable length string of octets that describes the
					resource.  The format of this information varies
					according to the TYPE and CLASS of the resource record.
	***/

    /// <summary>
    /// Resource Record.
    /// </summary>
    public class ResourceRecord
    {
        private int _timeLived;

        /// <summary>
        /// The name of the node to which this resource record pertains.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Specifies type of resource record.
        /// </summary>
        public TypeValue Type { get; }

        /// <summary>
        /// Specifies type class of resource record, mostly IN but can be CS, CH or HS .
        /// </summary>
        public ClassValue Class { get; }

        /// <summary>
        /// 
        /// </summary>
        public ushort RdLength { get; }

        /// <summary>
        /// One of the Record* classes.
        /// </summary>
        public Record Record { get; }

        /// <summary>
        /// The TTL of the item.
        /// </summary>
        public uint TimeToLive { get; private set; }

        public ResourceRecord(RecordReader recordReader)
        {
            _timeLived = 0;
            Name = recordReader.ReadDomainName();
            Type = (TypeValue)recordReader.ReadUInt16();
            Class = (ClassValue)recordReader.ReadUInt16();
            TimeToLive = recordReader.ReadUInt32();
            RdLength = recordReader.ReadUInt16();
            Record = recordReader.ReadRecord(this, Type, RdLength);
        }

        /// <summary>
        /// Time to live, the time interval that the resource record may be cached
        /// </summary>
        internal uint SetTimeToLive(int timeLived)
        {
            _timeLived = timeLived;
            TimeToLive = (uint)Math.Max(0, TimeToLive - timeLived);
            return TimeToLive;
        }

        /// </inheritdocs>
        public override string ToString()
        {
            return string.Format("{0,-32} {1}\t{2}\t{3}\t{4}",
                Name,
                TimeToLive,
                Class,
                Type,
                Record);
        }
    }
}
