using System;
using System.Linq;

namespace DnsClient
{
    /// <summary>
    /// Represents a simple request message which can be send through <see cref="DnsMessageHandler"/>.
    /// </summary>
    internal class DnsRequestMessage
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

            Header = header;
            Question = question;
        }
    }
}