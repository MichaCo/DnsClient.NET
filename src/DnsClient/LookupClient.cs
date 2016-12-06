using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DnsClient.Protocol;

namespace DnsClient
{
    public class LookupClient : IDisposable
    {
        private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan s_infiniteTimeout = System.Threading.Timeout.InfiniteTimeSpan;
        private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);
        private static ushort _uniqueId = 0;
        private readonly ResponseCache _cache = new ResponseCache(true);
        private readonly object _endpointLock = new object();
        private readonly DnsMessageHandler _messageHandler;
        private Queue<EndPointInfo> _endpoints;
        private TimeSpan _timeout = s_defaultTimeout;
        private bool _disposedValue = false;

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
        /// Gets or sets a flag indicating if the <see cref="LookupClient"/> should throw an <see cref="DnsResponseException"/>
        /// if the returned result contains an error flag other than <see cref="DnsResponseCode.NoError"/>.
        /// (The default behavior is <c>False</c>).
        /// </summary>
        public bool ThrowDnsErrors { get; set; } = false;

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
        /// Gets or sets a flag indicating if the <see cref="LookupClient"/> should use caching or not.
        /// The TTL of cached results is defined by each resource record individually.
        /// </summary>
        public bool UseCache
        {
            get
            {
                return _cache.Enabled;
            }
            set
            {
                _cache.Enabled = value;
            }
        }

        /// <summary>
        /// Gets or sets a <see cref="TimeSpan"/> which can override the TTL of a resource record in case the
        /// TTL of the record is lower than this minimum value.
        /// This is useful in cases where the server retruns a zero TTL and the record should be cached for a
        /// very short duration anyways.
        ///
        /// This setting gets igonred in case <see cref="UseCache"/> is set to <c>False</c>.
        /// </summary>
        public TimeSpan? MimimumCacheTimeout
        {
            get
            {
                return _cache.MinimumTimout;
            }
            set
            {
                _cache.MinimumTimout = value;
            }
        }

        public LookupClient()
            : this(NameServer.ResolveNameServers().ToArray())
        {
        }

        public LookupClient(params IPEndPoint[] nameServers)
            : this(new DnsUdpMessageHandler(), nameServers)
        {
        }

        public LookupClient(params IPAddress[] nameServers)
            : this(
                  new DnsUdpMessageHandler(),
                  nameServers.Select(p => new IPEndPoint(p, NameServer.DefaultPort)).ToArray())
        {
        }

        public LookupClient(DnsMessageHandler messageHandler, ICollection<IPEndPoint> nameServers)
        {
            if (messageHandler == null)
            {
                throw new ArgumentNullException(nameof(messageHandler));
            }
            if (nameServers == null || nameServers.Count == 0)
            {
                throw new ArgumentException("At least one name server must be configured.", nameof(nameServers));
            }

            NameServers = nameServers.ToArray();
            _endpoints = new Queue<EndPointInfo>();
            foreach (var server in NameServers)
            {
                _endpoints.Enqueue(new EndPointInfo(server));
            }
            _messageHandler = messageHandler;
        }

