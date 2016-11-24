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

        public DnsQuestion Question { get; }

        public DnsRequestMessage(DnsRequestHeader header, DnsQuestion question)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }
            if (question == null )
            {
                throw new ArgumentNullException(nameof(question));
            }
            if (header.QuestionCount != 1)
            {
                throw new InvalidOperationException("Header question count and number of questions do not match.");
            }

            Header = header;
            Question = question;
        }
    }
}