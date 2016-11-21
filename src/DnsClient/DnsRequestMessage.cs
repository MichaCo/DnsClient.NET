using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DnsClient
{
    public class DnsRequestMessage
    {
        public DnsRequestMessage(DnsRequestHeader header, params DnsQuestion[] questions)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }
            if (questions == null || questions.Length == 0)
            {
                throw new ArgumentException("At least one question must be specified for the request.", nameof(questions));
            }
            if (header.QuestionCount != questions.Length)
            {
                throw new InvalidOperationException("Header question count and number of questions do not match.");
            }

            Header = header;
            Questions = questions;
        }

        public DnsQuestion[] Questions { get; }

        public DnsRequestHeader Header { get; }
    }
}
