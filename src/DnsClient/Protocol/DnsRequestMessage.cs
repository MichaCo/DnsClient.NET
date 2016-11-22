using System;
using System.Linq;

namespace DnsClient.Protocol
{
    /// <summary>
    /// Represents a simple request message which can be send through <see cref="DnsMessageHandler"/>.
    /// </summary>
    public class DnsRequestMessage
    {
        public DnsRequestHeader Header { get; }

        public DnsQuestion[] Questions { get; }

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
    }
}