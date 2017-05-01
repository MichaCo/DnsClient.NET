using System;
using System.Collections.Generic;
using System.Linq;
using DnsClient.Protocol;

namespace DnsClient
{
    /// <summary>
    /// The response returned by any query performed by <see cref="IDnsQuery"/> with all answer sections, header and message information.
    /// </summary>
    /// <seealso cref="IDnsQuery"/>
    /// <seealso cref="ILookupClient"/>
    public class DnsQueryResponse : IDnsQueryResponse
    {
        private int? _hashCode;

        /// <inheritdoc />
        public NameServer NameServer { get; }

        /// <inheritdoc />
        public ICollection<DnsResourceRecord> Additionals { get; }

        /// <inheritdoc />
        public IEnumerable<DnsResourceRecord> AllRecords
        {
            get
            {
                return Answers.Concat(Additionals).Concat(Authorities);
            }
        }

        /// <inheritdoc />
        public string AuditTrail { get; internal set; }

        /// <inheritdoc />
        public ICollection<DnsResourceRecord> Answers { get; }

        /// <inheritdoc />
        public ICollection<DnsResourceRecord> Authorities { get; }

        /// <inheritdoc />
        public string ErrorMessage => DnsResponseCodeText.GetErrorText(Header.ResponseCode);

        /// <inheritdoc />
        public bool HasError => Header?.ResponseCode != DnsResponseCode.NoError;

        /// <inheritdoc />
        public DnsResponseHeader Header { get; }

        /// <inheritdoc />
        public ICollection<DnsQuestion> Questions { get; }

        /// <inheritdoc />
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