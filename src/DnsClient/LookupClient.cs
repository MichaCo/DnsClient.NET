using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DnsClient.Protocol.Options;

namespace DnsClient
{
    /// <summary>
    /// The <see cref="LookupClient"/> is the main query class of this library and should be used for any kind of DNS lookup query.
    /// <para>
    /// It implements <see cref="ILookupClient"/> and <see cref="IDnsQuery"/> which contains a number of extension methods, too.
    /// The extension methods internally all invoke the standard <see cref="IDnsQuery"/> queries though.
    /// </para>
    /// </summary>
    /// <seealso cref="IDnsQuery"/>
    /// <seealso cref="ILookupClient"/>
    /// <example>
    /// A basic example wihtout specifying any DNS server, which will use the DNS server configured by your local network.
    /// <code>
    /// <![CDATA[
    /// var client = new LookupClient();
    /// var result = client.Query("google.com", QueryType.A);
    ///
    /// foreach (var aRecord in result.Answers.ARecords())
    /// {
    ///     Console.WriteLine(aRecord);
    /// }
    /// ]]>
    /// </code>
    /// </example>
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

        /// <inheritdoc />
        public bool UseTcpFallback { get; set; } = true;

        /// <inheritdoc />
        public bool UseTcpOnly { get; set; }

        /// <inheritdoc />
        public IReadOnlyCollection<NameServer> NameServers { get; }

        /// <inheritdoc />
        public bool EnableAuditTrail { get; set; } = false;

        /// <inheritdoc />
        public bool Recursion { get; set; } = true;

        /// <inheritdoc />
        public int Retries { get; set; } = 5;

        /// <inheritdoc />
        public bool ThrowDnsErrors { get; set; } = false;

        public bool RequestDnsSecRecords { get; set; } = false;

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public TimeSpan? MinimumCacheTimeout
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

        /// <summary>
        /// Creates a new instance of <see cref="LookupClient"/> without specifying any name server.
        /// This will implicitly use the name server(s) configured by the local network adapter.
        /// </summary>
        /// <remarks>
        /// This uses <see cref="NameServer.ResolveNameServers(bool, bool)"/>.
        /// The resulting list of name servers is highly dependent on the local network configuration and OS.
        /// </remarks>
        /// <example>
        /// In the following example, we will create a new <see cref="LookupClient"/> without explicitly defining any DNS server.
        /// This will use the DNS server configured by your local network.
        /// <code>
        /// <![CDATA[
        /// var client = new LookupClient();
        /// var result = client.Query("google.com", QueryType.A);
        ///
        /// foreach (var aRecord in result.Answers.ARecords())
        /// {
        ///     Console.WriteLine(aRecord);
        /// }
        /// ]]>
        /// </code>
        /// </example>
        public LookupClient()
            : this(NameServer.ResolveNameServers()?.ToArray())
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="LookupClient"/> with one or more DNS servers identified by their <see cref="IPAddress"/>.
        /// The default port <c>53</c> will be used for all <see cref="IPAddress"/>s provided.
        /// </summary>
        /// <param name="nameServers">The <see cref="IPAddress"/>(s) to be used by this <see cref="LookupClient"/> instance.</param>
        /// <example>
        /// To connect to one or more DNS server using the default port, we can use this overload:
        /// <code>
        /// <![CDATA[
        /// // configuring the client to use google's public IPv4 DNS servers.
        /// var client = new LookupClient(IPAddress.Parse("8.8.8.8"), IPAddress.Parse("8.8.4.4"));
        /// ]]>
        /// </code>
        /// </example>
        public LookupClient(params IPAddress[] nameServers)
            : this(nameServers?.Select(p => new IPEndPoint(p, NameServer.DefaultPort)).ToArray())
        {
        }

        /// <summary>
        /// Create a new instance of <see cref="LookupClient"/> with one DNS server defined by <paramref name="address"/> and <paramref name="port"/>.
        /// </summary>
        /// <param name="address">The <see cref="IPAddress"/> of the DNS server.</param>
        /// <param name="port">The port of the DNS server.</param>
        /// <example>
        /// In case you want to connect to one specific DNS server which does not run on the default port <c>53</c>, you can do so like in the following example:
        /// <code>
        /// <![CDATA[
        /// var client = new LookupClient(IPAddress.Parse("127.0.0.1"), 8600);
        /// ]]>
        /// </code>
        /// </example>
        public LookupClient(IPAddress address, int port)
           : this(new IPEndPoint(address, port))
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="LookupClient"/> with one or more <see cref="IPAddress"/> and port combination
        /// stored in <see cref="IPEndPoint"/>(s).
        /// </summary>
        /// <param name="nameServers">The <see cref="IPEndPoint"/>(s) to be used by this <see cref="LookupClient"/> instance.</param>
        /// <example>
        /// In this example, we instantiate a new <see cref="IPEndPoint"/> using an <see cref="IPAddress"/> and custom port which is different than the default port <c>53</c>.
        /// <code>
        /// <![CDATA[
        /// // Using localhost and port 8600 to connect to a Consul agent.
        /// var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8600);
        /// var client = new LookupClient(endpoint);
        /// ]]>
        /// </code>
        /// <para>
        /// The <see cref="NameServer"/> class also contains pre defined <see cref="IPEndPoint"/>s for the public google DNS servers, which can be used as follows:
        /// <code>
        /// <![CDATA[
        /// var client = new LookupClient(NameServer.GooglePublicDns, NameServer.GooglePublicDnsIPv6);
        /// ]]>
        /// </code>
        /// </para>
        /// </example>
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

