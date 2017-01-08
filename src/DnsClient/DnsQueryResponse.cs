using System;
using System.Collections.Generic;
using System.Linq;
using DnsClient.Protocol;

namespace DnsClient
{
    /// <summary>
    /// Immutable version of the <see cref="DnsResponseMessage"/>.
    /// </summary>
    public class DnsQueryResponse : IDnsQueryResponse
    {
        private int? _hashCode;

        public NameServer NameServer { get; }

        /// <summary>
        /// Gets a list of additional records.
        /// </summary>
        public ICollection<DnsResourceRecord> Additionals { get; }

        /// <summary>
        /// Gets a list of all answers, addtional and authority records.
        /// </summary>
        public IEnumerable<DnsResourceRecord> AllRecords
        {
            get
            {
                return Answers.Concat(Additionals).Concat(Authorities);
            }
        }

        public string AuditTrail { get; internal set; }

        /// <summary>
        /// Gets a list of answer records.
        /// </summary>
        public ICollection<DnsResourceRecord> Answers { get; }

        /// <summary>
        /// Gets a list of authority records.
        /// </summary>
        public ICollection<DnsResourceRecord> Authorities { get; }

        /// <summary>
        /// Returns a string value representing the error response code in case an error occured, otherwise 'No Error'.
        /// </summary>
        public string ErrorMessage => DnsResponseCodeText.GetErrorText(Header.ResponseCode);

        /// <summary>
        /// A flag indicating if the header contains a response codde other than <see cref="DnsResponseCode.NoError"/>.
        /// </summary>
        public bool HasError => Header?.ResponseCode != DnsResponseCode.NoError;

        /// <summary>
        /// Gets the header of the response.
        /// </summary>
        public DnsResponseHeader Header { get; }

        /// <summary>
        /// Gets the list of questions.
        /// </summary>
        public ICollection<DnsQuestion> Questions { get; }

        public int MessageSize { get; }

        internal DnsQueryResponse(DnsResponseMessage dnsResponseMessage, NameServer nameServer)
        {
            if (dnsResponseMessage == null) throw new ArgumentNullException(nameof(dnsResponseMessage));
            Header = dnsResponseMessage.Header;
            MessageSize = dnsResponseMessage.MessageSize;
            Questions = dnsResponseMessage.Questions;
            Answers = dnsResponseMessage.Answers;
            Additionals = dnsResponseMessage.Additionals;
            Authorities = dnsResponseMessage.Authorities;
            NameServer = nameServer;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var response = obj as DnsQueryResponse;
            if (response == null)
            {
                return false;
            }

            return
                Header.ToString().Equals(response.Header.ToString())
                && string.Join("", Questions).Equals(string.Join("", response.Questions))
                && string.Join("", AllRecords).Equals(string.Join("", response.AllRecords));
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                _hashCode = (Header.ToString() + string.Join("", Questions) + string.Join("", AllRecords)).GetHashCode();
            }

            return _hashCode.Value;
        }
    }
}