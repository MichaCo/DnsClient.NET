using System;
using System.Collections.Generic;
using System.Linq;
using DnsClient.Protocol;

namespace DnsClient
{
    /// <summary>
    /// A simple response message which gets returned by the <see cref="LookupClient"/>.
    /// </summary>
    public class DnsResponseMessage
    {
        public DnsResponseMessage(DnsResponseHeader header, int messageSize)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            Header = header;
            MessageSize = messageSize;
        }

        public IList<DnsResourceRecord> Additionals { get; } = new List<DnsResourceRecord>();

        public IList<DnsResourceRecord> Answers { get; } = new List<DnsResourceRecord>();

        public IList<DnsResourceRecord> Authorities { get; } = new List<DnsResourceRecord>();

        public DnsResponseHeader Header { get; }

        public int MessageSize { get; }

        public IList<DnsQuestion> Questions { get; } = new List<DnsQuestion>();

        public void AddAdditional(DnsResourceRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            Additionals.Add(record);
        }

        public void AddAnswer(DnsResourceRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            Answers.Add(record);
        }

        public void AddAuthority(DnsResourceRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            Authorities.Add(record);
        }

        public void AddQuestion(DnsQuestion question)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            Questions.Add(question);
        }

        /// <summary>
        /// Gets the readonly representation of this message which can be returned.
        /// </summary>
        public DnsQueryResponse AsQueryResponse(NameServer nameServer)
            => new DnsQueryResponse(this, nameServer);
    }
}