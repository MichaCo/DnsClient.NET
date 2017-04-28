using System;

namespace DnsClient
{
    public class DnsQuestion
    {
        public DnsString QueryName { get; }

        public QueryClass QuestionClass { get; }

        public QueryType QuestionType { get; }

        public DnsQuestion(string query, QueryType questionType, QueryClass questionClass)
            : this(DnsString.Parse(query), questionType, questionClass)
        {
        }

        public DnsQuestion(DnsString query, QueryType questionType, QueryClass questionClass)
        {
            QueryName = query ?? throw new ArgumentNullException(nameof(query));
            QuestionType = questionType;
            QuestionClass = questionClass;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return ToString(0);
        }

        /// <summary>
        /// Returns the information of this instance in a friendly format with an optional <paramref name="offset"/>.
        /// </summary>
        /// <param name="offset">The optional offset which can be used for pretty printing.</param>
        /// <returns>The string representation of this instance.</returns>
        public string ToString(int offset = -32)
        {
            return string.Format("{0," + offset + "} \t{1} \t{2}", QueryName.Original, QuestionClass, QuestionType);
        }
    }
}