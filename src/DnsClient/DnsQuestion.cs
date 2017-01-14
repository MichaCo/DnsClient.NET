using System;

namespace DnsClient
{
    public class DnsQuestion
    {
        public DnsString QueryName { get; }

        public QueryClass QuestionClass { get; }

        public QueryType QuestionType { get; }

        public DnsQuestion(string query, QueryType questionType, QueryClass questionClass)
            : this(DnsString.ParseQueryString(query), questionType, questionClass)
        {
        }

        public DnsQuestion(DnsString query, QueryType questionType, QueryClass questionClass)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }
            
            QueryName = query;
            QuestionType = questionType;
            QuestionClass = questionClass;
        }

        public override string ToString()
        {
            return ToString(0);
        }

        public string ToString(int offset = -32)
        {
            return string.Format("{0," + offset + "} \t{1} \t{2}", QueryName.Original, QuestionClass, QuestionType);
        }
    }
}