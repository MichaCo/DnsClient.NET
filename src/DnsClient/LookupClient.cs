using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DnsClient.Internal;
using DnsClient.Protocol;
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
    /// A basic example without specifying any DNS server, which will use the DNS server configured by your local network.
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
        private const int LogEventStartQuery = 1;
        private const int LogEventQuery = 2;
        private const int LogEventQueryCachedResult = 3;
        private const int LogEventQueryTruncated = 5;
        private const int LogEventQuerySuccess = 10;
        private const int LogEventQueryReturnResponseError = 11;
        private const int LogEventQuerySuccessEmpty = 12;

        private const int LogEventQueryRetryErrorNextServer = 20;
        private const int LogEventQueryRetryErrorSameServer = 21;

        private const int LogEventQueryFail = 90;
        private const int LogEventQueryBadTruncation = 91;

        private const int LogEventResponseOpt = 31;
        private const int LogEventResponseMissingOpt = 80;

        private readonly LookupClientOptions _originalOptions;
        private readonly DnsMessageHandler _messageHandler;
        private readonly DnsMessageHandler _tcpFallbackHandler;
        private readonly ILogger _logger;
        private readonly SkipWorker _skipper = null;

        private IReadOnlyCollection<NameServer> _resolvedNameServers;

        /// <inheritdoc/>
        public IReadOnlyCollection<NameServer> NameServers => Settings.NameServers;

        /// <inheritdoc/>
        public LookupClientSettings Settings { get; private set; }

        #region obsolete properties
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        public TimeSpan? MinimumCacheTimeout
        {
            get => Settings.MinimumCacheTimeout;
            set
            {
                if (Settings.MinimumCacheTimeout != value)
                {
                    _originalOptions.MinimumCacheTimeout = value;
                    Settings = new LookupClientSettings(_originalOptions, Settings.NameServers);
                }
            }
        }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        public bool UseTcpFallback
        {
            get => Settings.UseTcpFallback;
            set
            {
                if (Settings.UseTcpFallback != value)
                {
                    _originalOptions.UseTcpFallback = value;
                    Settings = new LookupClientSettings(_originalOptions, Settings.NameServers);
                }
            }
        }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        public bool UseTcpOnly
        {
            get => Settings.UseTcpOnly;
            set
            {
                if (Settings.UseTcpOnly != value)
                {
                    _originalOptions.UseTcpOnly = value;
                    Settings = new LookupClientSettings(_originalOptions, Settings.NameServers);
                }
            }
        }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        public bool EnableAuditTrail
        {
            get => Settings.EnableAuditTrail;
            set
            {
                if (Settings.EnableAuditTrail != value)
                {
                    _originalOptions.EnableAuditTrail = value;
                    Settings = new LookupClientSettings(_originalOptions, Settings.NameServers);
                }
            }
        }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        public bool Recursion
        {
            get => Settings.Recursion;
            set
            {
                if (Settings.Recursion != value)
                {
                    _originalOptions.Recursion = value;
                    Settings = new LookupClientSettings(_originalOptions, Settings.NameServers);
                }
            }
        }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        public int Retries
        {
            get => Settings.Retries;
            set
            {
                if (Settings.Retries != value)
                {
                    _originalOptions.Retries = value;
                    Settings = new LookupClientSettings(_originalOptions, Settings.NameServers);
                }
            }
        }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        public bool ThrowDnsErrors
        {
            get => Settings.ThrowDnsErrors;
            set
            {
                if (Settings.ThrowDnsErrors != value)
                {
                    _originalOptions.ThrowDnsErrors = value;
                    Settings = new LookupClientSettings(_originalOptions, Settings.NameServers);
                }
            }
        }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        public TimeSpan Timeout
        {
            get => Settings.Timeout;
            set
            {
                if (Settings.Timeout != value)
                {
                    _originalOptions.Timeout = value;
                    Settings = new LookupClientSettings(_originalOptions, Settings.NameServers);
                }
            }
        }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        public bool UseCache
        {
            //TODO: change logic with options/settings - UseCache is just a setting, cache can still be enabled
            get => Settings.UseCache;
            set
            {
                if (Settings.UseCache != value)
                {
                    _originalOptions.UseCache = value;
                    Settings = new LookupClientSettings(_originalOptions, Settings.NameServers);
                }
            }
        }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        public bool UseRandomNameServer
        {
            get => Settings.UseRandomNameServer;
            set
            {
                if (Settings.UseRandomNameServer != value)
                {
                    _originalOptions.UseRandomNameServer = value;
                    Settings = new LookupClientSettings(_originalOptions, Settings.NameServers);
                }
            }
        }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        public bool ContinueOnDnsError
        {
            get => Settings.ContinueOnDnsError;
            set
            {
                if (Settings.ContinueOnDnsError != value)
                {
                    _originalOptions.ContinueOnDnsError = value;
                    Settings = new LookupClientSettings(_originalOptions, Settings.NameServers);
                }
            }
        }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        #endregion

        internal ResponseCache Cache { get; }

        /// <summary>
        /// Creates a new instance of <see cref="LookupClient"/> without specifying any name server.
        /// This will implicitly use the name server(s) configured by the local network adapter(s).
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
            : this(new LookupClientOptions())
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="LookupClient"/> with default settings and one or more DNS servers identified by their <see cref="IPAddress"/>.
        /// The default port <c>53</c> will be used for all <see cref="IPAddress"/>s provided.
        /// </summary>
        /// <param name="nameServers">The <see cref="IPAddress"/>(s) to be used by this <see cref="LookupClient"/> instance.</param>
        /// <example>
        /// Connecting to one or more DNS server using the default port:
        /// <code>
        /// <![CDATA[
        /// // configuring the client to use google's public IPv4 DNS servers.
        /// var client = new LookupClient(IPAddress.Parse("8.8.8.8"), IPAddress.Parse("8.8.4.4"));
        /// ]]>
        /// </code>
        /// </example>
        /// <exception cref="ArgumentNullException">If <paramref name="nameServers"/>is <c>null</c>.</exception>
        public LookupClient(params IPAddress[] nameServers)
            : this(new LookupClientOptions(nameServers))
        {
        }

        /// <summary>
        /// Create a new instance of <see cref="LookupClient"/> with default settings and one DNS server defined by <paramref name="address"/> and <paramref name="port"/>.
        /// </summary>
        /// <param name="address">The <see cref="IPAddress"/> of the DNS server.</param>
        /// <param name="port">The port of the DNS server.</param>
        /// <example>
        /// Connecting to one specific DNS server which does not run on the default port <c>53</c>:
        /// <code>
        /// <![CDATA[
        /// var client = new LookupClient(IPAddress.Parse("127.0.0.1"), 8600);
        /// ]]>
        /// </code>
        /// </example>
        /// <exception cref="ArgumentNullException">If <paramref name="address"/>is <c>null</c>.</exception>
        public LookupClient(IPAddress address, int port)
           : this(new LookupClientOptions(new[] { new NameServer(address, port) }))
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="LookupClient"/> with default settings and the given name servers.
        /// </summary>
        /// <param name="nameServers">The <see cref="IPEndPoint"/>(s) to be used by this <see cref="LookupClient"/> instance.</param>
        /// <example>
        /// Connecting to one specific DNS server which does not run on the default port <c>53</c>:
        /// <code>
        /// <![CDATA[
        /// var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8600);
        /// var client = new LookupClient(endpoint);
        /// ]]>
        /// </code>
        /// <para>
        /// The <see cref="NameServer"/> class also contains predefined <see cref="IPEndPoint"/>s for the public Google DNS servers, which can be used as follows:
        /// <code>
        /// <![CDATA[
        /// var client = new LookupClient(NameServer.GooglePublicDns, NameServer.GooglePublicDnsIPv6);
        /// ]]>
        /// </code>
        /// </para>
        /// </example>
        /// <exception cref="ArgumentNullException">If <paramref name="nameServers"/>is <c>null</c>.</exception>
        public LookupClient(params IPEndPoint[] nameServers)
            : this(new LookupClientOptions(nameServers))
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="LookupClient"/> with default settings and the given name servers.
        /// </summary>
        /// <param name="nameServers">The <see cref="NameServer"/>(s) to be used by this <see cref="LookupClient"/> instance.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="nameServers"/>is <c>null</c>.</exception>
        public LookupClient(params NameServer[] nameServers)
            : this(new LookupClientOptions(nameServers))
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="LookupClient"/> with custom settings.
        /// </summary>
        /// <param name="options">The options to use with this <see cref="LookupClient"/> instance.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="options"/>is <c>null</c>.</exception>
        public LookupClient(LookupClientOptions options)
            : this(options, null, null)
        {
        }

        internal LookupClient(LookupClientOptions options, DnsMessageHandler udpHandler = null, DnsMessageHandler tcpHandler = null)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _originalOptions = options;
            _logger = Logging.LoggerFactory.CreateLogger(GetType().FullName);
            _messageHandler = udpHandler ?? new DnsUdpMessageHandler(true);
            _tcpFallbackHandler = tcpHandler ?? new DnsTcpMessageHandler();

            if (_messageHandler.Type != DnsMessageHandleType.UDP)
            {
                throw new ArgumentException("UDP message handler's type must be UDP.", nameof(udpHandler));
            }
            if (_tcpFallbackHandler.Type != DnsMessageHandleType.TCP)
            {
                throw new ArgumentException("TCP message handler's type must be TCP.", nameof(tcpHandler));
            }

            // Setting up name servers.
            // Using manually configured ones and/or auto resolved ones.
            IReadOnlyCollection<NameServer> servers = _originalOptions.NameServers?.ToArray() ?? new NameServer[0];

            if (options.AutoResolveNameServers)
            {
                _resolvedNameServers = NameServer.ResolveNameServers(skipIPv6SiteLocal: true, fallbackToGooglePublicDns: false);
                servers = servers.Concat(_resolvedNameServers).ToArray();

                // This will periodically get triggered on Query calls and
                // will perform the same check as on NetworkAddressChanged.
                // The event doesn't seem to get fired on Linux for example...
                // TODO: Maybe there is a better way, but this will work for now.
                _skipper = new SkipWorker(
                () =>
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Checking resolved name servers for network changes...");
                    }

                    CheckResolvedNameservers();
                },
                skip: 60 * 1000);
            }

            servers = NameServer.ValidateNameServers(servers, _logger);

            Settings = new LookupClientSettings(options, servers);
            Cache = new ResponseCache(true, Settings.MinimumCacheTimeout, Settings.MaximumCacheTimeout, Settings.FailedResultsCacheDuration);
        }

        private void CheckResolvedNameservers()
        {
            try
            {
                var newServers = NameServer.ResolveNameServers(skipIPv6SiteLocal: true, fallbackToGooglePublicDns: false);

                if (newServers == null || newServers.Count == 0)
                {
                    _logger.LogWarning("Could not resolve any name servers, keeping current configuration.");
                    return;
                }

                if (_resolvedNameServers.SequenceEqual(newServers))
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("No name server changes detected, still using {0}", string.Join(",", newServers));
                    }
                    return;
                }

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Found changes in local network, configured name servers now are: {0}", string.Join(",", newServers));
                }

                _resolvedNameServers = newServers;
                var servers = _originalOptions.NameServers.Concat(_resolvedNameServers).ToArray();
                Settings = new LookupClientSettings(_originalOptions, servers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error trying to resolve name servers.");
            }
        }

        /// <inheritdoc/>
        public IDnsQueryResponse QueryReverse(IPAddress ipAddress)
            => Query(GetReverseQuestion(ipAddress));

        /// <inheritdoc/>
        public IDnsQueryResponse QueryReverse(IPAddress ipAddress, DnsQueryAndServerOptions queryOptions)
            => Query(GetReverseQuestion(ipAddress), queryOptions);

        /// <inheritdoc/>
        public Task<IDnsQueryResponse> QueryReverseAsync(IPAddress ipAddress, CancellationToken cancellationToken = default)
            => QueryAsync(GetReverseQuestion(ipAddress), cancellationToken: cancellationToken);

        /// <inheritdoc/>
        public Task<IDnsQueryResponse> QueryReverseAsync(IPAddress ipAddress, DnsQueryAndServerOptions queryOptions, CancellationToken cancellationToken = default)
            => QueryAsync(GetReverseQuestion(ipAddress), queryOptions, cancellationToken);

        /// <inheritdoc/>
        public IDnsQueryResponse Query(string query, QueryType queryType, QueryClass queryClass = QueryClass.IN)
            => Query(new DnsQuestion(query, queryType, queryClass));

        /// <inheritdoc/>
        public IDnsQueryResponse Query(DnsQuestion question)
        {
            if (question is null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            var settings = GetSettings();
            return QueryInternal(question, settings, settings.ShuffleNameServers());
        }

        /// <inheritdoc/>
        public IDnsQueryResponse Query(DnsQuestion question, DnsQueryAndServerOptions queryOptions)
        {
            if (question is null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            if (queryOptions is null)
            {
                throw new ArgumentNullException(nameof(queryOptions));
            }

            var settings = GetSettings(queryOptions);
            return QueryInternal(question, settings, settings.ShuffleNameServers());
        }

        /// <inheritdoc/>
        public IDnsQueryResponse QueryCache(string query, QueryType queryType, QueryClass queryClass = QueryClass.IN)
            => QueryCache(new DnsQuestion(query, queryType, queryClass));

        /// <inheritdoc/>
        public IDnsQueryResponse QueryCache(DnsQuestion question)
        {
            if (question is null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            var settings = GetSettings();
            return QueryCache(question, settings);
        }

        /// <inheritdoc/>
        public Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default)
            => QueryAsync(new DnsQuestion(query, queryType, queryClass), cancellationToken: cancellationToken);

        /// <inheritdoc/>
        public Task<IDnsQueryResponse> QueryAsync(DnsQuestion question, CancellationToken cancellationToken = default)
        {
            if (question is null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            var settings = GetSettings();
            return QueryInternalAsync(question, settings, settings.ShuffleNameServers(), cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public Task<IDnsQueryResponse> QueryAsync(DnsQuestion question, DnsQueryAndServerOptions queryOptions, CancellationToken cancellationToken = default)
        {
            if (question is null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            if (queryOptions is null)
            {
                throw new ArgumentNullException(nameof(queryOptions));
            }

            var settings = GetSettings(queryOptions);
            return QueryInternalAsync(question, settings, settings.ShuffleNameServers(), cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public IDnsQueryResponse QueryServer(IReadOnlyCollection<IPAddress> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN)
            => QueryServer(NameServer.Convert(servers), new DnsQuestion(query, queryType, queryClass));

        /// <inheritdoc/>
        public IDnsQueryResponse QueryServer(IReadOnlyCollection<IPEndPoint> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN)
            => QueryServer(NameServer.Convert(servers), new DnsQuestion(query, queryType, queryClass));

        /// <inheritdoc/>
        public IDnsQueryResponse QueryServer(IReadOnlyCollection<NameServer> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN)
            => QueryServer(servers, new DnsQuestion(query, queryType, queryClass));

        /// <inheritdoc/>
        public IDnsQueryResponse QueryServer(IReadOnlyCollection<NameServer> servers, DnsQuestion question)
        {
            if (servers == null)
            {
                throw new ArgumentNullException(nameof(servers));
            }

            if (servers.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(servers), "List of configured name servers must not be empty.");
            }

            servers = NameServer.ValidateNameServers(servers, _logger);
            return QueryInternal(question, Settings, servers);
        }

        /// <inheritdoc/>
        public IDnsQueryResponse QueryServer(IReadOnlyCollection<NameServer> servers, DnsQuestion question, DnsQueryOptions queryOptions)
        {
            if (servers == null)
            {
                throw new ArgumentNullException(nameof(servers));
            }

            if (servers.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(servers), "List of configured name servers must not be empty.");
            }

            servers = NameServer.ValidateNameServers(servers, _logger);
            return QueryInternal(question, queryOptions, servers);
        }

        /// <inheritdoc/>
        public Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<IPAddress> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default)
            => QueryServerAsync(NameServer.Convert(servers), new DnsQuestion(query, queryType, queryClass), cancellationToken);

        /// <inheritdoc/>
        public Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<IPEndPoint> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default)
            => QueryServerAsync(NameServer.Convert(servers), new DnsQuestion(query, queryType, queryClass), cancellationToken);

        /// <inheritdoc/>
        public Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<NameServer> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default)
            => QueryServerAsync(servers, new DnsQuestion(query, queryType, queryClass), cancellationToken);

        /// <inheritdoc/>
        public Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<NameServer> servers, DnsQuestion question, CancellationToken cancellationToken = default)
        {
            if (servers == null)
            {
                throw new ArgumentNullException(nameof(servers));
            }

            if (servers.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(servers), "List of configured name servers must not be empty.");
            }

            servers = NameServer.ValidateNameServers(servers, _logger);
            return QueryInternalAsync(question, Settings, servers, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<NameServer> servers, DnsQuestion question, DnsQueryOptions queryOptions, CancellationToken cancellationToken = default)
        {
            if (servers == null)
            {
                throw new ArgumentNullException(nameof(servers));
            }

            if (servers.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(servers), "List of configured name servers must not be empty.");
            }

            servers = NameServer.ValidateNameServers(servers, _logger);
            return QueryInternalAsync(question, queryOptions, servers, cancellationToken);
        }

        /// <inheritdoc/>
        public IDnsQueryResponse QueryServerReverse(IReadOnlyCollection<IPAddress> servers, IPAddress ipAddress)
            => QueryServerReverse(NameServer.Convert(servers), ipAddress);

        /// <inheritdoc/>
        public IDnsQueryResponse QueryServerReverse(IReadOnlyCollection<IPEndPoint> servers, IPAddress ipAddress)
            => QueryServerReverse(NameServer.Convert(servers), ipAddress);

        /// <inheritdoc/>
        public IDnsQueryResponse QueryServerReverse(IReadOnlyCollection<NameServer> servers, IPAddress ipAddress)
            => QueryServer(servers, GetReverseQuestion(ipAddress));

        /// <inheritdoc/>
        public IDnsQueryResponse QueryServerReverse(IReadOnlyCollection<NameServer> servers, IPAddress ipAddress, DnsQueryOptions queryOptions)
            => QueryServer(servers, GetReverseQuestion(ipAddress), queryOptions);

        /// <inheritdoc/>
        public Task<IDnsQueryResponse> QueryServerReverseAsync(IReadOnlyCollection<IPAddress> servers, IPAddress ipAddress, CancellationToken cancellationToken = default)
            => QueryServerReverseAsync(NameServer.Convert(servers), ipAddress, cancellationToken: cancellationToken);

        /// <inheritdoc/>
        public Task<IDnsQueryResponse> QueryServerReverseAsync(IReadOnlyCollection<IPEndPoint> servers, IPAddress ipAddress, CancellationToken cancellationToken = default)
            => QueryServerReverseAsync(NameServer.Convert(servers), ipAddress, cancellationToken: cancellationToken);

        /// <inheritdoc/>
        public Task<IDnsQueryResponse> QueryServerReverseAsync(IReadOnlyCollection<NameServer> servers, IPAddress ipAddress, CancellationToken cancellationToken = default)
            => QueryServerAsync(servers, GetReverseQuestion(ipAddress), cancellationToken);

        /// <inheritdoc/>
        public Task<IDnsQueryResponse> QueryServerReverseAsync(IReadOnlyCollection<NameServer> servers, IPAddress ipAddress, DnsQueryOptions queryOptions, CancellationToken cancellationToken = default)
            => QueryServerAsync(servers, GetReverseQuestion(ipAddress), queryOptions, cancellationToken);

        // For unit tests.
        internal DnsQueryAndServerSettings GetSettings(DnsQueryAndServerOptions queryOptions = null)
        {
            // Re-evaluating resolved nameservers here, seems the best place.
            if (_originalOptions.AutoResolveNameServers)
            {
                _skipper?.MaybeDoWork();
            }

            if (queryOptions == null)
            {
                return Settings;
            }

            if (queryOptions.NameServers == null || queryOptions.NameServers.Count == 0)
            {
                // fallback to already configured nameservers in case none are specified.
                return new DnsQueryAndServerSettings(queryOptions, Settings.NameServers);
            }

            return queryOptions;
        }

        private IDnsQueryResponse QueryInternal(DnsQuestion question, DnsQuerySettings queryOptions, IReadOnlyCollection<NameServer> servers)
        {
            if (servers == null)
            {
                throw new ArgumentNullException(nameof(servers));
            }
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }
            if (queryOptions == null)
            {
                throw new ArgumentNullException(nameof(queryOptions));
            }
            if (servers.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(servers), "List of configured name servers must not be empty.");
            }

            var head = new DnsRequestHeader(queryOptions.Recursion, DnsOpCode.Query);
            var request = new DnsRequestMessage(head, question, queryOptions);
            var handler = queryOptions.UseTcpOnly ? _tcpFallbackHandler : _messageHandler;
            var audit = queryOptions.EnableAuditTrail ? new LookupClientAudit(queryOptions) : null;

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(LogEventStartQuery, "Begin query [{0}] via {1} => {2} on [{3}].", head, handler.Type, question, string.Join(", ", servers));
            }

            var result = ResolveQuery(servers.ToList(), queryOptions, handler, request, audit);
            if (!(result is TruncatedQueryResponse))
            {
                return result;
            }

            if (!queryOptions.UseTcpFallback)
            {
                throw new DnsResponseException(DnsResponseCode.Unassigned, "Response was truncated and UseTcpFallback is disabled, unable to resolve the question.")
                {
                    AuditTrail = audit?.Build(result)
                };
            }

            request.Header.RefreshId();
            var tcpResult = ResolveQuery(servers.ToList(), queryOptions, _tcpFallbackHandler, request, audit);
            if (tcpResult is TruncatedQueryResponse)
            {
                throw new DnsResponseException("Unexpected truncated result from TCP response.")
                {
                    AuditTrail = audit?.Build(tcpResult)
                };
            }

            return tcpResult;
        }

        private async Task<IDnsQueryResponse> QueryInternalAsync(DnsQuestion question, DnsQuerySettings queryOptions, IReadOnlyCollection<NameServer> servers, CancellationToken cancellationToken = default)
        {
            if (servers == null)
            {
                throw new ArgumentNullException(nameof(servers));
            }
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }
            if (queryOptions == null)
            {
                throw new ArgumentNullException(nameof(queryOptions));
            }
            if (servers.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(servers), "List of configured name servers must not be empty.");
            }

            var head = new DnsRequestHeader(queryOptions.Recursion, DnsOpCode.Query);
            var request = new DnsRequestMessage(head, question, queryOptions);
            var handler = queryOptions.UseTcpOnly ? _tcpFallbackHandler : _messageHandler;
            var audit = queryOptions.EnableAuditTrail ? new LookupClientAudit(queryOptions) : null;

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(LogEventStartQuery, "Begin query [{0}] via {1} => {2} on [{3}].", head, handler.Type, question, string.Join(", ", servers));
            }

            var result = await ResolveQueryAsync(servers.ToList(), queryOptions, handler, request, audit, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!(result is TruncatedQueryResponse))
            {
                return result;
            }

            if (!queryOptions.UseTcpFallback)
            {
                throw new DnsResponseException(DnsResponseCode.Unassigned, "Response was truncated and UseTcpFallback is disabled, unable to resolve the question.")
                {
                    AuditTrail = audit?.Build(result)
                };
            }

            request.Header.RefreshId();
            var tcpResult = await ResolveQueryAsync(servers.ToList(), queryOptions, _tcpFallbackHandler, request, audit, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (tcpResult is TruncatedQueryResponse)
            {
                throw new DnsResponseException("Unexpected truncated result from TCP response.")
                {
                    AuditTrail = audit?.Build(tcpResult)
                };
            }

            return tcpResult;
        }

        private IDnsQueryResponse ResolveQuery(
            IReadOnlyList<NameServer> servers,
            DnsQuerySettings settings,
            DnsMessageHandler handler,
            DnsRequestMessage request,
            LookupClientAudit audit = null)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            for (var serverIndex = 0; serverIndex < servers.Count; serverIndex++)
            {
                var serverInfo = servers[serverIndex];
                var isLastServer = serverIndex >= servers.Count - 1;

                if (serverIndex > 0)
                {
                    request.Header.RefreshId();
                }

                if (settings.EnableAuditTrail && !isLastServer)
                {
                    audit?.AuditRetryNextServer();
                }

                var cacheKey = string.Empty;
                if (settings.UseCache)
                {
                    cacheKey = ResponseCache.GetCacheKey(request.Question);

                    if (TryGetCachedResult(cacheKey, request, settings, out var cachedResponse))
                    {
                        return cachedResponse;
                    }
                }

                var tries = 0;
                do
                {
                    if (tries > 0)
                    {
                        request.Header.RefreshId();
                    }

                    tries++;
                    var isLastTry = tries > settings.Retries;

                    IDnsQueryResponse lastQueryResponse = null;

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug(
                            LogEventQuery,
                            "TryResolve {0} via {1} => {2} on {3}, try {4}/{5}.",
                            request.Header.Id,
                            handler.Type,
                            request.Question,
                            serverInfo,
                            tries,
                            settings.Retries + 1);
                    }

                    try
                    {
                        audit?.StartTimer();

                        DnsResponseMessage response = handler.Query(serverInfo.IPEndPoint, request, settings.Timeout);

                        lastQueryResponse = ProcessResponseMessage(
                            audit,
                            request,
                            response,
                            settings,
                            serverInfo,
                            handler.Type,
                            servers.Count,
                            isLastServer,
                            out var retryQueryHint);

                        if (lastQueryResponse is TruncatedQueryResponse)
                        {
                            // Return right away and try to fallback to TCP.
                            return lastQueryResponse;
                        }

                        audit?.AuditEnd(lastQueryResponse, serverInfo);
                        audit?.Build(lastQueryResponse);

                        if (lastQueryResponse.HasError)
                        {
                            throw new DnsResponseException((DnsResponseCode)response.Header.ResponseCode)
                            {
                                AuditTrail = audit?.Build()
                            };
                        }

                        if (retryQueryHint == HandleError.RetryNextServer)
                        {
                            break;
                        }

                        if (settings.UseCache)
                        {
                            Cache.Add(cacheKey, lastQueryResponse);
                        }

                        return lastQueryResponse;
                    }
                    catch (DnsXidMismatchException ex)
                    {
                        var handle = HandleDnsXidMismatchException(ex, request, settings, handler.Type, isLastServer, isLastTry, tries);

                        if (handle == HandleError.RetryCurrentServer)
                        {
                            continue;
                        }
                        else if (handle == HandleError.RetryNextServer)
                        {
                            break;
                        }

                        throw;
                    }
                    catch (DnsResponseParseException ex)
                    {
                        var handle = HandleDnsResponeParseException(ex, request, handler.Type, isLastServer: isLastServer);
                        if (handle == HandleError.RetryNextServer)
                        {
                            break;
                        }
                        else if (handle == HandleError.ReturnResponse)
                        {
                            return new TruncatedQueryResponse();
                        }

                        throw;
                    }
                    catch (DnsResponseException ex)
                    {
                        // Response error handling and logging
                        var handle = HandleDnsResponseException(ex, request, settings, serverInfo, handler.Type, isLastServer: isLastServer, isLastTry: isLastTry, tries);

                        if (handle == HandleError.Throw)
                        {
                            throw;
                        }
                        else if (handle == HandleError.RetryCurrentServer)
                        {
                            continue;
                        }
                        else if (handle == HandleError.RetryNextServer)
                        {
                            break;
                        }

                        // This should not happen, but might if something upstream throws this exception before
                        // we got to parse the response, which, again, should not happen.
                        if (lastQueryResponse == null)
                        {
                            throw;
                        }

                        // If its the last server, return.
                        if (settings.UseCache && settings.CacheFailedResults)
                        {
                            Cache.Add(cacheKey, lastQueryResponse, true);
                        }

                        return lastQueryResponse;
                    }
                    catch (Exception ex) when (
                        ex is TimeoutException timeoutEx
                        || DnsMessageHandler.IsTransientException(ex)
                        || ex is OperationCanceledException)
                    {
                        var handle = HandleTimeoutException(ex, request, settings, serverInfo, handler.Type, isLastServer: isLastServer, isLastTry: isLastTry, currentTry: tries);

                        if (handle == HandleError.RetryCurrentServer)
                        {
                            continue;
                        }
                        else if (handle == HandleError.RetryNextServer)
                        {
                            break;
                        }

                        throw new DnsResponseException(
                            DnsResponseCode.ConnectionTimeout,
                            $"Query {request.Header.Id} => {request.Question} on {serverInfo} timed out or is a transient error.",
                            ex)
                        {
                            AuditTrail = audit?.Build()
                        };
                    }
                    catch (ArgumentException)
                    {
                        // Don't retry argument exceptions. This should not happen anyways unless we messed up somewhere.
                        throw;
                    }
                    catch (InvalidOperationException)
                    {
                        // Don't retry invalid ops exceptions. This should not happen anyways unless we messed up somewhere.
                        throw;
                    }
                    // Any other exception will not be retried on the same server. e.g. Socket Exceptions
                    catch (Exception ex)
                    {
                        audit?.AuditException(ex);

                        var handle = HandleUnhandledException(ex, request, serverInfo, handler.Type, isLastServer);

                        if (handle == HandleError.RetryNextServer)
                        {
                            break;
                        }
                        if (handle == HandleError.RetryCurrentServer)
                        {
                            continue;
                        }

                        throw new DnsResponseException(
                            DnsResponseCode.Unassigned,
                            $"Query {request.Header.Id} => {request.Question} on {serverInfo} failed with an error.",
                            ex)
                        {
                            AuditTrail = audit?.Build()
                        };
                    }
                } while (tries <= settings.Retries);
            } // next server

            // 1.3.0: With the error handling, this should never be reached.
            throw new DnsResponseException(DnsResponseCode.ConnectionTimeout, $"No connection could be established to any of the following name servers: {string.Join(", ", servers)}.")
            {
                AuditTrail = audit?.Build()
            };
        }

        private async Task<IDnsQueryResponse> ResolveQueryAsync(
            IReadOnlyList<NameServer> servers,
            DnsQuerySettings settings,
            DnsMessageHandler handler,
            DnsRequestMessage request,
            LookupClientAudit audit = null,
            CancellationToken cancellationToken = default)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            for (var serverIndex = 0; serverIndex < servers.Count; serverIndex++)
            {
                var serverInfo = servers[serverIndex];
                var isLastServer = serverIndex >= servers.Count - 1;

                if (serverIndex > 0)
                {
                    request.Header.RefreshId();
                }

                if (settings.EnableAuditTrail && serverIndex > 0 && !isLastServer)
                {
                    audit?.AuditRetryNextServer();
                }

                var cacheKey = string.Empty;
                if (settings.UseCache)
                {
                    cacheKey = ResponseCache.GetCacheKey(request.Question);

                    if (TryGetCachedResult(cacheKey, request, settings, out var cachedResponse))
                    {
                        return cachedResponse;
                    }
                }

                var tries = 0;
                do
                {
                    if (tries > 0)
                    {
                        request.Header.RefreshId();
                    }

                    tries++;
                    var isLastTry = tries > settings.Retries;

                    IDnsQueryResponse lastQueryResponse = null;

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug(
                            LogEventQuery,
                            "TryResolve {0} via {1} => {2} on {3}, try {4}/{5}.",
                            request.Header.Id,
                            handler.Type,
                            request.Question,
                            serverInfo,
                            tries,
                            settings.Retries + 1);
                    }

                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        audit?.StartTimer();

                        DnsResponseMessage response;

                        if (settings.Timeout != System.Threading.Timeout.InfiniteTimeSpan
                            || (cancellationToken != CancellationToken.None && cancellationToken.CanBeCanceled))
                        {
                            var cts = new CancellationTokenSource(settings.Timeout);
                            CancellationTokenSource linkedCts = null;
                            if (cancellationToken != CancellationToken.None)
                            {
                                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
                            }
                            using (cts)
                            using (linkedCts)
                            {
                                response = await handler.QueryAsync(
                                    serverInfo.IPEndPoint,
                                    request,
                                    (linkedCts ?? cts).Token)
                                .WithCancellation((linkedCts ?? cts).Token)
                                .ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            response = await handler.QueryAsync(
                                    serverInfo.IPEndPoint,
                                    request,
                                    cancellationToken)
                                .ConfigureAwait(false);
                        }

                        lastQueryResponse = ProcessResponseMessage(
                            audit,
                            request,
                            response,
                            settings,
                            serverInfo,
                            handler.Type,
                            servers.Count,
                            isLastServer,
                            out var retryQueryHint);

                        if (lastQueryResponse is TruncatedQueryResponse)
                        {
                            // Return right away and try to fallback to TCP.
                            return lastQueryResponse;
                        }

                        audit?.AuditEnd(lastQueryResponse, serverInfo);
                        audit?.Build(lastQueryResponse);

                        if (lastQueryResponse.HasError)
                        {
                            throw new DnsResponseException((DnsResponseCode)response.Header.ResponseCode)
                            {
                                AuditTrail = audit?.Build()
                            };
                        }

                        if (retryQueryHint == HandleError.RetryNextServer)
                        {
                            break;
                        }

                        if (settings.UseCache)
                        {
                            Cache.Add(cacheKey, lastQueryResponse);
                        }

                        return lastQueryResponse;
                    }
                    catch (DnsXidMismatchException ex)
                    {
                        var handle = HandleDnsXidMismatchException(ex, request, settings, handler.Type, isLastServer: isLastServer, isLastTry: isLastTry, currentTry: tries);

                        if (handle == HandleError.RetryCurrentServer)
                        {
                            continue;
                        }
                        else if (handle == HandleError.RetryNextServer)
                        {
                            break;
                        }

                        throw;
                    }
                    catch (DnsResponseParseException ex)
                    {
                        var handle = HandleDnsResponeParseException(ex, request, handler.Type, isLastServer: isLastServer);
                        if (handle == HandleError.RetryNextServer)
                        {
                            break;
                        }
                        else if (handle == HandleError.ReturnResponse)
                        {
                            return new TruncatedQueryResponse();
                        }

                        throw;
                    }
                    catch (DnsResponseException ex)
                    {
                        // Response error handling and logging
                        var handle = HandleDnsResponseException(ex, request, settings, serverInfo, handler.Type, isLastServer: isLastServer, isLastTry: isLastTry, tries);

                        if (handle == HandleError.Throw)
                        {
                            throw;
                        }
                        else if (handle == HandleError.RetryCurrentServer)
                        {
                            continue;
                        }
                        else if (handle == HandleError.RetryNextServer)
                        {
                            break;
                        }

                        // This should not happen, but might if something upstream throws this exception before
                        // we got to parse the response, which, again, should not happen.
                        if (lastQueryResponse == null)
                        {
                            throw;
                        }

                        // If its the last server, return.
                        if (settings.UseCache && settings.CacheFailedResults)
                        {
                            Cache.Add(cacheKey, lastQueryResponse, true);
                        }

                        return lastQueryResponse;
                    }
                    catch (Exception ex) when (
                        ex is TimeoutException timeoutEx
                        || DnsMessageHandler.IsTransientException(ex)
                        || ex is OperationCanceledException)
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            var handle = HandleTimeoutException(ex, request, settings, serverInfo, handler.Type, isLastServer: isLastServer, isLastTry: isLastTry, currentTry: tries);

                            if (handle == HandleError.RetryCurrentServer)
                            {
                                continue;
                            }
                            else if (handle == HandleError.RetryNextServer)
                            {
                                break;
                            }
                        }

                        throw new DnsResponseException(
                            DnsResponseCode.ConnectionTimeout,
                            $"Query {request.Header.Id} => {request.Question} on {serverInfo} timed out or is a transient error.",
                            ex)
                        {
                            AuditTrail = audit?.Build()
                        };
                    }
                    catch (ArgumentException)
                    {
                        // Don't retry argument exceptions. This should not happen anyways unless we messed up somewhere.
                        throw;
                    }
                    catch (InvalidOperationException)
                    {
                        // Don't retry invalid ops exceptions. This should not happen anyways unless we messed up somewhere.
                        throw;
                    }
                    // Any other exception will not be retried on the same server. e.g. Socket Exceptions
                    catch (Exception ex)
                    {
                        audit?.AuditException(ex);

                        var handle = HandleUnhandledException(ex, request, serverInfo, handler.Type, isLastServer);

                        if (handle == HandleError.RetryNextServer)
                        {
                            break;
                        }
                        if (handle == HandleError.RetryCurrentServer)
                        {
                            continue;
                        }

                        throw new DnsResponseException(
                            DnsResponseCode.Unassigned,
                            $"Query {request.Header.Id} => {request.Question} on {serverInfo} failed with an error.",
                            ex)
                        {
                            AuditTrail = audit?.Build()
                        };
                    }
                } while (tries <= settings.Retries);
            } // next server

            // 1.3.0: With the error handling, this should never be reached.
            throw new DnsResponseException(DnsResponseCode.ConnectionTimeout, $"No connection could be established to any of the following name servers: {string.Join(", ", servers)}.")
            {
                AuditTrail = audit?.Build()
            };
        }

        private IDnsQueryResponse QueryCache(
            DnsQuestion question,
            DnsQuerySettings settings)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            var head = new DnsRequestHeader(false, DnsOpCode.Query);
            var request = new DnsRequestMessage(head, question);

            var cacheKey = ResponseCache.GetCacheKey(request.Question);

            if (TryGetCachedResult(cacheKey, request, settings, out var cachedResponse))
            {
                return cachedResponse;
            }
            else
            {
                return null;
            }
        }

        private enum HandleError
        {
            None,
            Throw,
            RetryCurrentServer,
            RetryNextServer,
            ReturnResponse
        }

        private HandleError HandleDnsResponseException(DnsResponseException ex, DnsRequestMessage request, DnsQuerySettings settings, NameServer nameServer, DnsMessageHandleType handleType, bool isLastServer, bool isLastTry, int currentTry)
        {
            var handle = isLastServer ? settings.ThrowDnsErrors ? HandleError.Throw : HandleError.ReturnResponse : HandleError.RetryNextServer;

            if (!settings.ContinueOnDnsError)
            {
                handle = settings.ThrowDnsErrors ? HandleError.Throw : HandleError.ReturnResponse;
            }
            else if (!isLastTry &&
                (ex.Code == DnsResponseCode.ServerFailure || ex.Code == DnsResponseCode.FormatError))
            {
                handle = HandleError.RetryCurrentServer;
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                var eventId = 0;
                var message = "Query {0} via {1} => {2} on {3} returned a response error '{4}'.";

                switch (handle)
                {
                    case HandleError.Throw:
                        eventId = LogEventQueryFail;
                        message += " Throwing the error.";
                        break;

                    case HandleError.ReturnResponse:
                        eventId = LogEventQueryReturnResponseError;
                        message += " Returning response.";
                        break;

                    case HandleError.RetryCurrentServer:
                        eventId = LogEventQueryRetryErrorSameServer;
                        message += " Re-trying {5}/{6}....";
                        break;

                    case HandleError.RetryNextServer:
                        eventId = LogEventQueryRetryErrorNextServer;
                        message += " Trying next server.";
                        break;
                }

                if (handle == HandleError.RetryCurrentServer)
                {
                    _logger.LogInformation(eventId, message, request.Header.Id, handleType, request.Question, nameServer, ex.DnsError, currentTry + 1, settings.Retries + 1);
                }
                else
                {
                    _logger.LogInformation(eventId, message, request.Header.Id, handleType, request.Question, nameServer, ex.DnsError);
                }
            }

            return handle;
        }

        private HandleError HandleDnsXidMismatchException(DnsXidMismatchException ex, DnsRequestMessage request, DnsQuerySettings settings, DnsMessageHandleType handleType, bool isLastServer, bool isLastTry, int currentTry)
        {
            // No more retries
            if (isLastServer && isLastTry)
            {
                _logger.LogError(
                    LogEventQueryFail,
                    ex,
                    "Query {0} via {1} => {2} xid mismatch {3}. Throwing the error.",
                    ex.RequestXid,
                    handleType,
                    request.Question,
                    ex.ResponseXid);

                return HandleError.Throw;
            }

            // Last try on the current server, try the nextServer
            if (isLastTry)
            {
                _logger.LogError(
                    LogEventQueryRetryErrorNextServer,
                    ex,
                    "Query {0} via {1} => {2} xid mismatch {3}. Trying next server.",
                    ex.RequestXid,
                    handleType,
                    request.Question,
                    ex.ResponseXid);

                return HandleError.RetryNextServer;
            }

            // Next try
            _logger.LogWarning(
                LogEventQueryRetryErrorNextServer,
                ex,
                "Query {0} via {1} => {2} xid mismatch {3}. Re-trying {4}/{5}...",
                    ex.RequestXid,
                    handleType,
                    request.Question,
                    ex.ResponseXid,
                    currentTry,
                    settings.Retries + 1);

            return HandleError.RetryCurrentServer;
        }

        private HandleError HandleDnsResponeParseException(DnsResponseParseException ex, DnsRequestMessage request, DnsMessageHandleType handleType, bool isLastServer)
        {
            // Don't try to fallback to TCP if we already are on TCP
            // Assuming that if we only got 512 or less bytes, its probably some network issue.
            // Second assumption: If the parser tried to read outside the provided data, this might also be a network issue.
            if (handleType == DnsMessageHandleType.UDP
                && (ex.ResponseData.Length <= DnsQueryOptions.MinimumBufferSize || ex.ReadLength + ex.Index > ex.ResponseData.Length))
            {
                // lets assume the response was truncated and retry with TCP.
                // (Not retrying other servers as it is very unlikely they would provide better results on this network)
                _logger.LogError(
                    LogEventQueryBadTruncation,
                    ex,
                    "Query {0} via {1} => {2} error parsing the response. The response seems to be truncated without TC flag set! Re-trying via TCP anyways.",
                    request.Header.Id,
                    handleType,
                    request.Question);

                // In this case the caller should return TruncatedResponseMessage
                return HandleError.ReturnResponse;
            }

            if (isLastServer)
            {
                _logger.LogError(
                    LogEventQueryFail,
                    ex,
                    "Query {0} via {1} => {2} error parsing the response. Throwing the error.",
                    request.Header.Id,
                    handleType,
                    request.Question);

                return HandleError.Throw;
            }

            // Otherwise, lets continue at least with the next server
            _logger.LogWarning(
                LogEventQueryRetryErrorNextServer,
                ex,
                "Query {0} via {1} => {2} error parsing the response. Trying next server.",
                request.Header.Id,
                handleType,
                request.Question);

            return HandleError.RetryNextServer;
        }

        private HandleError HandleTimeoutException(Exception ex, DnsRequestMessage request, DnsQuerySettings settings, NameServer nameServer, DnsMessageHandleType handleType, bool isLastServer, bool isLastTry, int currentTry)
        {
            if (isLastTry && isLastServer)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        LogEventQueryFail,
                        ex,
                        "Query {0} via {1} => {2} on {3} timed out or is a transient error. Throwing the error.",
                        request.Header.Id,
                        handleType,
                        request.Question,
                        nameServer);
                }

                return HandleError.Throw;
            }
            else if (isLastTry)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        LogEventQueryRetryErrorNextServer,
                        ex,
                        "Query {0} via {1} => {2} on {3} timed out or is a transient error. Trying next server",
                        request.Header.Id,
                        handleType,
                        request.Question,
                        nameServer);
                }

                return HandleError.RetryNextServer;
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    LogEventQueryRetryErrorSameServer,
                    ex,
                    "Query {0} via {1} => {2} on {3} timed out or is a transient error. Re-trying {4}/{5}...",
                    request.Header.Id,
                    handleType,
                    request.Question,
                    nameServer,
                    currentTry,
                    settings.Retries + 1);
            }

            return HandleError.RetryCurrentServer;
        }

        private HandleError HandleUnhandledException(Exception ex, DnsRequestMessage request, NameServer nameServer, DnsMessageHandleType handleType, bool isLastServer)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    isLastServer ? LogEventQueryFail : LogEventQueryRetryErrorNextServer,
                    ex,
                    "Query {0} via {1} => {2} on {3} failed with an error."
                        + (isLastServer ? " Throwing the error." : " Trying next server."),
                    handleType,
                    request.Header.Id,
                    request.Question,
                    nameServer);
            }

            if (isLastServer)
            {
                return HandleError.Throw;
            }

            return HandleError.RetryNextServer;
        }

        private IDnsQueryResponse ProcessResponseMessage(
            LookupClientAudit audit,
            DnsRequestMessage request,
            DnsResponseMessage response,
            DnsQuerySettings settings,
            NameServer nameServer,
            DnsMessageHandleType handleType,
            int serverCount,
            bool isLastServer,
            out HandleError handleError)
        {
            handleError = HandleError.None;

            if (response.Header.ResultTruncated)
            {
                audit?.AuditTruncatedRetryTcp();

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(LogEventQueryTruncated, "Query {0} via {1} => {2} was truncated, re-trying with TCP.", request.Header.Id, handleType, request.Question);
                }

                return new TruncatedQueryResponse();
            }

            if (request.Header.Id != response.Header.Id)
            {
                _logger.LogWarning(
                    "Request header id {0} does not match response header {1}. This might be due to some non-standard configuration in your network.",
                    request.Header.Id,
                    response.Header.Id);
            }

            audit?.AuditResolveServers(serverCount);
            audit?.AuditResponseHeader(response.Header);

            if (response.Header.ResponseCode != DnsHeaderResponseCode.NoError)
            {
                audit?.AuditResponseError(response.Header.ResponseCode);
            }
            else
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(
                        LogEventQuerySuccess,
                        "Got {0} answers for query {1} via {2} => {3} from {4}.",
                        response.Answers.Count,
                        request.Header.Id,
                        handleType,
                        request.Question,
                        nameServer);
                }
            }

            HandleOptRecords(settings, audit, nameServer, response);

            var result = response.AsQueryResponse(nameServer, settings);

            // Set retry next server hint in case the question hasn't been answered.
            // Only if there are more servers and the response doesn't have an error as that gets retried already per configuration.
            // Also, only do this if the setting is enabled.
            if (!result.HasError && !isLastServer && settings.ContinueOnEmptyResponse)
            {
                // Try next server, if the success result has zero answers.
                if (result.Answers.Count == 0)
                {
                    handleError = HandleError.RetryNextServer;
                }

                // Try next server if the question isn't answered (ignoring ANY and AXFR queries)
                else if (request.Question.QuestionType != QueryType.ANY
                   && request.Question.QuestionType != QueryType.AXFR
                   && !((request.Question.QuestionType == QueryType.A || request.Question.QuestionType == QueryType.AAAA)
                        && result.Answers.OfRecordType(ResourceRecordType.CNAME).Any())
                   && !(request.Question.QuestionType == QueryType.NS && result.Authorities.Any())
                   && !result.Answers.OfRecordType((ResourceRecordType)request.Question.QuestionType).Any())
                {
                    handleError = HandleError.RetryNextServer;
                }

                if (handleError == HandleError.RetryNextServer
                    && _logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        LogEventQuerySuccessEmpty,
                        "Got no answer for query {0} via {1} => {2} from {3}. Trying next server.",
                        request.Header.Id,
                        handleType,
                        request.Question,
                        nameServer);
                }
            }

            return result;
        }

        private bool TryGetCachedResult(string cacheKey, DnsRequestMessage request, DnsQuerySettings settings, out IDnsQueryResponse response)
        {
            response = null;
            if (settings.UseCache)
            {
                response = Cache.Get(cacheKey);
                if (response != null)
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug(LogEventQueryCachedResult, "Got cached result for query {0} => {1}.", request.Header.Id, request.Question);
                    }

                    if (settings.EnableAuditTrail)
                    {
                        var cacheAudit = new LookupClientAudit(settings);
                        cacheAudit.AuditCachedItem(response);
                        cacheAudit.Build(response);
                    }

                    return true;
                }
            }

            return false;
        }

        private void HandleOptRecords(DnsQuerySettings settings, LookupClientAudit audit, NameServer serverInfo, DnsResponseMessage response)
        {
            if (settings.UseExtendedDns)
            {
                var record = response.Additionals.OfRecordType(Protocol.ResourceRecordType.OPT).FirstOrDefault();

                if (record == null)
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation(LogEventResponseMissingOpt, "Response {0} => {1} is missing the requested OPT record.", response.Header.Id, response.Questions.FirstOrDefault());
                    }
                }
                else if (record is OptRecord optRecord)
                {
                    audit?.AuditOptPseudo();

                    serverInfo.SupportedUdpPayloadSize = optRecord.UdpSize;

                    audit?.AuditEdnsOpt(optRecord.UdpSize, optRecord.Version, optRecord.IsDnsSecOk, optRecord.ResponseCodeEx);

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug(
                            LogEventResponseOpt,
                            "Response {0} => {1} opt record sets buffer of {2} to {3}.",
                            response.Header.Id,
                            response.Questions.FirstOrDefault(),
                            serverInfo,
                            optRecord.UdpSize);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a reverse lookup question for an <see cref="IPAddress"/>.
        /// </summary>
        /// <param name="ipAddress">The address.</param>
        /// <returns>A <see cref="DnsQuestion"/> with the proper arpa domain query for the given address.</returns>
        public static DnsQuestion GetReverseQuestion(IPAddress ipAddress)
        {
            if (ipAddress == null)
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            var arpa = ipAddress.GetArpaName();
            return new DnsQuestion(arpa, QueryType.PTR, QueryClass.IN);
        }

        private class SkipWorker
        {
            private readonly Action _worker;
            private readonly int _skipFor = 5000;
            private int _lastRun = 0;

            public SkipWorker(Action worker, int skip = 5000)
            {
                _worker = worker ?? throw new ArgumentNullException(nameof(worker));

                if (skip <= 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(skip));
                }

                _skipFor = skip;

                // Skip first run
                _lastRun = (Environment.TickCount & int.MaxValue);
            }

            public void MaybeDoWork()
            {
                if (_lastRun + _skipFor >= (Environment.TickCount & int.MaxValue))
                {
                    return;
                }

                var oldValue = _lastRun;
                if (Interlocked.CompareExchange(ref _lastRun, (Environment.TickCount & int.MaxValue), oldValue) == oldValue)
                {
                    _worker();
                }
            }
        }
    }

    internal class LookupClientAudit
    {
        private static readonly int s_printOffset = -32;
        private readonly StringBuilder _auditWriter = new StringBuilder();
        private Stopwatch _swatch;

        public DnsQuerySettings Settings { get; }

        public LookupClientAudit(DnsQuerySettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void AuditCachedItem(IDnsQueryResponse response)
        {
            if (!Settings.EnableAuditTrail)
            {
                return;
            }

            StartTimer();
            _auditWriter.AppendLine($"; (cached result)");
            AuditResponseHeader(response.Header);

            AuditOptPseudo();

            var record = response.Additionals.OfRecordType(Protocol.ResourceRecordType.OPT).FirstOrDefault();
            if (record != null && record is OptRecord optRecord)
            {
                AuditEdnsOpt(optRecord.UdpSize, optRecord.Version, optRecord.IsDnsSecOk, optRecord.ResponseCodeEx);
            }

            AuditEnd(response, response.NameServer);
        }

        public void StartTimer()
        {
            if (!Settings.EnableAuditTrail)
            {
                return;
            }

            _swatch = Stopwatch.StartNew();
            _swatch.Restart();
        }

        public void AuditResolveServers(int count)
        {
            if (!Settings.EnableAuditTrail)
            {
                return;
            }

            _auditWriter.AppendLine($"; ({count} server found)");
        }

        public string Build(IDnsQueryResponse response = null)
        {
            if (!Settings.EnableAuditTrail)
            {
                return string.Empty;
            }

            var audit = _auditWriter.ToString();
            if (response != null)
            {
                DnsQueryResponse.SetAuditTrail(response, audit);
            }

            return audit;
        }

        public void AuditTruncatedRetryTcp()
        {
            if (!Settings.EnableAuditTrail)
            {
                return;
            }

            _auditWriter.AppendLine(";; Truncated, retrying in TCP mode.");
            _auditWriter.AppendLine();
        }

        public void AuditResponseError(DnsHeaderResponseCode responseCode)
        {
            if (!Settings.EnableAuditTrail)
            {
                return;
            }

            _auditWriter.AppendLine($";; ERROR: {DnsResponseCodeText.GetErrorText((DnsResponseCode)responseCode)}");
        }

        public void AuditOptPseudo()
        {
            if (!Settings.EnableAuditTrail)
            {
                return;
            }

            _auditWriter.AppendLine(";; OPT PSEUDOSECTION:");
        }

        public void AuditResponseHeader(DnsResponseHeader header)
        {
            if (!Settings.EnableAuditTrail)
            {
                return;
            }

            _auditWriter.AppendLine(";; Got answer:");
            _auditWriter.AppendLine(header.ToString());
            if (header.RecursionDesired && !header.RecursionAvailable)
            {
                _auditWriter.AppendLine(";; WARNING: recursion requested but not available");
            }
            _auditWriter.AppendLine();
        }

        public void AuditEdnsOpt(short udpSize, byte version, bool doFlag, DnsResponseCode responseCode)
        {
            if (!Settings.EnableAuditTrail)
            {
                return;
            }

            _auditWriter.AppendLine($"; EDNS: version: {version}, flags:{(doFlag ? " do" : string.Empty)}; UDP: {udpSize}; code: {responseCode}");
        }

        public void AuditEnd(IDnsQueryResponse queryResponse, NameServer nameServer)
        {
            if (queryResponse is null)
            {
                throw new ArgumentNullException(nameof(queryResponse));
            }

            if (nameServer is null)
            {
                throw new ArgumentNullException(nameof(nameServer));
            }

            if (!Settings.EnableAuditTrail)
            {
                return;
            }

            var elapsed = _swatch.ElapsedMilliseconds;

            // TODO: find better way to print the actual TTL of cached values
            if (queryResponse != null)
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

                var additionals = queryResponse.Additionals.Where(p => !(p is OptRecord)).ToArray();
                if (additionals.Length > 0)
                {
                    _auditWriter.AppendLine(";; ADDITIONALS SECTION:");
                    foreach (var additional in additionals)
                    {
                        _auditWriter.AppendLine(additional.ToString(s_printOffset));
                    }
                    _auditWriter.AppendLine();
                }
            }

            _auditWriter.AppendLine($";; Query time: {elapsed} msec");
            _auditWriter.AppendLine($";; SERVER: {nameServer.Address}#{nameServer.Port}");
            _auditWriter.AppendLine($";; WHEN: {DateTime.UtcNow.ToString("ddd MMM dd HH:mm:ss K yyyy", CultureInfo.InvariantCulture)}");
            _auditWriter.AppendLine($";; MSG SIZE  rcvd: {queryResponse.MessageSize}");
        }

        public void AuditException(Exception ex)
        {
            if (!Settings.EnableAuditTrail)
            {
                return;
            }

            if (ex is DnsResponseException dnsEx)
            {
                _auditWriter.AppendLine($";; Error: {DnsResponseCodeText.GetErrorText(dnsEx.Code)} {dnsEx.InnerException?.Message ?? dnsEx.Message}");
            }
            else if (ex is AggregateException aggEx)
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

        public void AuditRetryNextServer()
        {
            if (!Settings.EnableAuditTrail)
            {
                return;
            }

            _auditWriter.AppendLine();
            _auditWriter.AppendLine($"; Trying next server.");
        }
    }
}
