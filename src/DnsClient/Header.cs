using System;
using System.Collections.Generic;
using System.Net;

namespace DnsClient
{
    #region RFC specification
    /*
	4.1.1. Header section format

	The header contains the following fields:

										1  1  1  1  1  1
		  0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
		+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
		|                      ID                       |
		+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
		|QR|   Opcode  |AA|TC|RD|RA|   Z    |   RCODE   |
		+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
		|                    QDCOUNT                    |
		+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
		|                    ANCOUNT                    |
		+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
		|                    NSCOUNT                    |
		+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
		|                    ARCOUNT                    |
		+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

		where:

		ID              A 16 bit identifier assigned by the program that
						generates any kind of query.  This identifier is copied
						the corresponding reply and can be used by the requester
						to match up replies to outstanding queries.

		QR              A one bit field that specifies whether this message is a
						query (0), or a response (1).

		OPCODE          A four bit field that specifies kind of query in this
						message.  This value is set by the originator of a query
						and copied into the response.  The values are:

						0               a standard query (QUERY)

						1               an inverse query (IQUERY)

						2               a server status request (STATUS)

						3-15            reserved for future use

		AA              Authoritative Answer - this bit is valid in responses,
						and specifies that the responding name server is an
						authority for the domain name in question section.

						Note that the contents of the answer section may have
						multiple owner names because of aliases.  The AA bit
						corresponds to the name which matches the query name, or
						the first owner name in the answer section.

		TC              TrunCation - specifies that this message was truncated
						due to length greater than that permitted on the
						transmission channel.

		RD              Recursion Desired - this bit may be set in a query and
						is copied into the response.  If RD is set, it directs
						the name server to pursue the query recursively.
						Recursive query support is optional.

		RA              Recursion Available - this be is set or cleared in a
						response, and denotes whether recursive query support is
						available in the name server.

		Z               Reserved for future use.  Must be zero in all queries
						and responses.

		RCODE           Response code - this 4 bit field is set as part of
						responses.  The values have the following
						interpretation:

						0               No error condition

						1               Format error - The name server was
										unable to interpret the query.

						2               Server failure - The name server was
										unable to process this query due to a
										problem with the name server.

						3               Name Error - Meaningful only for
										responses from an authoritative name
										server, this code signifies that the
										domain name referenced in the query does
										not exist.

						4               Not Implemented - The name server does
										not support the requested kind of query.

						5               Refused - The name server refuses to
										perform the specified operation for
										policy reasons.  For example, a name
										server may not wish to provide the
										information to the particular requester,
										or a name server may not wish to perform
										a particular operation (e.g., zone
										transfer) for particular data.

						6-15            Reserved for future use.

		QDCOUNT         an unsigned 16 bit integer specifying the number of
						entries in the question section.

		ANCOUNT         an unsigned 16 bit integer specifying the number of
						resource records in the answer section.

		NSCOUNT         an unsigned 16 bit integer specifying the number of name
						server resource records in the authority records
						section.

		ARCOUNT         an unsigned 16 bit integer specifying the number of
						resource records in the additional records section.

		*/
    #endregion

    public class Header
    {
        private ushort _flags;

        internal Header(ushort id, OPCode queryCode, ushort questionCount, bool recursionEnabled)
		{
            Id = id;
            QuestionCount = questionCount;
            OPCode = queryCode;
            RecursionEnabled = recursionEnabled;
		}

        internal Header(RecordReader rr)
		{
			Id = rr.ReadUInt16();
			_flags = rr.ReadUInt16();
			QuestionCount = rr.ReadUInt16();
			AnswerCount = rr.ReadUInt16();
			NameServerCount = rr.ReadUInt16();
			AdditionalCount = rr.ReadUInt16();
		}

        /// <summary>
        /// An identifier assigned by the program
        /// </summary>
        public ushort Id { get; }

        /// <summary>
        /// the number of entries in the question section
        /// </summary>
        public ushort QuestionCount { get; internal set; }

        /// <summary>
        /// the number of resource records in the answer section
        /// </summary>
        public ushort AnswerCount { get; internal set; }

        /// <summary>
        /// the number of name server resource records in the authority records section
        /// </summary>
        public ushort NameServerCount { get; internal set; }

        /// <summary>
        /// the number of resource records in the additional records section
        /// </summary>
        public ushort AdditionalCount { get; internal set; }

        /// <summary>
        /// Represents the header as a byte array
        /// </summary>
        public byte[] Data
		{
			get
			{
				List<byte> data = new List<byte>();
				data.AddRange(WriteShort(Id));
				data.AddRange(WriteShort(_flags));
				data.AddRange(WriteShort(QuestionCount));
				data.AddRange(WriteShort(AnswerCount));
				data.AddRange(WriteShort(NameServerCount));
				data.AddRange(WriteShort(AdditionalCount));
				return data.ToArray();
			}
		}

		/// <summary>
		/// query (false), or a response (true)
		/// </summary>
		public bool HasQuery
		{
			get
			{
				return GetBits(_flags, 15, 1) == 1;
			}
		}

		/// <summary>
		/// Specifies kind of query
		/// </summary>
		public OPCode OPCode
		{
			get
			{
				return (OPCode)GetBits(_flags, 11, 4);
			}
			private set
			{
				_flags = SetBits(_flags, 11, 4, (ushort)value);
			}
		}

		/// <summary>
		/// Authoritative Answer
		/// </summary>
		public bool HasAuthoritativeAnswer
		{
			get
			{
				return GetBits(_flags, 10, 1) == 1;
			}
		}

		/// <summary>
		/// TrunCation
		/// </summary>
		public bool TruncationEnabled
		{
			get
			{
				return GetBits(_flags, 9, 1) == 1;
			}
		}

		/// <summary>
		/// Recursion Desired
		/// </summary>
		public bool RecursionEnabled
		{
			get
			{
				return GetBits(_flags, 8, 1) == 1;
			}
            private set
            {
				_flags = SetBits(_flags, 8, 1, value);
			}
		}

		/// <summary>
		/// Recursion Available
		/// </summary>
		public bool RecursionAvailable
		{
			get
			{
				return GetBits(_flags, 7, 1) == 1;
			}
		}

		/// <summary>
		/// Reserved for future use
		/// </summary>
		public ushort ZFutureFlag
		{
			get
			{
				return GetBits(_flags, 4, 3);
			}
		}

		/// <summary>
		/// Response code
		/// </summary>
		public RCode ResponseCode
		{
			get
			{
				return (RCode)GetBits(_flags, 0, 4);
			}
        }

        private byte[] WriteShort(ushort sValue)
        {
            return BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)sValue));
        }

        private ushort SetBits(ushort oldValue, int position, int length, bool blnValue)
        {
            return SetBits(oldValue, position, length, blnValue ? (ushort)1 : (ushort)0);
        }

        private ushort SetBits(ushort oldValue, int position, int length, ushort newValue)
        {
            // sanity check
            if (length <= 0 || position >= 16)
            {
                return oldValue;
            }

            // get some mask to put on
            int mask = (2 << (length - 1)) - 1;

            // clear out value
            oldValue &= (ushort)~(mask << position);

            // set new value
            oldValue |= (ushort)((newValue & mask) << position);
            return oldValue;
        }

        private ushort GetBits(ushort oldValue, int position, int length)
        {
            // sanity check
            if (length <= 0 || position >= 16)
            {
                return 0;
            }

            // get some mask to put on
            int mask = (2 << (length - 1)) - 1;

            // shift down to get some value and mask it
            return (ushort)((oldValue >> position) & mask);
        }
    }
}