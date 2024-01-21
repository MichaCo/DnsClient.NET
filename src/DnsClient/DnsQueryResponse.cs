using System;
using System.Collections.Generic;
using System.Linq;
using DnsClient.Protocol;

namespace DnsClient
{
    internal class TruncatedQueryResponse : IDnsQueryResponse
    {
        public IReadOnlyList<DnsQuestion> Questions => throw new NotImplementedException();

        public IReadOnlyList<DnsResourceRecord> Additionals => throw new NotImplementedException();

        public IEnumerable<DnsResourceRecord> AllRecords => throw new NotImplementedException();

        public IReadOnlyList<DnsResourceRecord> Answers => throw new NotImplementedException();

        public IReadOnlyList<DnsResourceRecord> Authorities => throw new NotImplementedException();

        public string AuditTrail => throw new NotImplementedException();

        public string ErrorMessage => throw new NotImplementedException();

        public bool HasError => throw new NotImplementedException();

        public DnsResponseHeader Header => throw new NotImplementedException();

        public int MessageSize => throw new NotImplementedException();

        public NameServer NameServer => throw new NotImplementedException();

        public DnsQuerySettings Settings => throw new NotImplementedException();
    }

    /// <summary>
    /// The response returned by any query performed by <see cref="IDnsQuery"/> with all answer sections, header and message information.
    /// </summary>
    /// <seealso cref="IDnsQuery"/>
    /// <seealso cref="ILookupClient"/>
    public class DnsQueryResponse : IDnsQueryResponse
    {
        private int? _hashCode;

        /// <summary>
        /// Gets the name server which responded with this result.
        /// </summary>
        /// <value>
        /// The name server.
        /// </value>
        public NameServer NameServer { get; }

        /// <summary>
        /// Gets a list of additional records.
        /// </summary>
        public IReadOnlyList<DnsResourceRecord> Additionals { get; }

        /// <summary>
        /// Gets a list of all answers, additional and authority records.
        /// </summary>
        public IEnumerable<DnsResourceRecord> AllRecords
        {
            get
            {
                return Answers.Concat(Additionals).Concat(Authorities);
            }
        }

        /// <summary>
        /// Gets the audit trail if <see cref="DnsQueryOptions.EnableAuditTrail"/>. as set to <c>true</c>, <c>null</c> otherwise.
        /// </summary>
        /// <value>
        /// The audit trail.
        /// </value>
        public string AuditTrail { get; private set; }

        /// <summary>
        /// Gets a list of answer records.
        /// </summary>
        public IReadOnlyList<DnsResourceRecord> Answers { get; }

        /// <summary>
        /// Gets a list of authority records.
        /// </summary>
        public IReadOnlyList<DnsResourceRecord> Authorities { get; }

        /// <summary>
        /// Returns a string value representing the error response code in case an error occurred,
        /// otherwise '<see cref="DnsResponseCode.NoError"/>'.
        /// </summary>
        public string ErrorMessage => DnsResponseCodeText.GetErrorText((DnsResponseCode)Header.ResponseCode);

        /// <summary>
        /// A flag indicating if the header contains a response code other than <see cref="DnsResponseCode.NoError"/>.
        /// </summary>
        public bool HasError => Header?.ResponseCode != DnsHeaderResponseCode.NoError;

        /// <summary>
        /// Gets the header of the response.
        /// </summary>
        public DnsResponseHeader Header { get; }

        /// <summary>
        /// Gets the list of questions.
        /// </summary>
        public IReadOnlyList<DnsQuestion> Questions { get; }

        /// <summary>
        /// Gets the size of the message.
        /// </summary>
        /// <value>
        /// The size of the message.
        /// </value>
        public int MessageSize { get; }

        /// <summary>
        /// Gets the settings used to produce this response.
        /// </summary>
        public DnsQuerySettings Settings { get; }

        internal DnsQueryResponse(DnsResponseMessage dnsResponseMessage, NameServer nameServer, DnsQuerySettings settings)
        {
            if (dnsResponseMessage == null)
            {
                throw new ArgumentNullException(nameof(dnsResponseMessage));
            }

            Header = dnsResponseMessage.Header;
            MessageSize = dnsResponseMessage.MessageSize;
            Questions = dnsResponseMessage.Questions.ToArray();
            Answers = dnsResponseMessage.Answers.ToArray();
            Additionals = dnsResponseMessage.Additionals.ToArray();
            Authorities = dnsResponseMessage.Authorities.ToArray();
            NameServer = nameServer ?? throw new ArgumentNullException(nameof(nameServer));
            Settings = settings;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj is not DnsQueryResponse response)
            {
                return false;
            }

            return
                Header.ToString().Equals(response.Header.ToString(), StringComparison.OrdinalIgnoreCase)
                && string.Join("", Questions).Equals(string.Join("", response.Questions), StringComparison.OrdinalIgnoreCase)
                && string.Join("", AllRecords).Equals(string.Join("", response.AllRecords), StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                var value = (Header.ToString() + string.Join("", Questions) + string.Join("", AllRecords));

#if NET6_0_OR_GREATER || NETSTANDARD2_1
                _hashCode = value.GetHashCode(StringComparison.Ordinal);
#else
                _hashCode = value.GetHashCode();
#endif
            }

            return _hashCode.Value;
        }

        internal static void SetAuditTrail(IDnsQueryResponse response, string value)
        {
            if (response is DnsQueryResponse queryResponse)
            {
                queryResponse.AuditTrail = value;
            }
        }
    }
}
