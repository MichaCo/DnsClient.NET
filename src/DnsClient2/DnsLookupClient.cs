using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DnsClient2
{
    public class DnsLookupClient
    {
        private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan s_infiniteTimeout = System.Threading.Timeout.InfiniteTimeSpan;
        private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);
        private readonly DnsMessageInvoker _messageInvoker;
        private TimeSpan _timeout;

        /// <summary>
        /// Gets the list of configured name servers.
        /// </summary>
        public IReadOnlyCollection<IPEndPoint> NameServers { get; }

        /// <summary>
        /// Gets or set a flag indicating if recursion should be enabled for DNS queries.
        /// </summary>
        public bool Recursion { get; set; } = true;

        /// <summary>
        /// Gets or sets number of tries to connect to one name server before trying the next one or throwing an exception.
        /// </summary>
        public int Retries { get; set; } = 5;

        /// <summary>
        /// Gets or sets timeout in milliseconds.
        /// Timeout must be greater than zero and less than <see cref="int.MaxValue"/>.
        /// </summary>
        public TimeSpan Timeout
        {
            get { return _timeout; }
            set
            {
                if ((value <= TimeSpan.Zero || value > s_maxTimeout) && value != s_infiniteTimeout)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _timeout = value;
            }
        }

        /// <summary>
        /// Gets or sets protocol to use.
        /// </summary>
        public TransportProtocol TransportType { get; set; } = TransportProtocol.Udp;

        /// <summary>
        /// Gets or sets a flag indicating if the <see cref="DnsLookupClient"/> should use caching or not.
        /// </summary>
        public bool UseCache { get; set; } = true;

        public DnsLookupClient(DnsMessageInvoker messageInvoker, ICollection<IPEndPoint> nameServers)
        {
            if (messageInvoker == null)
            {
                throw new ArgumentNullException(nameof(messageInvoker));
            }
            if (nameServers == null || nameServers.Count == 0)
            {
                throw new ArgumentException("At least one name server must be configured.", nameof(nameServers));
            }

            NameServers = nameServers.ToArray();
            _messageInvoker = messageInvoker;
        }

        public Task<DnsResponseMessage> QueryAsync(string query, ushort qtype)
        {
            var head = new DnsRequestHeader()
            {
            };

            var request = new DnsRequestMessage(head, new DnsQuestion());

            return QueryAsync(request);
        }

        public Task<DnsResponseMessage> QueryAsync(string query, ushort qtype, ushort qclass)
        {
            var head = new DnsRequestHeader()
            {
            };

            var request = new DnsRequestMessage(head, new DnsQuestion());

            return QueryAsync(request);
        }

        public Task<DnsResponseMessage> QueryAsync(DnsRequestMessage request)
        {
            return QueryAsync(request, CancellationToken.None);
        }

        public Task<DnsResponseMessage> QueryAsync(DnsRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            foreach (var server in NameServers)
            {
                var tries = 0;
                do
                {
                    tries++;
                    try
                    {
                        var resultTask = _messageInvoker.QueryAsync(server, request, cancellationToken);
                        if (Timeout != s_infiniteTimeout)
                        {
                            return resultTask.TimeoutAfter(Timeout);
                        }

                        return resultTask;
                    }
                    catch (TimeoutException)
                    {
                    }
                    finally
                    {
                    }
                } while (tries <= Retries && !cancellationToken.IsCancellationRequested);
            }

            throw new InvalidOperationException("No connection could be established.");
        }
    }
}