        /// <inheritdoc />
        public IDnsQueryResponse QueryReverse(IPAddress ipAddress)
        {
            if (ipAddress == null)
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            var arpa = ipAddress.GetArpaName();
            return Query(arpa, QueryType.PTR, QueryClass.IN);
        }

        /// <inheritdoc />
        /// <remarks>
        /// The behavior of the query can be controlled by the properties of this <see cref="LookupClient"/> instance.
        /// <see cref="Recursion"/> for example can be disabled and would instruct the DNS server to return no additional records.
        /// </remarks>
        public Task<IDnsQueryResponse> QueryReverseAsync(IPAddress ipAddress)
            => QueryReverseAsync(ipAddress, CancellationToken.None);

        /// <inheritdoc />
        /// <remarks>
        /// The behavior of the query can be controlled by the properties of this <see cref="LookupClient"/> instance.
        /// <see cref="Recursion"/> for example can be disabled and would instruct the DNS server to return no additional records.
        /// </remarks>
        public Task<IDnsQueryResponse> QueryReverseAsync(IPAddress ipAddress, CancellationToken cancellationToken)
        {
            if (ipAddress == null)
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            var arpa = ipAddress.GetArpaName();
            return QueryAsync(arpa, QueryType.PTR, QueryClass.IN, cancellationToken);
        }

        /// <inheritdoc />
        /// <remarks>
        /// The behavior of the query can be controlled by the properties of this <see cref="LookupClient"/> instance.
        /// <see cref="Recursion"/> for example can be disabled and would instruct the DNS server to return no additional records.
        /// </remarks>
        public IDnsQueryResponse Query(string query, QueryType queryType)
            => Query(query, queryType, QueryClass.IN);

        /// <inheritdoc />
        /// <remarks>
        /// The behavior of the query can be controlled by the properties of this <see cref="LookupClient"/> instance.
        /// <see cref="Recursion"/> for example can be disabled and would instruct the DNS server to return no additional records.
        /// </remarks>
        public IDnsQueryResponse Query(string query, QueryType queryType, QueryClass queryClass)
            => Query(new DnsQuestion(query, queryType, queryClass));

        private IDnsQueryResponse Query(DnsQuestion question)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            var head = new DnsRequestHeader(GetNextUniqueId(), Recursion, DnsOpCode.Query, RequestDnsSecRecords);
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

                        ////Interlocked.Increment(ref StaticLog.ResolveQueryCount);
                        ////Interlocked.Add(ref StaticLog.ResolveQueryTries, tries);
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

        /// <inheritdoc />
        /// <remarks>
        /// The behavior of the query can be controlled by the properties of this <see cref="LookupClient"/> instance.
        /// <see cref="Recursion"/> for example can be disabled and would instruct the DNS server to return no additional records.
        /// </remarks>
        public Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType)
            => QueryAsync(query, queryType, CancellationToken.None);

        /// <inheritdoc />
        /// <remarks>
        /// The behavior of the query can be controlled by the properties of this <see cref="LookupClient"/> instance.
        /// <see cref="Recursion"/> for example can be disabled and would instruct the DNS server to return no additional records.
        /// </remarks>
        public Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType, CancellationToken cancellationToken)
            => QueryAsync(query, queryType, QueryClass.IN, cancellationToken);

        /// <inheritdoc />
        /// <remarks>
        /// The behavior of the query can be controlled by the properties of this <see cref="LookupClient"/> instance.
        /// <see cref="Recursion"/> for example can be disabled and would instruct the DNS server to return no additional records.
        /// </remarks>
        public Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType, QueryClass queryClass)
            => QueryAsync(query, queryType, queryClass, CancellationToken.None);

        /// <inheritdoc />
        /// <remarks>
        /// The behavior of the query can be controlled by the properties of this <see cref="LookupClient"/> instance.
        /// <see cref="Recursion"/> for example can be disabled and would instruct the DNS server to return no additional records.
        /// </remarks>
        public Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType, QueryClass queryClass, CancellationToken cancellationToken)
            => QueryAsync(new DnsQuestion(query, queryType, queryClass), cancellationToken);

        private async Task<IDnsQueryResponse> QueryAsync(DnsQuestion question, CancellationToken cancellationToken)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            var head = new DnsRequestHeader(GetNextUniqueId(), Recursion, DnsOpCode.Query, RequestDnsSecRecords);
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

                        ////Interlocked.Increment(ref StaticLog.ResolveQueryCount);
                        ////Interlocked.Add(ref StaticLog.ResolveQueryTries, tries);
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
                        if (ex is AggregateException agg)
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

                if (_endpoints.TryDequeue(out NameServer server))
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
                _auditWriter.AppendLine($";; WHEN: {DateTime.UtcNow.ToString("ddd MMM dd HH:mm:ss K yyyy", CultureInfo.InvariantCulture)}");
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