using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DnsClient.Protocol.Options;

namespace DnsClient
{
    public class LookupClient : ILookupClient, IDnsQuery
    {
        private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan s_infiniteTimeout = System.Threading.Timeout.InfiniteTimeSpan;
        private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);
        private static int _uniqueId = 0;
        private readonly ResponseCache _cache = new ResponseCache(true);
        private readonly object _endpointLock = new object();
        private readonly DnsMessageHandler _messageHandler;
        private readonly DnsMessageHandler _tcpFallbackHandler;
        private readonly ConcurrentQueue<NameServer> _endpoints;
        private readonly Random _random = new Random();
        private TimeSpan _timeout = s_defaultTimeout;

        /// <summary>
        /// Gets or sets a flag indicating if Tcp should not be used in case a Udp response is truncated.
        /// If <c>True</c>, truncated results will potentially yield no answers.
        /// </summary>
        public bool UseTcpFallback { get; set; } = true;

        /// <summary>
        /// Gets or sets a flag indicating if Udp should not be used at all.
        /// </summary>
        public bool UseTcpOnly { get; set; }

        /// <summary>
        /// Gets the list of configured name servers.
        /// </summary>
        public IReadOnlyCollection<NameServer> NameServers { get; }

        /// <summary>
        /// If enabled, each response will contain a full documentation of the lookup chain
        /// </summary>
        public bool EnableAuditTrail { get; set; } = false;

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
            : this(NameServer.ResolveNameServers()?.ToArray())
        {
        }

        public LookupClient(params IPAddress[] nameServers)
            : this(nameServers?.Select(p => new IPEndPoint(p, NameServer.DefaultPort)).ToArray())
        {
        }

        public LookupClient(IPAddress address, int port)
           : this(new IPEndPoint(address, port))
        {
        }

        public LookupClient(params IPEndPoint[] nameServers)
        {
            if (nameServers == null || nameServers.Length == 0)
            {
                throw new ArgumentException("At least one name server must be configured.", nameof(nameServers));
            }

            // TODO validate ip endpoints

            NameServers = nameServers.Select(p => new NameServer(p)).ToArray();
            _endpoints = new ConcurrentQueue<NameServer>(NameServers);
            _messageHandler = new DnsUdpMessageHandler(true);
            _tcpFallbackHandler = new DnsTcpMessageHandler();
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

        public IDnsQueryResponse QueryReverse(IPAddress ipAddress)
        {
            if (ipAddress == null)
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            var arpa = GetArpaName(ipAddress);
            return Query(arpa, QueryType.PTR, QueryClass.IN);
        }

        public Task<IDnsQueryResponse> QueryReverseAsync(IPAddress ipAddress)
            => QueryReverseAsync(ipAddress, CancellationToken.None);

        public Task<IDnsQueryResponse> QueryReverseAsync(IPAddress ipAddress, CancellationToken cancellationToken)
        {
            if (ipAddress == null)
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            var arpa = GetArpaName(ipAddress);
            return QueryAsync(arpa, QueryType.PTR, QueryClass.IN, cancellationToken);
        }

        public IDnsQueryResponse Query(string query, QueryType queryType)
            => Query(query, queryType, QueryClass.IN);

        public IDnsQueryResponse Query(string query, QueryType queryType, QueryClass queryClass)
            => Query(new DnsQuestion(query, queryType, queryClass));

        private IDnsQueryResponse Query(DnsQuestion question)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            var head = new DnsRequestHeader(GetNextUniqueId(), Recursion, DnsOpCode.Query);
            var request = new DnsRequestMessage(head, question);
            var handler = UseTcpOnly ? _tcpFallbackHandler : _messageHandler;

            if (_cache.Enabled)
            {
                var cacheKey = ResponseCache.GetCacheKey(question);
                var item = _cache.Get(cacheKey);
                if (item == null)
                {
                    item = ResolveQuery(handler, request);
                    _cache.Add(cacheKey, item);
                }

                return item;
            }
            else
            {
                return ResolveQuery(handler, request);
            }
        }

        private IDnsQueryResponse ResolveQuery(DnsMessageHandler handler, DnsRequestMessage request, Audit continueAudit = null)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var audit = continueAudit ?? new Audit();
            var servers = GetNextServers();

