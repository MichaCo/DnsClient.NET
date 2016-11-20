using System.Collections.Generic;
using System.Linq;
using DnsClient2.Protocol;
using DnsClient2.Protocol.Record;

namespace DnsClient2
{
    public class DnsResponseMessage
    {
        private IList<ResourceRecord> _additionals = new List<ResourceRecord>();
        private IList<ResourceRecord> _answers = new List<ResourceRecord>();
        private IList<DnsQuestion> _questions = new List<DnsQuestion>();
        private IList<ResourceRecord> _servers = new List<ResourceRecord>();

        /// <summary>
        /// Gets a list of additional records.
        /// </summary>
        public IReadOnlyCollection<ResourceRecord> Additionals => _additionals.ToArray();

        /// <summary>
        /// Gets a list of answer records.
        /// </summary>
        public IReadOnlyCollection<ResourceRecord> Answers => _answers.ToArray();

        /// <summary>
        /// Gets a list of authority records.
        /// </summary>
        public IReadOnlyCollection<ResourceRecord> Authorities => _servers.ToArray();

        /// <summary>
        /// Gets the header of the response.
        /// </summary>
        public DnsResponseHeader Header { get; }

        /// <summary>
        /// Gets the list of questions.
        /// </summary>
        public IReadOnlyCollection<DnsQuestion> Questions => _questions.ToArray();

        public DnsResponseMessage(DnsResponseHeader header)
        {
            this.Header = header;
        }

        public void AddAdditional(ResourceRecord record)
        {
            _additionals.Add(record);
        }

        public void AddAnswer(ResourceRecord record)
        {
            _answers.Add(record);
        }

        public void AddQuestion(DnsQuestion question)
        {
            _questions.Add(question);
        }

        public void AddServer(ResourceRecord record)
        {
            _servers.Add(record);
        }
    }
}