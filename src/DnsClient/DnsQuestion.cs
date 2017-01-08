using System;

namespace DnsClient
{
    public class DnsQuestion
    {
        public QueryName QueryName { get; }

        public QueryClass QuestionClass { get; }

        public QueryType QuestionType { get; }

        public DnsQuestion(string queryName, QueryType questionType, QueryClass questionClass)
            : this(new QueryName(queryName), questionType, questionClass)
        {
        }

        public DnsQuestion(QueryName queryName, QueryType questionType, QueryClass questionClass)
        {
            if (queryName == null)
            {
                throw new ArgumentNullException(nameof(queryName));
            }
            
            QueryName = queryName;
            QuestionType = questionType;
            QuestionClass = questionClass;
        }

        public override string ToString()
        {
            return ToString(0);
        }

        public string ToString(int offset = -32)
        {
            return string.Format("{0," + offset + "} \t{1} \t{2}", ((DnsName)QueryName).ValueUTF8, QuestionClass, QuestionType);
        }
    }
}