            foreach (var serverInfo in servers)
            {
                var tries = 0;
                do
                {
                    tries++;

                    try
                    {
                        if (EnableAuditTrail)
                        {
                            audit.StartTimer();
                        }

                        DnsResponseMessage response = handler.Query(serverInfo.Endpoint, request, Timeout);

                        if (response.Header.ResultTruncated && UseTcpFallback && !handler.GetType().Equals(typeof(DnsTcpMessageHandler)))
                        {
                            if (EnableAuditTrail)
                            {
                                audit.AuditTruncatedRetryTcp();
                            }

                            return ResolveQuery(_tcpFallbackHandler, request, audit);
                        }

                        if (EnableAuditTrail)
                        {
                            audit.AuditResolveServers(_endpoints.Count);
                            audit.AuditResponseHeader(response.Header);
                        }

                        if (response.Header.ResponseCode != DnsResponseCode.NoError)
                        {
                            if (EnableAuditTrail)
                            {
                                audit.AuditResponseError(response.Header.ResponseCode);
                            }

                            if (ThrowDnsErrors)
                            {
                                throw new DnsResponseException(response.Header.ResponseCode);
                            }
                        }

                        HandleOptRecords(audit, serverInfo, response);

                        DnsQueryResponse queryResponse = response.AsQueryResponse(serverInfo.Clone());

                        if (EnableAuditTrail)
                        {
                            audit.AuditResponse(queryResponse);
                            audit.AuditEnd(queryResponse);
                            queryResponse.AuditTrail = audit.Build();
                        }

                        Interlocked.Increment(ref StaticLog.SyncResolveQueryCount);
                        Interlocked.Add(ref StaticLog.SyncResolveQueryTries, tries);
                        return queryResponse;
                    }
                    catch (DnsResponseException ex)
                    {
                        audit.AuditException(ex);
                        ex.AuditTrail = audit.Build();
                        throw;
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressFamilyNotSupported)
                    {
                        // this socket error might indicate the server endpoint is actually bad and should be ignored in future queries.
                        DisableServer(serverInfo);
                        break;
                    }
                    catch (Exception ex) when (ex is TimeoutException || handler.IsTransientException(ex))
                    {
                        DisableServer(serverInfo);
                    }
                    catch (Exception ex)
                    {
                        DisableServer(serverInfo);

                        if (ex is OperationCanceledException || ex is TaskCanceledException)
                        {
                            // timeout
                            continue;
                        }

                        audit.AuditException(ex);

                        throw new DnsResponseException(DnsResponseCode.Unassigned, "Unhandled exception", ex)
                        {
                            AuditTrail = audit.Build()
                        };
                    }
                } while (tries <= Retries && serverInfo.Enabled);
            }
            throw new DnsResponseException(DnsResponseCode.ConnectionTimeout, $"No connection could be established to any of the following name servers: {string.Join(", ", NameServers)}.")
            {
                AuditTrail = audit.Build()
            };
        }

        public Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType)
            => QueryAsync(query, queryType, CancellationToken.None);

        public Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType, CancellationToken cancellationToken)
            => QueryAsync(query, queryType, QueryClass.IN, cancellationToken);

        public Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType, QueryClass queryClass)
            => QueryAsync(query, queryType, queryClass, CancellationToken.None);

        public Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType, QueryClass queryClass, CancellationToken cancellationToken)
            => QueryAsync(new DnsQuestion(query, queryType, queryClass), cancellationToken);

        private async Task<IDnsQueryResponse> QueryAsync(DnsQuestion question, CancellationToken cancellationToken)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            var head = new DnsRequestHeader(GetNextUniqueId(), Recursion, DnsOpCode.Query);
            var request = new DnsRequestMessage(head, question);
            var handler = UseTcpOnly ? _tcpFallbackHandler : _messageHandler;

            if (_cache.Enabled)
            {
                var cacheKey = ResponseCache.GetCacheKey(question);
                var item = _cache.Get(cacheKey);
                if (item == null)
                {
                    item = await ResolveQueryAsync(handler, request, cancellationToken).ConfigureAwait(false);
                    _cache.Add(cacheKey, item);
                }

                return item;
            }
            else
            {
                return await ResolveQueryAsync(handler, request, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<IDnsQueryResponse> ResolveQueryAsync(DnsMessageHandler handler, DnsRequestMessage request, CancellationToken cancellationToken, Audit continueAudit = null)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var audit = continueAudit ?? new Audit();
            var servers = GetNextServers();

            foreach (var serverInfo in servers)
            {
                var tries = 0;
                do
                {
                    tries++;

                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (EnableAuditTrail)
                        {
                            audit.StartTimer();
                        }

                        DnsResponseMessage response;
                        Action onCancel = () => { };
                        Task<DnsResponseMessage> resultTask = handler.QueryAsync(serverInfo.Endpoint, request, cancellationToken, (cancel) =>
                        {
                            onCancel = cancel;
                        });

                        if (Timeout != s_infiniteTimeout || (cancellationToken != CancellationToken.None && cancellationToken.CanBeCanceled))
                        {
                            var cts = new CancellationTokenSource(Timeout);
                            CancellationTokenSource linkedCts = null;
                            if (cancellationToken != CancellationToken.None)
                            {
                                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
                            }
                            using (cts)
                            using (linkedCts)
                            {
                                response = await resultTask.WithCancellation((linkedCts ?? cts).Token, onCancel).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            response = await resultTask.ConfigureAwait(false);
                        }

                        if (response.Header.ResultTruncated && UseTcpFallback && !handler.GetType().Equals(typeof(DnsTcpMessageHandler)))
                        {
                            if (EnableAuditTrail)
                            {
                                audit.AuditTruncatedRetryTcp();
                            }

                            return await ResolveQueryAsync(_tcpFallbackHandler, request, cancellationToken, audit).ConfigureAwait(false);
                        }

                        if (EnableAuditTrail)
                        {
                            audit.AuditResolveServers(_endpoints.Count);
                            audit.AuditResponseHeader(response.Header);
                        }

                        if (response.Header.ResponseCode != DnsResponseCode.NoError)
                        {
                            if (EnableAuditTrail)
                            {
                                audit.AuditResponseError(response.Header.ResponseCode);
                            }

                            if (ThrowDnsErrors)
                            {
                                throw new DnsResponseException(response.Header.ResponseCode);
                            }
                        }

                        HandleOptRecords(audit, serverInfo, response);

                        DnsQueryResponse queryResponse = response.AsQueryResponse(serverInfo.Clone());

                        if (EnableAuditTrail)
                        {
                            audit.AuditResponse(queryResponse);
                            audit.AuditEnd(queryResponse);
                            queryResponse.AuditTrail = audit.Build();
                        }

                        Interlocked.Increment(ref StaticLog.SyncResolveQueryCount);
                        Interlocked.Add(ref StaticLog.SyncResolveQueryTries, tries);
                        return queryResponse;
                    }
                    catch (DnsResponseException ex)
                    {
                        audit.AuditException(ex);
                        ex.AuditTrail = audit.Build();
                        throw;
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressFamilyNotSupported)
                    {
                        // this socket error might indicate the server endpoint is actually bad and should be ignored in future queries.
                        DisableServer(serverInfo);
                        break;
                    }
                    catch (Exception ex) when (ex is TimeoutException || handler.IsTransientException(ex))
                    {
                        // our timeout got eventually triggered by the a task cancelation token, throw OCE instead...
                        if (cancellationToken.IsCancellationRequested)
                        {
                            throw new OperationCanceledException(cancellationToken);
                        }

                        DisableServer(serverInfo);
                    }
                    catch (Exception ex)
                    {
                        DisableServer(serverInfo);

                        var handleEx = ex;
                        var agg = ex as AggregateException;
                        if (agg != null)
                        {
                            if (agg.InnerExceptions.Any(e => e is TimeoutException || handler.IsTransientException(e)))
                            {
                                continue;
                            }

                            handleEx = agg.InnerException;
                        }

                        if (handleEx is OperationCanceledException || handleEx is TaskCanceledException)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                throw new OperationCanceledException(cancellationToken);
                            }

                            continue;
                        }

                        audit.AuditException(ex);
                        throw new DnsResponseException("Unhandled exception", ex)
                        {
                            AuditTrail = audit.Build()
                        };
                    }
                } while (tries <= Retries && !cancellationToken.IsCancellationRequested && serverInfo.Enabled);
            }

            throw new DnsResponseException(
                DnsResponseCode.ConnectionTimeout,
                $"No connection could be established to any of the following name servers: {string.Join(", ", NameServers)}.")
            {
                AuditTrail = audit.Build()
            };
        }

        private void HandleOptRecords(Audit audit, NameServer serverInfo, DnsResponseMessage response)
        {
            var opt = response.Additionals.OfType<OptRecord>().FirstOrDefault();
            if (opt != null)
            {
                if (EnableAuditTrail)
                {
                    audit.AuditOptPseudo();
                }

                serverInfo.SupportedUdpPayloadSize = opt.UdpSize;

                // TODO: handle opt records and remove them later
                response.Additionals.Remove(opt);

                if (EnableAuditTrail)
                {
                    audit.AuditEdnsOpt(opt.UdpSize, opt.Version, opt.ResponseCodeEx);
                }
            }
        }

        private IEnumerable<NameServer> GetNextServers()
        {
            IEnumerable<NameServer> servers = null;
            if (_endpoints.Count > 1)
            {
                servers = _endpoints.Where(p => p.Enabled);

                NameServer server = null;
                if (_endpoints.TryDequeue(out server))
                {
                    _endpoints.Enqueue(server);
                }
            }
            else
            {
                servers = _endpoints;
            }

            return servers;
        }

        private void DisableServer(NameServer server)
        {
            lock (_endpointLock)
            {
                server.Enabled = false;

                if (_endpoints.Count(p => p.Enabled == true) == 0)
                {
                    // reset all servers to try again...
                    _endpoints.ToList().ForEach(p => p.Enabled = true);
                }
            }
        }

        private ushort GetNextUniqueId()
        {
            if (_uniqueId == ushort.MaxValue || _uniqueId == 0)
            {
                _uniqueId = (ushort)_random.Next(ushort.MaxValue / 2);
            }

            return unchecked((ushort)Interlocked.Increment(ref _uniqueId));
        }

        private class Audit
        {
            private static readonly int s_printOffset = -32;
            private StringBuilder _auditWriter = new StringBuilder();
            private Stopwatch _swatch;

            public Audit()
            {
            }

            public void StartTimer()
            {
                _swatch = Stopwatch.StartNew();
                _swatch.Restart();
            }

            public void AuditResolveServers(int count)
            {
                _auditWriter.AppendLine($"; ({count} server found)");
            }

            public string Build()
            {
                return _auditWriter.ToString();
            }

            public void AuditTruncatedRetryTcp()
            {
                _auditWriter.AppendLine(";; Truncated, retrying in TCP mode.");
                _auditWriter.AppendLine();
            }

            public void AuditResponseError(DnsResponseCode responseCode)
            {
                _auditWriter.AppendLine($";; ERROR: {DnsResponseCodeText.GetErrorText(responseCode)}");
            }

            public void AuditOptPseudo()
            {
                _auditWriter.AppendLine(";; OPT PSEUDOSECTION:");
            }

            public void AuditResponseHeader(DnsResponseHeader header)
            {
                _auditWriter.AppendLine(";; Got answer:");
                _auditWriter.AppendLine(header.ToString());
                if (header.RecursionDesired && !header.RecursionAvailable)
                {
                    _auditWriter.AppendLine(";; WARNING: recursion requested but not available");
                }
                _auditWriter.AppendLine();
            }

            public void AuditEdnsOpt(short udpSize, byte version, DnsResponseCode responseCodeEx)
            {
                // TODO: flags
                _auditWriter.AppendLine($"; EDNS: version: {version}, flags:; udp: {udpSize}");
            }

            public void AuditResponse(IDnsQueryResponse queryResponse)
            {
                if (queryResponse.Questions.Count > 0)
                {
                    _auditWriter.AppendLine(";; QUESTION SECTION:");
                    foreach (var question in queryResponse.Questions)
                    {
                        _auditWriter.AppendLine(question.ToString(s_printOffset));
                    }
                    _auditWriter.AppendLine();
                }

                if (queryResponse.Answers.Count > 0)
                {
                    _auditWriter.AppendLine(";; ANSWER SECTION:");
                    foreach (var answer in queryResponse.Answers)
                    {
                        _auditWriter.AppendLine(answer.ToString(s_printOffset));
                    }
                    _auditWriter.AppendLine();
                }

                if (queryResponse.Authorities.Count > 0)
                {
                    _auditWriter.AppendLine(";; AUTHORITIES SECTION:");
                    foreach (var auth in queryResponse.Authorities)
                    {
                        _auditWriter.AppendLine(auth.ToString(s_printOffset));
                    }
                    _auditWriter.AppendLine();
                }

                if (queryResponse.Additionals.Count > 0)
                {
                    _auditWriter.AppendLine(";; ADDITIONALS SECTION:");
                    foreach (var additional in queryResponse.Additionals)
                    {
                        _auditWriter.AppendLine(additional.ToString(s_printOffset));
                    }
                    _auditWriter.AppendLine();
                }
            }

            public void AuditEnd(DnsQueryResponse queryResponse)
            {
                var elapsed = _swatch.ElapsedMilliseconds;
                _auditWriter.AppendLine($";; Query time: {elapsed} msec");
                _auditWriter.AppendLine($";; SERVER: {queryResponse.NameServer.Endpoint.Address}#{queryResponse.NameServer.Endpoint.Port}");
                _auditWriter.AppendLine($";; WHEN: {DateTime.Now.ToString("R")}");
                _auditWriter.AppendLine($";; MSG SIZE  rcvd: {queryResponse.MessageSize}");
            }

            public void AuditException(Exception ex)
            {
                var dnsEx = ex as DnsResponseException;
                var aggEx = ex as AggregateException;
                if (dnsEx != null)
                {
                    _auditWriter.AppendLine($";; Error: {DnsResponseCodeText.GetErrorText(dnsEx.Code)} {dnsEx.InnerException?.Message ?? dnsEx.Message}");
                }
                else if (aggEx != null)
                {
                    _auditWriter.AppendLine($";; Error: {aggEx.InnerException?.Message ?? aggEx.Message}");
                }
                else
                {
                    _auditWriter.AppendLine($";; Error: {ex.Message}");
                }

                if (Debugger.IsAttached)
                {
                    _auditWriter.AppendLine(ex.ToString());
                }
            }
        }
    }
}