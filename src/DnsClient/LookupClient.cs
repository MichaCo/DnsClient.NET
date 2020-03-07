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
        private const int c_eventQueryTruncated = 3;
        private const int c_eventQuerySuccess = 10;
        private const int c_eventQueryCachedResult = 11;
        private const int c_eventQueryResponseError = 40;
        private const int c_eventQueryRetryError = 50;
        private const int c_eventQueryFail = 60;
        private readonly DnsMessageHandler _messageHandler;
        private readonly DnsMessageHandler _tcpFallbackHandler;
        private readonly ILogger _logger;
        private readonly Random _random = new Random();

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

            // TODO: revisit, do we need this check? Maybe throw on query instead, in case no default name servers nor the per query settings have any defined.
            ////if (Settings.NameServers == null || Settings.NameServers.Count == 0)
            ////{
            ////    throw new ArgumentException("At least one name server must be configured.", nameof(options));
            ////}

            _messageHandler = udpHandler ?? new DnsUdpMessageHandler(true);
            _tcpFallbackHandler = tcpHandler ?? new DnsTcpMessageHandler();
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

        private DnsQueryAndServerSettings GetSettings(DnsQueryAndServerOptions queryOptions = null)
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

        private IDnsQueryResponse QueryInternal(DnsQuestion question, DnsQuerySettings settings, IReadOnlyCollection<NameServer> useServers)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var head = new DnsRequestHeader(GetNextUniqueId(), settings.Recursion, DnsOpCode.Query);
            var request = new DnsRequestMessage(head, question, settings);
            var handler = settings.UseTcpOnly ? _tcpFallbackHandler : _messageHandler;

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(c_eventStartQuery, "Begin query {0} => {1} on [{2}]", head, question, string.Join(", ", useServers));
            }

            return ResolveQuery(useServers, settings, handler, request);
        }

        private Task<IDnsQueryResponse> QueryInternalAsync(DnsQuestion question, DnsQuerySettings settings, IReadOnlyCollection<NameServer> useServers, CancellationToken cancellationToken = default)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var head = new DnsRequestHeader(GetNextUniqueId(), settings.Recursion, DnsOpCode.Query);
            var request = new DnsRequestMessage(head, question, settings);
            var handler = settings.UseTcpOnly ? _tcpFallbackHandler : _messageHandler;

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(c_eventStartQuery, "Begin query {0} => {1} on [{2}].", head.Id, question, string.Join(", ", useServers));
            }

            return ResolveQueryAsync(useServers, settings, handler, request, cancellationToken: cancellationToken);
        }

        // making it internal for unit testing
        internal IDnsQueryResponse ResolveQuery(IReadOnlyCollection<NameServer> servers, DnsQuerySettings settings, DnsMessageHandler handler, DnsRequestMessage request, LookupClientAudit continueAudit = null)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (servers == null || servers.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(servers), "List of configured name servers must not be empty.");
            }

            LookupClientAudit audit = null;
            if (settings.EnableAuditTrail)
            {
                audit = continueAudit ?? new LookupClientAudit(settings);
            }

            DnsResponseException lastDnsResponseException = null;
            Exception lastException = null;
            DnsQueryResponse lastQueryResponse = null;

            foreach (var serverInfo in servers)
            {
                var cacheKey = string.Empty;
                if (settings.UseCache)
                {
                    cacheKey = ResponseCache.GetCacheKey(request.Question);
                    var item = Cache.Get(cacheKey);
                    if (item != null)
                    {
                        if (_logger.IsEnabled(LogLevel.Debug))
                        {
                            _logger.LogDebug(c_eventQueryCachedResult, "Got cached result for query {0} => {1}.", request.Header.Id, request.Question);
                        }

                        return item;
                    }
                }

                var tries = 0;
                do
                {
                    tries++;
                    lastDnsResponseException = null;
                    lastException = null;

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

                        response.Audit = audit;

                        if (response.Header.ResultTruncated && settings.UseTcpFallback && !handler.GetType().Equals(typeof(DnsTcpMessageHandler)))
                        {
                            audit?.AuditTruncatedRetryTcp();

                            if (_logger.IsEnabled(LogLevel.Information))
                            {
                                _logger.LogInformation(c_eventQueryTruncated, "Query {0} => {1} using UDP was truncated, re-trying with TCP.", request.Header.Id, request.Question);
                            }

                            return ResolveQuery(new[] { serverInfo }, settings, _tcpFallbackHandler, request, audit);
                        }

                        audit?.AuditResolveServers(servers.Count);
                        audit?.AuditResponseHeader(response.Header);

                        if (response.Header.ResponseCode != DnsResponseCode.NoError)
                        {
                            if (settings.EnableAuditTrail)
                            {
                                audit.AuditResponseError(response.Header.ResponseCode);
                            }

                            if (_logger.IsEnabled(LogLevel.Information))
                            {
                                _logger.LogInformation(
                                    c_eventQueryResponseError,
                                    "Query {0} => {1} got a response error {2} from the server {3}.",
                                    request.Header.Id,
                                    request.Question,
                                    response.Header.ResponseCode,
                                    serverInfo);
                            }
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
                                    serverInfo,
                                    response.Answers.Count);
                            }
                        }

                        HandleOptRecords(settings, audit, serverInfo, response);

                        audit?.AuditResponse();
                        audit?.AuditEnd(response, serverInfo);

                        DnsQueryResponse queryResponse = response.AsQueryResponse(serverInfo, settings);
                        lastQueryResponse = queryResponse;

                        if (response.Header.ResponseCode != DnsResponseCode.NoError &&
                            (settings.ThrowDnsErrors || settings.ContinueOnDnsError))
                        {
                            throw new DnsResponseException(response.Header.ResponseCode);
                        }

                        if (settings.UseCache)
                        {
                            Cache.Add(cacheKey, queryResponse);
                        }

                        return queryResponse;
                    }
                    catch (DnsResponseException ex)
                    {
                        ex.AuditTrail = audit?.Build(null);
                        lastDnsResponseException = ex;

                        if (settings.ContinueOnDnsError)
                        {
                            if (ex.Code == DnsResponseCode.ConnectionTimeout
                                || ex.Code == DnsResponseCode.FormatError)
                            {
                                if (_logger.IsEnabled(LogLevel.Information))
                                {
                                    _logger.LogInformation(
                                        c_eventQueryRetryError,
                                        "Query {0} => {1} on {2} returned a response error. Re-trying {3}/{4}...",
                                        request.Header.Id,
                                        request.Question,
                                        serverInfo,
                                        tries,
                                        settings.Retries + 1);
                                }

                                continue;
                            }

                            if (_logger.IsEnabled(LogLevel.Information))
                            {
                                _logger.LogInformation(
                                    c_eventQueryRetryError,
                                    "Query {0} => {1} on {2} returned a response error. Trying next server (if any)...",
                                    request.Header.Id,
                                    request.Question,
                                    serverInfo);
                            }

                            break;
                        }

                        if (_logger.IsEnabled(LogLevel.Information))
                        {
                            _logger.LogInformation(
                                c_eventQueryFail,
                                ex,
                                "Query {0} => {1} on {2} returned a response error, throwing the error because ContinueOnDnsError=True.",
                                request.Header.Id,
                                request.Question,
                                serverInfo);
                        }

                        throw;
                    }
                    catch (DnsResponseParseException ex)
                    {
                        // Response parsing can be retried on the same server...
                        lastException = ex;
                        this._logger.LogWarning(
                            c_eventQueryRetryError,
                            ex,
                            "Query {0} => {1} error parsing the response. Re-trying {0}/{1}...",
                            request.Header.Id,
                            request.Question,
                            tries,
                            settings.Retries + 1);

                        continue;
                    }
                    catch (SocketException ex) when (
                        ex.SocketErrorCode == SocketError.AddressFamilyNotSupported
                        || ex.SocketErrorCode == SocketError.ConnectionRefused
                        || ex.SocketErrorCode == SocketError.ConnectionReset)
                    {
                        if (_logger.IsEnabled(LogLevel.Warning))
                        {
                            _logger.LogWarning(
                                c_eventQueryFail,
                                ex,
                                "Query {0} => {1} on {2} failed to connect. Trying next server (if any)...",
                                request.Header.Id,
                                request.Question,
                                serverInfo);
                        }

                        break;
                    }
                    catch (Exception ex) when (
                        ex is TimeoutException
                        || handler.IsTransientException(ex)
                        || ex is OperationCanceledException)
                    {
                        if (_logger.IsEnabled(LogLevel.Information))
                        {
                            _logger.LogInformation(
                                c_eventQueryFail,
                                ex,
                                "Query {0} => {1} on {2} timed out or is a transient error. Re-trying {3}/{4}...",
                                request.Header.Id,
                                request.Question,
                                serverInfo,
                                tries,
                                settings.Retries + 1);
                        }

                        continue;
                    }
                    catch (Exception ex)
                    {
                        audit?.AuditException(ex);

                        lastException = ex;

                        if (_logger.IsEnabled(LogLevel.Warning))
                        {
                            _logger.LogWarning(
                                c_eventQueryFail,
                                ex,
                                "Query {0} => {1} on {2} failed with an unhandled error. Trying next server (if any)...",
                                request.Header.Id,
                                request.Question,
                                serverInfo);
                        }

                        break;
                    }
                } while (tries <= settings.Retries);

                if (settings.EnableAuditTrail && servers.Count > 1 && serverInfo != servers.Last())
                {
                    audit?.AuditRetryNextServer(serverInfo);
                }
            }

            if (lastDnsResponseException != null && settings.ThrowDnsErrors)
            {
                throw lastDnsResponseException;
            }

            if (lastQueryResponse != null)
            {
                return lastQueryResponse;
            }

            if (lastException != null)
            {
                throw new DnsResponseException(DnsResponseCode.Unassigned, "Unhandled exception", lastException)
                {
                    AuditTrail = audit?.Build(null)
                };
            }

            throw new DnsResponseException(DnsResponseCode.ConnectionTimeout, $"No connection could be established to any of the following name servers: {string.Join(", ", servers)}.")
            {
                AuditTrail = audit?.Build(null)
            };
        }

        internal async Task<IDnsQueryResponse> ResolveQueryAsync(IReadOnlyCollection<NameServer> servers, DnsQuerySettings settings, DnsMessageHandler handler, DnsRequestMessage request, LookupClientAudit continueAudit = null, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (servers == null || servers.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(servers), "List of configured name servers must not be empty.");
            }

            LookupClientAudit audit = null;
            if (settings.EnableAuditTrail)
            {
                audit = continueAudit ?? new LookupClientAudit(settings);
            }

            DnsResponseException lastDnsResponseException = null;
            Exception lastException = null;
            DnsQueryResponse lastQueryResponse = null;

            foreach (var serverInfo in servers)
            {
                var cacheKey = string.Empty;
                if (settings.UseCache)
                {
                    cacheKey = ResponseCache.GetCacheKey(request.Question);
                    var item = Cache.Get(cacheKey);
                    if (item != null)
                    {
                        if (_logger.IsEnabled(LogLevel.Debug))
                        {
                            _logger.LogDebug(c_eventQueryCachedResult, "Got cached result for query {0} => {1}.", request.Header.Id, request.Question);
                        }

                        return item;
                    }
                }

                var tries = 0;
                do
                {
                    tries++;
                    lastDnsResponseException = null;
                    lastException = null;

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
                        cancellationToken.ThrowIfCancellationRequested();

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

                        response.Audit = audit;

                        // TODO: better way to prevent infinity looping TCP calls (remove GetType.Equals...)
                        if (response.Header.ResultTruncated && settings.UseTcpFallback && !handler.GetType().Equals(typeof(DnsTcpMessageHandler)))
                        {
                            audit?.AuditTruncatedRetryTcp();

                            if (_logger.IsEnabled(LogLevel.Information))
                            {
                                _logger.LogInformation(c_eventQueryTruncated, "Query {0} => {1} using UDP was truncated, re-trying with TCP.", request.Header.Id, request.Question);
                            }

                            return await ResolveQueryAsync(new[] { serverInfo }, settings, _tcpFallbackHandler, request, audit, cancellationToken).ConfigureAwait(false);
                        }

                        audit?.AuditResolveServers(servers.Count);
                        audit?.AuditResponseHeader(response.Header);

                        if (response.Header.ResponseCode != DnsResponseCode.NoError)
                        {
                            if (settings.EnableAuditTrail)
                            {
                                audit?.AuditResponseError(response.Header.ResponseCode);
                            }

                            if (_logger.IsEnabled(LogLevel.Information))
                            {
                                _logger.LogInformation(
                                    c_eventQueryResponseError,
                                    "Query {0} => {1} got a response error {2} from the server {3}.",
                                    request.Header.Id,
                                    request.Question,
                                    response.Header.ResponseCode,
                                    serverInfo);
                            }
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
                                    serverInfo,
                                    response.Answers.Count);
                            }
                        }

                        HandleOptRecords(settings, audit, serverInfo, response);

                        audit?.AuditResponse();
                        audit?.AuditEnd(response, serverInfo);

                        DnsQueryResponse queryResponse = response.AsQueryResponse(serverInfo, settings);
                        lastQueryResponse = queryResponse;

                        if (response.Header.ResponseCode != DnsResponseCode.NoError &&
                            (settings.ThrowDnsErrors || settings.ContinueOnDnsError))
                        {
                            throw new DnsResponseException(response.Header.ResponseCode);
                        }

                        if (settings.UseCache)
                        {
                            Cache.Add(cacheKey, queryResponse);
                        }

                        return queryResponse;
                    }
                    catch (DnsResponseException ex)
                    {
                        ex.AuditTrail = audit?.Build(null);
                        lastDnsResponseException = ex;

                        if (settings.ContinueOnDnsError)
                        {
                            if (ex.Code == DnsResponseCode.ConnectionTimeout
                                || ex.Code == DnsResponseCode.FormatError)
                            {
                                if (_logger.IsEnabled(LogLevel.Information))
                                {
                                    _logger.LogInformation(
                                        c_eventQueryRetryError,
                                        "Query {0} => {1} on {2} returned a response error. Re-trying {3}/{4}...",
                                        request.Header.Id,
                                        request.Question,
                                        serverInfo,
                                        tries,
                                        settings.Retries + 1);
                                }

                                continue;
                            }

                            if (_logger.IsEnabled(LogLevel.Information))
                            {
                                _logger.LogInformation(
                                    c_eventQueryRetryError,
                                    "Query {0} => {1} on {2} returned a response error. Trying next server (if any)...",
                                    request.Header.Id,
                                    request.Question,
                                    serverInfo);
                            }

                            break;
                        }

                        if (_logger.IsEnabled(LogLevel.Information))
                        {
                            _logger.LogInformation(
                                c_eventQueryFail,
                                ex,
                                "Query {0} => {1} on {2} returned a response error, throwing the error because ContinueOnDnsError=True.",
                                request.Header.Id,
                                request.Question,
                                serverInfo);
                        }

                        throw;
                    }
                    catch (DnsResponseParseException ex)
                    {
                        // Response parsing can be retried on the same server...
                        lastException = ex;
                        this._logger.LogWarning(
                            c_eventQueryRetryError,
                            ex,
                            "Query {0} => {1} error parsing the response. Re-trying {0}/{1}...",
                            request.Header.Id,
                            request.Question,
                            tries,
                            settings.Retries + 1);

                        continue;
                    }
                    catch (SocketException ex) when (
                        ex.SocketErrorCode == SocketError.AddressFamilyNotSupported
                        || ex.SocketErrorCode == SocketError.ConnectionRefused
                        || ex.SocketErrorCode == SocketError.ConnectionReset)
                    {
                        if (_logger.IsEnabled(LogLevel.Warning))
                        {
                            _logger.LogWarning(
                                c_eventQueryFail,
                                ex,
                                "Query {0} => {1} on {2} failed to connect. Trying next server (if any)...",
                                request.Header.Id,
                                request.Question,
                                serverInfo);
                        }

                        break;
                    }
                    catch (Exception ex) when (
                        ex is TimeoutException timeoutEx
                        || handler.IsTransientException(ex)
                        || ex is OperationCanceledException)
                    {
                        // user's token got canceled, throw right away...
                        if (cancellationToken.IsCancellationRequested)
                        {
                            throw new OperationCanceledException(cancellationToken);
                        }

                        if (_logger.IsEnabled(LogLevel.Information))
                        {
                            _logger.LogInformation(
                                c_eventQueryFail,
                                ex,
                                "Query {0} => {1} on {2} timed out or is a transient error. Re-trying {3}/{4}...",
                                request.Header.Id,
                                request.Question,
                                serverInfo,
                                tries,
                                settings.Retries + 1);
                        }

                        continue;
                    }
                    catch (Exception ex)
                    {
                        if (ex is AggregateException agg)
                        {
                            agg.Handle((e) =>
                            {
                                if (e is TimeoutException
                                    || handler.IsTransientException(e)
                                    || e is OperationCanceledException)
                                {
                                    if (cancellationToken.IsCancellationRequested)
                                    {
                                        throw new OperationCanceledException(cancellationToken);
                                    }

                                    return true;
                                }

                                return false;
                            });
                        }

                        audit?.AuditException(ex);
                        lastException = ex;

                        if (_logger.IsEnabled(LogLevel.Warning))
                        {
                            _logger.LogWarning(
                                c_eventQueryFail,
                                ex,
                                "Query {0} => {1} on {2} failed with an unhandled error. Trying next server (if any)...",
                                request.Header.Id,
                                request.Question,
                                serverInfo);
                        }

                        break;
                    }
                } while (tries <= settings.Retries && !cancellationToken.IsCancellationRequested);

                if (settings.EnableAuditTrail && servers.Count > 1 && serverInfo != servers.Last())
                {
                    audit?.AuditRetryNextServer(serverInfo);
                }
            }

            if (lastDnsResponseException != null && settings.ThrowDnsErrors)
            {
                throw lastDnsResponseException;
            }

            if (lastQueryResponse != null)
            {
                return lastQueryResponse;
            }

            if (lastException != null)
            {
                throw new DnsResponseException(DnsResponseCode.Unassigned, "Unhandled exception", lastException)
                {
                    AuditTrail = audit?.Build(null)
                };
            }

            throw new DnsResponseException(DnsResponseCode.ConnectionTimeout, $"No connection could be established to any of the following name servers: {string.Join(", ", servers)}.")
            {
                AuditTrail = audit?.Build(null)
            };
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

        private void HandleOptRecords(DnsQuerySettings settings, LookupClientAudit audit, NameServer serverInfo, DnsResponseMessage response)
        {
            // TODO: add logging about the opt record
            var record = response.Additionals.OfRecordType(Protocol.ResourceRecordType.OPT).FirstOrDefault();

            if (record != null && record is OptRecord optRecord)
            {
                audit?.AuditOptPseudo();

                serverInfo.SupportedUdpPayloadSize = optRecord.UdpSize;

                audit?.AuditEdnsOpt(optRecord.UdpSize, optRecord.Version, optRecord.IsDnsSecOk, optRecord.ResponseCodeEx);
            }
        }

        private ushort GetNextUniqueId()
        {
            return (ushort)_random.Next(1, ushort.MaxValue);
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

        public string Build(IDnsQueryResponse queryResponse)
        {
            if (!Settings.EnableAuditTrail)
            {
                return string.Empty;
            }

            return _auditWriter.ToString();
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

        public void AuditResponseError(DnsResponseCode responseCode)
        {
            if (!Settings.EnableAuditTrail)
            {
                return;
            }

            _auditWriter.AppendLine($";; ERROR: {DnsResponseCodeText.GetErrorText(responseCode)}");
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

            // TODO: flags
            _auditWriter.AppendLine($"; EDNS: version: {version}, flags:{(doFlag ? " do" : string.Empty)}; udp: {udpSize}; code: {responseCode}");
        }

        public void AuditResponse()
        {
            ////    if (!Settings.EnableAuditTrail)
            ////    {
            ////        return;
            ////    }

            ////    _auditWriter.AppendLine(c_placeHolder);
        }

        public void AuditEnd(DnsResponseMessage queryResponse, NameServer nameServer)
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