using System;
using System.Collections.Generic;
using System.Linq;

namespace DnsClient.Protocol
{
    /// <summary>
    /// A simple response message which gets returned by the <see cref="LookupClient"/>.
    /// </summary>
    public class DnsResponseMessage
    {
        private readonly IList<DnsResourceRecord> _additionals = new List<DnsResourceRecord>();
        private readonly IList<DnsResourceRecord> _answers = new List<DnsResourceRecord>();
        private readonly IList<DnsResourceRecord> _authorities = new List<DnsResourceRecord>();
        private readonly DnsResponseHeader _header;
        private readonly IList<DnsQuestion> _questions = new List<DnsQuestion>();

        /// <summary>
        /// Gets the readonly representation of this message which can be returned.
        /// </summary>
        public DnsQueryResponse AsReadonly
            => new DnsQueryResponse(_header, _questions.ToArray(), _answers.ToArray(), _additionals.ToArray(), _authorities.ToArray());

        public DnsResponseHeader Header => _header;

        public DnsResponseMessage(DnsResponseHeader header)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            _header = header;
        }

        public void AddAdditional(DnsResourceRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            _additionals.Add(record);
        }

        public void AddAnswer(DnsResourceRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            _answers.Add(record);
        }

        public void AddAuthority(DnsResourceRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            _authorities.Add(record);
        }

        public void AddQuestion(DnsQuestion question)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            _questions.Add(question);
        }
    }
}