using System;

namespace DnsClient2
{
    public class DnsQuestion
    {
        public DnsName QueryName { get; }

        public ushort QuestionClass { get; }

        public ushort QuestionType { get; }

        public DnsQuestion(string queryName, ushort questionType, ushort questionClass)
            : this(new DnsName(queryName), questionType, questionClass)
        {
        }

        public DnsQuestion(DnsName queryName, ushort questionType, ushort questionClass)
        {
            if (queryName == null)
            {
                throw new ArgumentNullException(nameof(queryName));
            }

            QueryName = queryName;
            QuestionType = questionType;
            QuestionClass = questionClass;
        }
    }
}