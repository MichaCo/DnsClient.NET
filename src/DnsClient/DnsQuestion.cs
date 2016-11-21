using System;

namespace DnsClient
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

        public override string ToString()
        {
            return ToString(0);
        }

        public string ToString(int offset = -32)
        {
            return string.Format("{0,"+offset+"} \t{1} \t{2}", QueryName, QuestionClass, QuestionType);
        }
    }
}