        /// <summary>
        /// Translates the IPV4 or IPV6 address into an arpa address.
        /// </summary>
        /// <param name="ip">IP address to get the arpa address form</param>
        /// <returns>The mirrored IPV4 or IPV6 arpa address</returns>
        public static string GetArpaName(IPAddress ip)
        {
            var bytes = ip.GetAddressBytes();
            Array.Reverse(bytes);

            // check IP6
            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // reveresed bytes need to be split into 4 bit parts and separated by '.'
                var newBytes = bytes
                    .SelectMany(b => new[] { (b >> 0) & 0xf, (b >> 4) & 0xf })
                    .Aggregate(new StringBuilder(), (s, b) => s.Append(b.ToString("x")).Append(".")) + "ip6.arpa.";

                return newBytes;
            }
            else if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                // else IP4
                return string.Join(".", bytes) + ".in-addr.arpa.";
            }

            throw new InvalidOperationException("Not a valid IP4 or IP6 address.");
        }

        public Task<DnsQueryResponse> QueryAsync(string query, QueryType queryType)
            => QueryAsync(query, queryType, CancellationToken.None);

        public Task<DnsQueryResponse> QueryAsync(string query, QueryType queryType, CancellationToken cancellationToken)
            => QueryAsync(query, queryType, QueryClass.IN, cancellationToken);

        public Task<DnsQueryResponse> QueryAsync(string query, QueryType queryType, QueryClass queryClass)
            => QueryAsync(query, queryType, queryClass, CancellationToken.None);

        public Task<DnsQueryResponse> QueryAsync(string query, QueryType queryType, QueryClass queryClass, CancellationToken cancellationToken)
            => QueryAsync(new DnsQuestion(query, queryType, queryClass), cancellationToken);

        ////public Task<DnsQueryResponse> QueryAsync(params DnsQuestion[] questions)
        ////    => QueryAsync(CancellationToken.None, questions);

        private async Task<DnsQueryResponse> QueryAsync(DnsQuestion question, CancellationToken cancellationToken)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            var head = new DnsRequestHeader(GetNextUniqueId(), 1, Recursion, DnsOpCode.Query);
            var request = new DnsRequestMessage(head, question);
            var cacheKey = ResponseCache.GetCacheKey(question);
            var result = await _cache.GetOrAdd(cacheKey, async () => await ResolveQueryAsync(request, cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
            
            return result;
        }

        public Task<DnsQueryResponse> QueryReverseAsync(IPAddress ipAddress)
            => QueryReverseAsync(ipAddress, CancellationToken.None);

        public Task<DnsQueryResponse> QueryReverseAsync(IPAddress ipAddress, CancellationToken cancellationToken)
        {
            if (ipAddress == null)
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            var arpa = GetArpaName(ipAddress);            
            return QueryAsync(arpa, QueryType.PTR, QueryClass.IN, cancellationToken);
        }

        private static ushort GetNextUniqueId()
        {
            if (_uniqueId == ushort.MaxValue || _uniqueId == 0)
            {
                _uniqueId = (ushort)(new Random()).Next(ushort.MaxValue / 2);
            }

            return _uniqueId++;
        }

        // TODO: TCP fallback on truncates
        // TODO: most popular DNS servers do not support mulitple queries in one packet, therefore, split it into multiple requests?
        //private async Task<DnsQueryResponse> QueryAsync(DnsRequestMessage request, CancellationToken cancellationToken)
        //{
            
        //}

        private async Task<DnsQueryResponse> ResolveQueryAsync(DnsRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            for (int index = 0; index < NameServers.Count; index++)
            {
                EndPointInfo serverInfo = null;
                lock (_endpointLock)
                {
                    while (_endpoints.Count > 0 && serverInfo == null)
                    {
                        serverInfo = _endpoints.Dequeue();

                        if (serverInfo.IsDisabled)
                        {
                            serverInfo = null;
                        }
                        else
                        {
                            // put it back and then use it..
                            _endpoints.Enqueue(serverInfo);
                        }
                    }

                    if (serverInfo == null)
                    {
                        // let's be optimistic and eable them again, maybe they wher offline one for a while
                        _endpoints.ToList().ForEach(p => p.IsDisabled = false);

                        continue;
                    }
                }

                var tries = 0;
                do
                {
                    tries++;
                    try
                    {
                        DnsResponseMessage response;
                        var resultTask = _messageHandler.QueryAsync(serverInfo.Endpoint, request, cancellationToken);
                        if (Timeout != s_infiniteTimeout)
                        {
                            response = await resultTask.TimeoutAfter(Timeout).ConfigureAwait(false);
                        }

                        response = await resultTask.ConfigureAwait(false);

                        var result = response.AsReadonly;

                        if (ThrowDnsErrors && result.Header.ResponseCode != DnsResponseCode.NoError)
                        {
                            throw new DnsResponseException(result.Header.ResponseCode);
                        }

                        return result;
                    }
                    catch (DnsResponseException)
                    {
                        // occurs only if the option to throw dns exceptions is enabled on the lookup client. (see above).
                        // lets not mess with the stack
                        throw;
                    }
                    catch (TimeoutException)
                    {
                        // do nothing... transient if timeoutAfter timed out
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressFamilyNotSupported)
                    {
                        // this socket error might indicate the server endpoint is actually bad and should be ignored in future queries.
                        serverInfo.IsDisabled = true;
                        Debug.WriteLine($"Disabling name server {serverInfo.Endpoint}.");
                        break;
                    }
                    catch (Exception ex) when (_messageHandler.IsTransientException(ex))
                    {
                    }
                    catch (Exception ex)
                    {
                        var agg = ex as AggregateException;
                        if (agg != null)
                        {
                            agg.Handle(e =>
                            {
                                if (e is TimeoutException) return true;
                                if (_messageHandler.IsTransientException(e)) return true;
                                return false;
                            });

                            throw new DnsResponseException("Unhandled exception", agg.InnerException);
                        }

                        throw new DnsResponseException("Unhandled exception", ex);
                    }
                    finally
                    {
                        // do cleanup stuff or logging?
                    }
                } while (tries <= Retries && !cancellationToken.IsCancellationRequested);
            }

            throw new DnsResponseException($"No connection could be established to any of the following name servers: {string.Join(", ", NameServers)}.");
        }

        private class EndPointInfo
        {
            public IPEndPoint Endpoint { get; }

            public bool IsDisabled { get; set; }

            public EndPointInfo(IPEndPoint endpoint)
            {
                Endpoint = endpoint;
                IsDisabled = false;
            }
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _messageHandler.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}