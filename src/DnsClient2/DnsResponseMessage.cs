using System.Collections.Generic;
using System.Linq;
using DnsClient2.Protocol;
using DnsClient2.Protocol.Record;

namespace DnsClient2
{
    public class DnsResponseMessage
    {
        private IList<DnsResourceRecord> _additionals = new List<DnsResourceRecord>();
        private IList<DnsResourceRecord> _answers = new List<DnsResourceRecord>();
        private IList<DnsQuestion> _questions = new List<DnsQuestion>();
        private IList<DnsResourceRecord> _servers = new List<DnsResourceRecord>();

        /// <summary>
        /// Gets a list of additional records.
        /// </summary>
        public IReadOnlyCollection<DnsResourceRecord> Additionals => _additionals.ToArray();

        /// <summary>
        /// Gets a list of answer records.
        /// </summary>
        public IReadOnlyCollection<DnsResourceRecord> Answers => _answers.ToArray();

        /// <summary>
        /// Gets a list of authority records.
        /// </summary>
        public IReadOnlyCollection<DnsResourceRecord> Authorities => _servers.ToArray();

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

        public void AddAdditional(DnsResourceRecord record)
        {
            _additionals.Add(record);
        }

        public void AddAnswer(DnsResourceRecord record)
        {
            _answers.Add(record);
        }

        public void AddQuestion(DnsQuestion question)
        {
            _questions.Add(question);
        }

        public void AddServer(DnsResourceRecord record)
        {
            _servers.Add(record);
        }
    }
}