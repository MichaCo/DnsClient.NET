using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DnsClient.Internal;
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
        private const int c_eventStartQuery = 1;
        private const int c_eventQuery = 2;
        private const int c_eventQueryCachedResult = 3;
        private const int c_eventQueryTruncated = 5;
        private const int c_eventQuerySuccess = 10;
        private const int c_eventQueryReturnResponseError = 11;

        private const int c_eventQueryRetryErrorNextServer = 20;
        private const int c_eventQueryRetryErrorSameServer = 21;

        private const int c_eventQueryFail = 90;
        private const int c_eventQueryBadTruncation = 91;

        private const int c_eventResponseOpt = 31;
        private const int c_eventResponseMissingOpt = 80;

        private readonly DnsMessageHandler _messageHandler;
        private readonly DnsMessageHandler _tcpFallbackHandler;
        private readonly ILogger _logger;

        // for backward compat
        /// <summary>
        /// Gets the list of configured name servers.
        /// </summary>
        public IReadOnlyCollection<NameServer> NameServers => Settings.NameServers;

        // TODO: make readonly when obsolete stuff is removed
        /// <summary>
        /// Gets the settings.
        /// </summary>
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
                    Settings = Settings.WithMinimumCacheTimeout(value);
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
                    Settings = Settings.WithUseTcpFallback(value);
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
                    Settings = Settings.WithUseTcpOnly(value);
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
                    Settings = Settings.WithEnableAuditTrail(value);
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
                    Settings = Settings.WithRecursion(value);
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
                    Settings = Settings.WithRetries(value);
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
                    Settings = Settings.WithThrowDnsErrors(value);
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
                    Settings = Settings.WithTimeout(value);
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
                    Settings = Settings.WithUseCache(value);
                }
            }
        }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        public bool UseRandomNameServer { get; set; } = true;

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        public bool ContinueOnDnsError
        {
            get => Settings.ContinueOnDnsError;
            set
            {
                if (Settings.ContinueOnDnsError != value)
                {
                    Settings = Settings.WithContinueOnDnsError(value);
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
            : this(new LookupClientOptions(resolveNameServers: true))
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
        /// The <see cref="NameServer"/> class also contains pre defined <see cref="IPEndPoint"/>s for the public google DNS servers, which can be used as follows:
        /// <code>
        /// <![CDATA[
        /// var client = new LookupClient(NameServer.GooglePublicDns, NameServer.GooglePublicDnsIPv6);
        /// ]]>
        /// </code>
        /// </para>
        /// </example>
        public LookupClient(params IPEndPoint[] nameServers)
            : this(new LookupClientOptions(nameServers))
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="LookupClient"/> with default settings and the given name servers.
        /// </summary>
        /// <param name="nameServers">The <see cref="NameServer"/>(s) to be used by this <see cref="LookupClient"/> instance.</param>
        public LookupClient(params NameServer[] nameServers)
            : this(new LookupClientOptions(nameServers))
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="LookupClient"/> with custom settings.
        /// </summary>
        /// <param name="options">The options to use with this <see cref="LookupClient"/> instance.</param>
        public LookupClient(LookupClientOptions options)
            : this(options, null, null)
        {
        }

        internal LookupClient(LookupClientOptions options, DnsMessageHandler udpHandler = null, DnsMessageHandler tcpHandler = null)
        {
            Settings = options ?? throw new ArgumentNullException(nameof(options));
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

            _logger = Logging.LoggerFactory.CreateLogger(nameof(LookupClient));
            Cache = new ResponseCache(true, Settings.MinimumCacheTimeout, Settings.MaximumCacheTimeout);
        }

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress"/>.
        /// </summary>
        /// <param name="ipAddress">The <see cref="IPAddress"/>.</param>
        ///
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="ipAddress"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        public IDnsQueryResponse QueryReverse(IPAddress ipAddress)
            => Query(GetReverseQuestion(ipAddress));

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress"/>.
        /// </summary>
        /// <param name="ipAddress">The <see cref="IPAddress"/>.</param>
        /// <param name="queryOptions">Query options to be used instead of <see cref="LookupClient"/>'s settings.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="ipAddress"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        public IDnsQueryResponse QueryReverse(IPAddress ipAddress, DnsQueryAndServerOptions queryOptions)
        {
            if (queryOptions is null)
            {
                throw new ArgumentNullException(nameof(queryOptions));
            }

            return Query(GetReverseQuestion(ipAddress), queryOptions);
        }

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress" />.
        /// </summary>
        /// <param name="ipAddress">The <see cref="IPAddress" />.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="ipAddress"/> is null.</exception>
        /// <exception cref="OperationCanceledException">If cancellation has been requested for the passed in <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        public Task<IDnsQueryResponse> QueryReverseAsync(IPAddress ipAddress, CancellationToken cancellationToken = default)
            => QueryAsync(GetReverseQuestion(ipAddress), cancellationToken: cancellationToken);

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress" />.
        /// </summary>
        /// <param name="ipAddress">The <see cref="IPAddress" />.</param>
        /// <param name="queryOptions">Query options to be used instead of <see cref="LookupClient"/>'s settings.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="ipAddress"/> is null.</exception>
        /// <exception cref="OperationCanceledException">If cancellation has been requested for the passed in <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        public Task<IDnsQueryResponse> QueryReverseAsync(IPAddress ipAddress, DnsQueryAndServerOptions queryOptions, CancellationToken cancellationToken = default)
        {
            if (queryOptions is null)
            {
                throw new ArgumentNullException(nameof(queryOptions));
            }

            return QueryAsync(GetReverseQuestion(ipAddress), queryOptions, cancellationToken);
        }

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="query" />, <paramref name="queryType" /> and <paramref name="queryClass" />.
        /// </summary>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <param name="queryClass">The <see cref="QueryClass"/>.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        /// <remarks>
        /// The behavior of the query can be controlled by default settings of this <see cref="LookupClient"/> instance.
        /// <see cref="Recursion"/> for example can be disabled and would instruct the DNS server to return no additional records.
        /// </remarks>
        public IDnsQueryResponse Query(string query, QueryType queryType, QueryClass queryClass = QueryClass.IN)
            => Query(new DnsQuestion(query, queryType, queryClass), queryOptions: null);

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="query" />, <paramref name="queryType" /> and <paramref name="queryClass" />.
        /// </summary>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <param name="queryClass">The <see cref="QueryClass"/>.</param>
        /// <param name="queryOptions">Query options to be used instead of <see cref="LookupClient"/>'s settings.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        /// <remarks>
        /// The behavior of the query can be controlled by default settings of this <see cref="LookupClient"/> instance or via <paramref name="queryOptions"/>.
        /// <see cref="Recursion"/> for example can be disabled and would instruct the DNS server to return no additional records.
        /// </remarks>
        public IDnsQueryResponse Query(string query, QueryType queryType, DnsQueryAndServerOptions queryOptions, QueryClass queryClass = QueryClass.IN)
        {
            if (queryOptions is null)
            {
                throw new ArgumentNullException(nameof(queryOptions));
            }

            return Query(new DnsQuestion(query, queryType, queryClass), queryOptions);
        }

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="question" />.
        /// </summary>
        /// <param name="question">The domain name query.</param>
        /// <param name="queryOptions">Query options to be used instead of <see cref="LookupClient"/>'s settings.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="question"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        public IDnsQueryResponse Query(DnsQuestion question, DnsQueryAndServerOptions queryOptions = null)
        {
            var settings = GetSettings(queryOptions);
            return QueryInternal(question, settings, settings.ShuffleNameServers());
        }

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="query" />, <paramref name="queryType" /> and <paramref name="queryClass" />.
        /// </summary>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <param name="queryClass">The <see cref="QueryClass" />.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="OperationCanceledException">If cancellation has been requested for the passed in <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        /// <remarks>
        /// The behavior of the query can be controlled by default settings of this <see cref="LookupClient"/>.
        /// <see cref="Recursion"/> for example can be disabled and would instruct the DNS server to return no additional records.
        /// </remarks>
        public Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default)
            => QueryAsync(new DnsQuestion(query, queryType, queryClass), queryOptions: null, cancellationToken: cancellationToken);

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="query" />, <paramref name="queryType" /> and <paramref name="queryClass" />.
        /// </summary>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <param name="queryClass">The <see cref="QueryClass" />.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="queryOptions">Query options to be used instead of <see cref="LookupClient"/>'s settings.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="OperationCanceledException">If cancellation has been requested for the passed in <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        /// <remarks>
        /// The behavior of the query can be controlled by default settings of this <see cref="LookupClient"/> instance or via <paramref name="queryOptions"/>.
        /// <see cref="Recursion"/> for example can be disabled and would instruct the DNS server to return no additional records.
        /// </remarks>
        public Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType, DnsQueryAndServerOptions queryOptions, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default)
        {
            if (queryOptions is null)
            {
                throw new ArgumentNullException(nameof(queryOptions));
            }

            return QueryAsync(new DnsQuestion(query, queryType, queryClass), queryOptions, cancellationToken);
        }

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="question" />.
        /// </summary>
        /// <param name="question">The domain name query.</param>
        /// <param name="queryOptions">Query options to be used instead of <see cref="LookupClient"/>'s settings.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="question"/> or <paramref name="queryOptions"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        public Task<IDnsQueryResponse> QueryAsync(DnsQuestion question, DnsQueryAndServerOptions queryOptions = null, CancellationToken cancellationToken = default)
        {
            var settings = GetSettings(queryOptions);
            return QueryInternalAsync(question, settings, settings.ShuffleNameServers(), cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="query" />, <paramref name="queryType" /> and <paramref name="queryClass" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <remarks>
        /// To query specific servers can be useful in cases where you have to use a different DNS server than initially configured
        /// (without creating a new instance of <see cref="ILookupClient"/> for example).
        /// </remarks>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <param name="queryClass">The <see cref="QueryClass" />.</param>
        /// <param name="queryOptions">Query options to be used instead of <see cref="LookupClient"/>'s settings.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        public IDnsQueryResponse QueryServer(IReadOnlyCollection<NameServer> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, DnsQueryOptions queryOptions = null)
            => QueryInternal(new DnsQuestion(query, queryType, queryClass), GetSettings(queryOptions), servers);

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="query" />, <paramref name="queryType" /> and <paramref name="queryClass" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <remarks>
        /// To query specific servers can be useful in cases where you have to use a different DNS server than initially configured
        /// (without creating a new instance of <see cref="ILookupClient"/> for example).
        /// </remarks>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <param name="queryClass">The <see cref="QueryClass" />.</param>
        /// <param name="queryOptions">Query options to be used instead of <see cref="LookupClient"/>'s settings.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="OperationCanceledException">If cancellation has been requested for the passed in <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        public Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<NameServer> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, DnsQueryOptions queryOptions = null, CancellationToken cancellationToken = default)
            => QueryInternalAsync(new DnsQuestion(query, queryType, queryClass), GetSettings(queryOptions), servers, cancellationToken);

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="ipAddress">The <see cref="IPAddress" />.</param>
        /// <param name="queryOptions">Query options to be used instead of <see cref="LookupClient"/>'s settings.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="ipAddress"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        public IDnsQueryResponse QueryServerReverse(IReadOnlyCollection<NameServer> servers, IPAddress ipAddress, DnsQueryOptions queryOptions = null)
            => QueryInternal(GetReverseQuestion(ipAddress), GetSettings(queryOptions), servers);

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="ipAddress">The <see cref="IPAddress" />.</param>
        /// <param name="queryOptions">Query options to be used instead of <see cref="LookupClient"/>'s settings.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="ipAddress"/> is null.</exception>
        /// <exception cref="OperationCanceledException">If cancellation has been requested for the passed in <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        public Task<IDnsQueryResponse> QueryServerReverseAsync(IReadOnlyCollection<NameServer> servers, IPAddress ipAddress, DnsQueryOptions queryOptions = null, CancellationToken cancellationToken = default)
            => QueryInternalAsync(GetReverseQuestion(ipAddress), GetSettings(queryOptions), servers, cancellationToken);

        #region obsolete overloads

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public IDnsQueryResponse QueryServer(IReadOnlyCollection<IPAddress> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN)
            => QueryServer(NameServer.Convert(servers), query, queryType, queryClass, queryOptions: null);

        public IDnsQueryResponse QueryServer(IReadOnlyCollection<IPEndPoint> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN)
            => QueryServer(NameServer.Convert(servers), query, queryType, queryClass, queryOptions: null);

        public Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<IPAddress> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default)
            => QueryServerAsync(NameServer.Convert(servers), query, queryType, queryClass, queryOptions: null, cancellationToken: cancellationToken);

        public Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<IPEndPoint> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default)
            => QueryServerAsync(NameServer.Convert(servers), query, queryType, queryClass, queryOptions: null, cancellationToken: cancellationToken);

        public IDnsQueryResponse QueryServerReverse(IReadOnlyCollection<IPAddress> servers, IPAddress ipAddress)
            => QueryServerReverse(NameServer.Convert(servers), ipAddress, queryOptions: null);

        public IDnsQueryResponse QueryServerReverse(IReadOnlyCollection<IPEndPoint> servers, IPAddress ipAddress)
            => QueryServerReverse(NameServer.Convert(servers), ipAddress, queryOptions: null);

        public Task<IDnsQueryResponse> QueryServerReverseAsync(IReadOnlyCollection<IPAddress> servers, IPAddress ipAddress, CancellationToken cancellationToken = default)
            => QueryServerReverseAsync(NameServer.Convert(servers), ipAddress, queryOptions: null, cancellationToken: cancellationToken);

        public Task<IDnsQueryResponse> QueryServerReverseAsync(IReadOnlyCollection<IPEndPoint> servers, IPAddress ipAddress, CancellationToken cancellationToken = default)
            => QueryServerReverseAsync(NameServer.Convert(servers), ipAddress, queryOptions: null, cancellationToken: cancellationToken);

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        #endregion

        // For unit tests.
        internal DnsQueryAndServerSettings GetSettings(DnsQueryAndServerOptions queryOptions = null)
        {
            if (queryOptions == null)
            {
                return Settings;
            }

            if ((queryOptions.NameServers == null || queryOptions.NameServers.Count == 0)
                && queryOptions.AutoResolvedNameServers == false)
            {
                // fallback to already configured nameservers in case none are specified.
                return new DnsQueryAndServerSettings(queryOptions, Settings.NameServers);
            }

            return queryOptions;
        }

        private DnsQuerySettings GetSettings(DnsQueryOptions queryOptions = null)
            => queryOptions == null ? Settings : (DnsQuerySettings)queryOptions;

        private IDnsQueryResponse QueryInternal(DnsQuestion question, DnsQuerySettings settings, IReadOnlyCollection<NameServer> servers)
        {
            if (servers == null)
            {
                throw new ArgumentNullException(nameof(servers));
            }
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (servers.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(servers), "List of configured name servers must not be empty.");
            }

            var head = new DnsRequestHeader(settings.Recursion, DnsOpCode.Query);
            var request = new DnsRequestMessage(head, question, settings);
            var handler = settings.UseTcpOnly ? _tcpFallbackHandler : _messageHandler;
            var audit = settings.EnableAuditTrail ? new LookupClientAudit(settings) : null;

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(c_eventStartQuery, "Begin query {0} => {1} on [{2}]", head, question, string.Join(", ", servers));
            }

            var result = ResolveQuery(servers.ToList(), settings, handler, request, audit);
            if (!(result is TruncatedQueryResponse))
            {
                return result;
            }

            if (!settings.UseTcpFallback)
            {
                throw new DnsResponseException(DnsResponseCode.Unassigned, "Response was truncated and UseTcpFallback is disabled, unable to resolve the question.")
                {
                    AuditTrail = audit?.Build(result)
                };
            }

            request.Header.RefreshId();
            var tcpResult = ResolveQuery(servers.ToList(), settings, _tcpFallbackHandler, request, audit);
            if (tcpResult is TruncatedQueryResponse)
            {
                throw new DnsResponseException("Unexpected truncated result from TCP response.")
                {
                    AuditTrail = audit?.Build(tcpResult)
                };
            }

            return tcpResult;
        }

        private async Task<IDnsQueryResponse> QueryInternalAsync(DnsQuestion question, DnsQuerySettings settings, IReadOnlyCollection<NameServer> servers, CancellationToken cancellationToken = default)
        {
            if (servers == null)
            {
                throw new ArgumentNullException(nameof(servers));
            }
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (servers.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(servers), "List of configured name servers must not be empty.");
            }

            var head = new DnsRequestHeader(settings.Recursion, DnsOpCode.Query);
            var request = new DnsRequestMessage(head, question, settings);
            var handler = settings.UseTcpOnly ? _tcpFallbackHandler : _messageHandler;
            var audit = settings.EnableAuditTrail ? new LookupClientAudit(settings) : null;

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(c_eventStartQuery, "Begin query {0} => {1} on [{2}].", head.Id, question, string.Join(", ", servers));
            }

            var result = await ResolveQueryAsync(servers.ToList(), settings, handler, request, audit, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!(result is TruncatedQueryResponse))
            {
                return result;
            }

            if (!settings.UseTcpFallback)
            {
                throw new DnsResponseException(DnsResponseCode.Unassigned, "Response was truncated and UseTcpFallback is disabled, unable to resolve the question.")
                {
                    AuditTrail = audit?.Build(result)
                };
            }

            request.Header.RefreshId();
            var tcpResult = await ResolveQueryAsync(servers.ToList(), settings, _tcpFallbackHandler, request, audit, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (tcpResult is TruncatedQueryResponse)
            {
                throw new DnsResponseException("Unexpected truncated result from TCP response.")
                {
                    AuditTrail = audit?.Build(tcpResult)
                };
            }

            return tcpResult;
        }

        // making it internal for unit testing
        internal IDnsQueryResponse ResolveQuery(
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
                    audit?.AuditRetryNextServer(serverInfo);
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
                            c_eventQuery,
                            "TryResolve {0} => {1} on {2}, try {3}/{4}.",
                            request.Header.Id,
                            request.Question,
                            serverInfo,
                            tries,
                            settings.Retries + 1);
                    }

                    try
                    {
                        audit?.StartTimer();

                        DnsResponseMessage response = handler.Query(serverInfo.IPEndPoint, request, settings.Timeout);

                        lastQueryResponse = ProcessResponseMessage(audit, request, response, settings, serverInfo, servers.Count);

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

                        if (settings.UseCache)
                        {
                            Cache.Add(cacheKey, lastQueryResponse);
                        }

                        return lastQueryResponse;
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
                        var handle = HandleDnsResponseException(ex, request, settings, serverInfo, isLastServer: isLastServer, isLastTry: isLastTry, tries);

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
                        return lastQueryResponse;
                    }
                    catch (Exception ex) when (
                        ex is TimeoutException timeoutEx
                        || handler.IsTransientException(ex)
                        || ex is OperationCanceledException)
                    {
                        var handle = HandleTimeoutException(ex, request, settings, serverInfo, isLastServer: isLastServer, isLastTry: isLastTry, currentTry: tries);

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

                        var handle = HandleUnhandledException(ex, request, serverInfo, isLastServer);

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

                if (settings.EnableAuditTrail && !isLastServer)
                {
                    audit?.AuditRetryNextServer(serverInfo);
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
                            c_eventQuery,
                            "TryResolve {0} => {1} on {2}, try {3}/{4}.",
                            request.Header.Id,
                            request.Question,
                            serverInfo,
                            tries,
                            settings.Retries + 1);
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        audit?.StartTimer();

                        DnsResponseMessage response;
                        Action onCancel = () => { };
                        Task<DnsResponseMessage> resultTask = handler.QueryAsync(serverInfo.IPEndPoint, request, cancellationToken, (cancel) =>
                        {
                            onCancel = cancel;
                        });

                        if (settings.Timeout != System.Threading.Timeout.InfiniteTimeSpan || (cancellationToken != CancellationToken.None && cancellationToken.CanBeCanceled))
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
                                response = await resultTask.WithCancellation((linkedCts ?? cts).Token, onCancel).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            response = await resultTask.ConfigureAwait(false);
                        }

                        lastQueryResponse = ProcessResponseMessage(audit, request, response, settings, serverInfo, servers.Count);

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

                        if (settings.UseCache)
                        {
                            Cache.Add(cacheKey, lastQueryResponse);
                        }

                        return lastQueryResponse;
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
                        var handle = HandleDnsResponseException(ex, request, settings, serverInfo, isLastServer: isLastServer, isLastTry: isLastTry, tries);

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
                        return lastQueryResponse;
                    }
                    catch (Exception ex) when (
                        ex is TimeoutException timeoutEx
                        || handler.IsTransientException(ex)
                        || ex is OperationCanceledException)
                    {
                        // if user's token got canceled, throw right away.
                        cancellationToken.ThrowIfCancellationRequested();

                        var handle = HandleTimeoutException(ex, request, settings, serverInfo, isLastServer: isLastServer, isLastTry: isLastTry, currentTry: tries);

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

                        var handle = HandleUnhandledException(ex, request, serverInfo, isLastServer);

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
                } while (tries <= settings.Retries && !cancellationToken.IsCancellationRequested);
            } // next server

            cancellationToken.ThrowIfCancellationRequested();

            // 1.3.0: With the error handling, this should never be reached.
            throw new DnsResponseException(DnsResponseCode.ConnectionTimeout, $"No connection could be established to any of the following name servers: {string.Join(", ", servers)}.")
            {
                AuditTrail = audit?.Build()
            };
        }

        private enum HandleError
        {
            None,
            Throw,
            RetryCurrentServer,
            RetryNextServer,
            ReturnResponse
        }

        private HandleError HandleDnsResponseException(DnsResponseException ex, DnsRequestMessage request, DnsQuerySettings settings, NameServer nameServer, bool isLastServer, bool isLastTry, int currentTry)
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
                var message = "Query {0} => {1} on {2} returned a response error '{3}'.";

                switch (handle)
                {
                    case HandleError.Throw:
                        eventId = c_eventQueryFail;
                        message += " Throwing the error.";
                        break;

                    case HandleError.ReturnResponse:
                        eventId = c_eventQueryReturnResponseError;
                        message += " Returning response.";
                        break;

                    case HandleError.RetryCurrentServer:
                        eventId = c_eventQueryRetryErrorSameServer;
                        message += " Re-trying {4}/{5}....";
                        break;

                    case HandleError.RetryNextServer:
                        eventId = c_eventQueryRetryErrorNextServer;
                        message += " Trying next server.";
                        break;
                }

                if (handle == HandleError.RetryCurrentServer)
                {
                    _logger.LogInformation(eventId, message, request.Header.Id, request.Question, nameServer, ex.DnsError, currentTry + 1, settings.Retries + 1);
                }
                else
                {
                    _logger.LogInformation(eventId, message, request.Header.Id, request.Question, nameServer, ex.DnsError);
                }
            }

            return handle;
        }

        private HandleError HandleDnsResponeParseException(DnsResponseParseException ex, DnsRequestMessage request, DnsMessageHandleType handleType, bool isLastServer)
        {
            // Don't try to fallback to TCP if we already are on TCP
            if (handleType == DnsMessageHandleType.UDP
                // Assuming that if we only got 512 or less bytes, its probably some network issue.
                && (ex.ResponseData.Length <= DnsQueryOptions.MinimumBufferSize
                // Second assumption: If the parser tried to read outside the provided data, this might also be a network issue.
                || ex.ReadLength + ex.Index > ex.ResponseData.Length))
            {
                // lets assume the response was truncated and retry with TCP.
                // (Not retrying other servers as it is very unlikely they would provide better results on this network)
                this._logger.LogError(
                    c_eventQueryBadTruncation,
                    ex,
                    "Query {0} => {1} error parsing the response. The response seems to be truncated without TC flag set! Re-trying via TCP anyways.",
                    request.Header.Id,
                    request.Question);

                // In this case the caller should return TruncatedResponseMessage
                return HandleError.ReturnResponse;
            }

            if (isLastServer)
            {
                this._logger.LogError(
                    c_eventQueryFail,
                    ex,
                    "Query {0} => {1} error parsing the response. Throwing the error.",
                    request.Header.Id,
                    request.Question);

                return HandleError.Throw;
            }

            // Otherwise, lets continue at least with the next server
            this._logger.LogWarning(
                c_eventQueryRetryErrorNextServer,
                ex,
                "Query {0} => {1} error parsing the response. Trying next server.",
                request.Header.Id,
                request.Question);

            return HandleError.RetryNextServer;
        }

        private HandleError HandleTimeoutException(Exception ex, DnsRequestMessage request, DnsQuerySettings settings, NameServer nameServer, bool isLastServer, bool isLastTry, int currentTry)
        {
            if (isLastTry && isLastServer)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        c_eventQueryFail,
                        ex,
                        "Query {0} => {1} on {2} timed out or is a transient error. Throwing the error.",
                        request.Header.Id,
                        request.Question,
                        nameServer,
                        currentTry,
                        settings.Retries + 1);
                }

                return HandleError.Throw;
            }
            else if (isLastTry)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        c_eventQueryRetryErrorNextServer,
                        ex,
                        "Query {0} => {1} on {2} timed out or is a transient error. Trying next server",
                        request.Header.Id,
                        request.Question,
                        nameServer);
                }

                return HandleError.RetryNextServer;
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    c_eventQueryRetryErrorSameServer,
                    ex,
                    "Query {0} => {1} on {2} timed out or is a transient error. Re-trying {3}/{4}...",
                    request.Header.Id,
                    request.Question,
                    nameServer,
                    currentTry,
                    settings.Retries + 1);
            }

            return HandleError.RetryCurrentServer;
        }

        private HandleError HandleUnhandledException(Exception ex, DnsRequestMessage request, NameServer nameServer, bool isLastServer)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    isLastServer ? c_eventQueryFail : c_eventQueryRetryErrorNextServer,
                    ex,
                    "Query {0} => {1} on {2} failed with an error."
                        + (isLastServer ? " Throwing the error." : " Trying next server."),
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
            int serverCount)
        {
            if (response.Header.ResultTruncated)
            {
                audit?.AuditTruncatedRetryTcp();

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(c_eventQueryTruncated, "Query {0} => {1} using UDP was truncated, re-trying with TCP.", request.Header.Id, request.Question);
                }

                return new TruncatedQueryResponse();
            }

            if (request.Header.Id != response.Header.Id)
            {
                throw new DnsResponseException("Header id mismatch.");
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
                        c_eventQuerySuccess,
                        "Query {0} => {1} on {2} received result with {3} answers.",
                        request.Header.Id,
                        request.Question,
                        nameServer,
                        response.Answers.Count);
                }
            }

            HandleOptRecords(settings, audit, nameServer, response);
            return response.AsQueryResponse(nameServer, settings);
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
                        _logger.LogDebug(c_eventQueryCachedResult, "Got cached result for query {0} => {1}.", request.Header.Id, request.Question);
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
                    _logger.LogWarning(c_eventResponseMissingOpt, "Response {0} => {1} is missing the requested OPT record.", response.Header.Id, response.Questions.FirstOrDefault());
                }
                else if (record is OptRecord optRecord)
                {
                    audit?.AuditOptPseudo();

                    serverInfo.SupportedUdpPayloadSize = optRecord.UdpSize;

                    audit?.AuditEdnsOpt(optRecord.UdpSize, optRecord.Version, optRecord.IsDnsSecOk, optRecord.ResponseCodeEx);

                    _logger.LogDebug(
                        c_eventResponseOpt,
                        "Response {0} => {1} opt record sets buffer of {2} to {3}.",
                        response.Header.Id,
                        response.Questions.FirstOrDefault(),
                        serverInfo,
                        optRecord.UdpSize);
                }
            }
        }

        private static DnsQuestion GetReverseQuestion(IPAddress ipAddress)
        {
            if (ipAddress == null)
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            var arpa = ipAddress.GetArpaName();
            return new DnsQuestion(arpa, QueryType.PTR, QueryClass.IN);
        }
    }

    internal class LookupClientAudit
    {
        private static readonly int s_printOffset = -32;
        private StringBuilder _auditWriter = new StringBuilder();
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

            _auditWriter.AppendLine($"; EDNS: version: {version}, flags:{(doFlag ? " do" : string.Empty)}; udp: {udpSize}; code: {responseCode}");
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

            // TODO: find better way to print the actual ttl of cached values
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

            var aggEx = ex as AggregateException;
            if (ex is DnsResponseException dnsEx)
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

        public void AuditRetryNextServer(NameServer current)
        {
            if (!Settings.EnableAuditTrail)
            {
                return;
            }

            _auditWriter.AppendLine();
            _auditWriter.AppendLine($"; SERVER: {current.Address}#{current.Port} failed; Retrying with the next server.");
        }
    }
}