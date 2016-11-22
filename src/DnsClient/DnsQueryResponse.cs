using System;
using System.Collections.Generic;
using System.Linq;
using DnsClient.Protocol;

namespace DnsClient
{
    /// <summary>
    /// Immutable version of the <see cref="DnsResponseMessage"/>.
    /// </summary>
    public class DnsQueryResponse
    {
        private int? _hashCode;

        /// <summary>
        /// Gets a list of additional records.
        /// </summary>
        public IReadOnlyCollection<DnsResourceRecord> Additionals { get; }

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

        /// <summary>
        /// Gets a list of answer records.
        /// </summary>
        public IReadOnlyCollection<DnsResourceRecord> Answers { get; }

        /// <summary>
        /// Gets a list of authority records.
        /// </summary>
        public IReadOnlyCollection<DnsResourceRecord> Authorities { get; }

        /// <summary>
        /// Returns a string value representing the error response code in case an error occured, otherwise empty.
        /// </summary>
        public string ErrorMessage => HasError ? DnsResponseCodeText.GetErrorText(Header.ResponseCode) : string.Empty;

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
        public IReadOnlyCollection<DnsQuestion> Questions { get; }

        /// <summary>
        /// Creates a new instace of <see cref="DnsQueryResponse"/>.
        /// </summary>
        /// <see cref="DnsResponseMessage"/>
        public DnsQueryResponse(
            DnsResponseHeader header,
            IReadOnlyCollection<DnsQuestion> questions,
            IReadOnlyCollection<DnsResourceRecord> answers,
            IReadOnlyCollection<DnsResourceRecord> additionals,
            IReadOnlyCollection<DnsResourceRecord> authorities)
        {
            if (header == null) throw new ArgumentNullException(nameof(header));
            if (questions == null) throw new ArgumentNullException(nameof(questions));
            if (answers == null) throw new ArgumentNullException(nameof(answers));
            if (additionals == null) throw new ArgumentNullException(nameof(additionals));
            if (authorities == null) throw new ArgumentNullException(nameof(authorities));

            Header = header;
            Questions = questions;
            Answers = answers;
            Additionals = additionals;
            Authorities = authorities;
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