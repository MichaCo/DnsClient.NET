using System;

namespace DnsClient
{
    public class DnsQuestion
    {
        public DnsName QueryName { get; }

        public QueryClass QuestionClass { get; }

        public QueryType QuestionType { get; }

        public DnsQuestion(string queryName, QueryType questionType, QueryClass questionClass)
            : this(new DnsName(queryName), questionType, questionClass)
        {
        }

        public DnsQuestion(DnsName queryName, QueryType questionType, QueryClass questionClass)
        {
            if (queryName == null)
            {
                throw new ArgumentNullException(nameof(queryName));
            }

            if (!queryName.IsHostName)
            {
                DnsName original = queryName;
                try
                {
                    queryName = DnsName.ParsePuny(queryName.ValueUTF8);
                }
                catch
                {
                    throw new ArgumentException($"'{original.OriginalString}' is not a valid hostname or puny address.", nameof(queryName));
                }
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
            return string.Format("{0," + offset + "} \t{1} \t{2}", QueryName, QuestionClass, QuestionType);
        }
    }
}