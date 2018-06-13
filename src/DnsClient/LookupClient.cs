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
        private static readonly int s_serverHealthCheckInterval = (int)TimeSpan.FromSeconds(30).TotalMilliseconds;
        private static int _uniqueId = 0;
        private bool _healthCheckRunning = false;
        private int _lastHealthCheck = 0;
        private readonly ResponseCache _cache = new ResponseCache(true);

        ////private readonly object _endpointLock = new object();
        private readonly DnsMessageHandler _messageHandler;

        private readonly DnsMessageHandler _tcpFallbackHandler;
        private readonly ConcurrentQueue<NameServer> _endpoints;
        private readonly Random _random = new Random();
        private TimeSpan _timeout = s_defaultTimeout;

        /// <summary>
        /// Gets or sets a flag indicating whether Tcp should be used in case a Udp response is truncated.
        /// Default is <c>True</c>.
        /// <para>
        /// If <c>False</c>, truncated results will potentially yield no answers.
        /// </para>
        /// </summary>
        public bool UseTcpFallback { get; set; } = true;

        /// <summary>
        /// Gets or sets a flag indicating whether Udp should not be used at all.
        /// Default is <c>False</c>.
        /// <para>
        /// Enable this only if Udp cannot be used because of your firewall rules for example.
        /// </para>
        /// </summary>
        public bool UseTcpOnly { get; set; }

        /// <summary>
        /// Gets the list of configured name servers.
        /// </summary>
        public IReadOnlyCollection<NameServer> NameServers { get; }

        /// <summary>
        /// If enabled, each <see cref="IDnsQueryResponse"/> will contain a full documentation of the response(s).
        /// Default is <c>False</c>.
        /// </summary>
        /// <seealso cref="IDnsQueryResponse.AuditTrail"/>
        public bool EnableAuditTrail { get; set; } = false;

        /// <summary>
        /// Gets or sets a flag indicating whether DNS queries should instruct the DNS server to do recursive lookups, or not.
        /// Default is <c>True</c>.
        /// </summary>
        /// <value>The flag indicating if recursion should be used or not.</value>
        public bool Recursion { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of tries to get a response from one name server before trying the next one.
        /// Only transient errors, like network or connection errors will be retried.
        /// Default is <c>5</c>.
        /// <para>
        /// If all configured <see cref="NameServers"/> error out after retries, an exception will be thrown at the end.
        /// </para>
        /// </summary>
        /// <value>The number of retries.</value>
        public int Retries { get; set; } = 5;

        /// <summary>
        /// Gets or sets a flag indicating whether the <see cref="ILookupClient"/> should throw a <see cref="DnsResponseException"/>
        /// in case the query result has a <see cref="DnsResponseCode"/> other than <see cref="DnsResponseCode.NoError"/>.
        /// Default is <c>False</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If set to <c>False</c>, the query will return a result with an <see cref="IDnsQueryResponse.ErrorMessage"/>
        /// which contains more information.
        /// </para>
        /// <para>
        /// If set to <c>True</c>, any query method of <see cref="IDnsQuery"/> will throw an <see cref="DnsResponseException"/> if
        /// the response header indicates an error.
        /// </para>
        /// <para>
        /// If both, <see cref="ContinueOnDnsError"/> and <see cref="ThrowDnsErrors"/> are set to <c>True</c>,
        /// <see cref="ILookupClient"/> will continue to query all configured <see cref="NameServers"/>.
        /// If none of the servers yield a valid response, a <see cref="DnsResponseException"/> will be thrown
        /// with the error of the last response.
        /// </para>
        /// </remarks>
        /// <seealso cref="DnsResponseCode"/>
        /// <seealso cref="ContinueOnDnsError"/>
        public bool ThrowDnsErrors { get; set; } = false;

        /// <summary>
        /// Gets or sets the request timeout in milliseconds. <see cref="Timeout"/> is used for limiting the connection and request time for one operation.
        /// Timeout must be greater than zero and less than <see cref="int.MaxValue"/>.
        /// If <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> (or -1) is used, no timeout will be applied.
        /// Default is 5 seconds.
        /// </summary>
        /// <remarks>
        /// If a very short timeout is configured, queries will more likely result in <see cref="TimeoutException"/>s.
        /// <para>
        /// Important to note, <see cref="TimeoutException"/>s will be retried, if <see cref="Retries"/> are not disabled (set to <c>0</c>).
        /// This should help in case one or more configured DNS servers are not reachable or under load for example.
        /// </para>
        /// </remarks>
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
        /// Gets or sets a flag indicating if the <see cref="LookupClient"/> should use response caching or not.
        /// The cache duration is calculated by the resource record of the response. Usually, the lowest TTL is used.
        /// Default is <c>True</c>.
        /// </summary>
        /// <remarks>
        /// In case the DNS Server returns records with a TTL of zero. The response cannot be cached.
        /// Setting <see cref="MinimumCacheTimeout"/> can overwrite this behavior and cache those responses anyways for at least the given duration.
        /// </remarks>
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
        /// Default is <c>Null</c>.
        /// <para>
        /// This is useful in cases where the server retruns records with zero TTL.
        /// </para>
        /// </summary>
        /// <remarks>
        /// This setting gets igonred in case <see cref="UseCache"/> is set to <c>False</c>.
        /// The maximum value is 24 days or <see cref="Timeout.Infinite"/>.
        /// </remarks>
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
        /// Gets or sets a flag indicating whether the <see cref="ILookupClient"/> can cycle through all
        /// configured <see cref="NameServers"/> on each consecutive request, basically using a random server, or not.
        /// Default is <c>True</c>.
        /// If only one <see cref="NameServer"/> is configured, this setting is not used.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If <c>False</c>, configured endpoint will be used in random order.
        /// If <c>True</c>, the order will be preserved.
        /// </para>
        /// <para>
        /// Even if <see cref="UseRandomNameServer"/> is set to <c>True</c>, the endpoint might still get
        /// disabled and might not being used for some time if it errors out, e.g. no connection can be established.
        /// </para>
        /// </remarks>
        public bool UseRandomNameServer { get; set; } = true;

        /// <summary>
        /// Gets or sets a flag indicating whether to query the next configured <see cref="NameServers"/> in case the response of the last query
        /// returned a <see cref="DnsResponseCode"/> other than <see cref="DnsResponseCode.NoError"/>.
        /// Default is <c>True</c>.
        /// </summary>
        /// <remarks>
        /// If <c>True</c>, lookup client will continue until a server returns a valid result, or,
        /// if no <see cref="NameServers"/> yield a valid result, the last response with the error will be returned.
        /// In case no server yields a valid result and <see cref="ThrowDnsErrors"/> is also enabled, an exception
        /// will be thrown containing the error of the last response.
        /// </remarks>
        /// <seealso cref="ThrowDnsErrors"/>
        public bool ContinueOnDnsError { get; set; } = true;

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
            : this(nameServers?.Select(p => new IPEndPoint(p, NameServer.DefaultPort))?.ToArray())
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
            : this(nameServers?.Select(p => new NameServer(p))?.ToArray())
        {
        }

        // adding this one for unit testing
        internal LookupClient(params NameServer[] nameServers)
        {
            if (nameServers == null || nameServers.Length == 0)
            {
                throw new ArgumentException("At least one name server must be configured.", nameof(nameServers));
            }

            NameServers = nameServers;

            _endpoints = new ConcurrentQueue<NameServer>(NameServers);
            _messageHandler = new DnsUdpMessageHandler(true);
            _tcpFallbackHandler = new DnsTcpMessageHandler();
        }

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress"/>.
        /// </summary>
        /// <param name="ipAddress">The <see cref="IPAddress"/>.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="ipAddress"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="ILookupClient.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        public IDnsQueryResponse QueryReverse(IPAddress ipAddress)
        {
            if (ipAddress == null)
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            var arpa = ipAddress.GetArpaName();
            return Query(arpa, QueryType.PTR, QueryClass.IN);
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
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="ILookupClient.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        public Task<IDnsQueryResponse> QueryReverseAsync(IPAddress ipAddress, CancellationToken cancellationToken = default)
        {
            if (ipAddress == null)
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            var arpa = ipAddress.GetArpaName();
            return QueryAsync(arpa, QueryType.PTR, QueryClass.IN, cancellationToken);
        }

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress" />
        /// using only the passed in <paramref name="servers"/> with the <see cref="NameServer.DefaultPort"/>.
        /// </summary>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="ipAddress">The <see cref="IPAddress" />.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="ipAddress"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="ILookupClient.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        public IDnsQueryResponse QueryServerReverse(IReadOnlyCollection<IPAddress> servers, IPAddress ipAddress)
            => QueryServerReverse(servers?.Select(p => new IPEndPoint(p, NameServer.DefaultPort))?.ToArray(), ipAddress);

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="ipAddress">The <see cref="IPAddress" />.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="ipAddress"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="ILookupClient.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        public IDnsQueryResponse QueryServerReverse(IReadOnlyCollection<IPEndPoint> servers, IPAddress ipAddress)
        {
            if (ipAddress == null)
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            var arpa = ipAddress.GetArpaName();
            return QueryServer(servers, arpa, QueryType.PTR, QueryClass.IN);
        }

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress" />
        /// using only the passed in <paramref name="servers"/> with the <see cref="NameServer.DefaultPort"/>.
        /// </summary>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="ipAddress">The <see cref="IPAddress" />.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="ipAddress"/> is null.</exception>
        /// <exception cref="OperationCanceledException">If cancellation has been requested for the passed in <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="ILookupClient.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        public Task<IDnsQueryResponse> QueryServerReverseAsync(IReadOnlyCollection<IPAddress> servers, IPAddress ipAddress, CancellationToken cancellationToken = default)
            => QueryServerReverseAsync(servers?.Select(p => new IPEndPoint(p, NameServer.DefaultPort))?.ToArray(), ipAddress, cancellationToken);

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="ipAddress">The <see cref="IPAddress" />.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="ipAddress"/> is null.</exception>
        /// <exception cref="OperationCanceledException">If cancellation has been requested for the passed in <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="ILookupClient.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        public Task<IDnsQueryResponse> QueryServerReverseAsync(IReadOnlyCollection<IPEndPoint> servers, IPAddress ipAddress, CancellationToken cancellationToken = default)
        {
            if (ipAddress == null)
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            var arpa = ipAddress.GetArpaName();
            return QueryServerAsync(servers, arpa, QueryType.PTR, QueryClass.IN);
        }

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="query" />, <paramref name="queryType" /> and <paramref name="queryClass" />
        /// </summary>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <param name="queryClass">The <see cref="QueryClass"/>.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="ILookupClient.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        /// <remarks>
        /// The behavior of the query can be controlled by the properties of this <see cref="LookupClient"/> instance.
        /// <see cref="Recursion"/> for example can be disabled and would instruct the DNS server to return no additional records.
        /// </remarks>
        public IDnsQueryResponse Query(string query, QueryType queryType, QueryClass queryClass = QueryClass.IN)
            => QueryInternal(GetNextServers(), new DnsQuestion(query, queryType, queryClass));

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="query" />, <paramref name="queryType" /> and <paramref name="queryClass" />
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
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="ILookupClient.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        /// <remarks>
        /// The behavior of the query can be controlled by the properties of this <see cref="LookupClient"/> instance.
        /// <see cref="Recursion"/> for example can be disabled and would instruct the DNS server to return no additional records.
        /// </remarks>
        public Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default)
            => QueryInternalAsync(GetNextServers(), new DnsQuestion(query, queryType, queryClass), cancellationToken);

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="query" />, <paramref name="queryType" /> and <paramref name="queryClass" />
        /// using only the passed in <paramref name="servers"/> with the <see cref="NameServer.DefaultPort"/>.
        /// </summary>
        /// <remarks>
        /// To query specific servers can be useful in cases where you have to use a different DNS server than initially configured
        /// (without creating a new instance of <see cref="ILookupClient"/> for example).
        /// </remarks>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <param name="queryClass">The <see cref="QueryClass" />.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="ILookupClient.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        public IDnsQueryResponse QueryServer(IReadOnlyCollection<IPAddress> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN)
            => QueryServer(servers?.Select(p => new IPEndPoint(p, NameServer.DefaultPort))?.ToArray(), query, queryType, queryClass);

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
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="ILookupClient.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        public IDnsQueryResponse QueryServer(IReadOnlyCollection<IPEndPoint> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN)
            => QueryInternal(servers?.Select(p => new NameServer(p))?.ToArray(), new DnsQuestion(query, queryType, queryClass));

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="query" />, <paramref name="queryType" /> and <paramref name="queryClass" />
        /// using only the passed in <paramref name="servers"/> with the <see cref="NameServer.DefaultPort"/>.
        /// </summary>
        /// <remarks>
        /// To query specific servers can be useful in cases where you have to use a different DNS server than initially configured
        /// (without creating a new instance of <see cref="ILookupClient"/> for example).
        /// </remarks>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <param name="queryClass">The <see cref="QueryClass" />.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="OperationCanceledException">If cancellation has been requested for the passed in <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="ILookupClient.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        public Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<IPAddress> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default)
            => QueryServerAsync(servers?.Select(p => new IPEndPoint(p, NameServer.DefaultPort))?.ToArray(), query, queryType, queryClass, cancellationToken);

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
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="OperationCanceledException">If cancellation has been requested for the passed in <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="ILookupClient.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        public Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<IPEndPoint> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default)
            => QueryInternalAsync(servers?.Select(p => new NameServer(p))?.ToArray(), new DnsQuestion(query, queryType, queryClass), cancellationToken);

        private IDnsQueryResponse QueryInternal(IReadOnlyCollection<NameServer> servers, DnsQuestion question, bool useCache = true)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            var head = new DnsRequestHeader(GetNextUniqueId(), Recursion, DnsOpCode.Query);
            var request = new DnsRequestMessage(head, question);
            var handler = UseTcpOnly ? _tcpFallbackHandler : _messageHandler;

            return ResolveQuery(servers, handler, request, useCache);
        }

        private Task<IDnsQueryResponse> QueryInternalAsync(IReadOnlyCollection<NameServer> servers, DnsQuestion question, CancellationToken cancellationToken = default, bool useCache = true)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            var head = new DnsRequestHeader(GetNextUniqueId(), Recursion, DnsOpCode.Query);
            var request = new DnsRequestMessage(head, question);
            var handler = UseTcpOnly ? _tcpFallbackHandler : _messageHandler;

            return ResolveQueryAsync(servers, handler, request, useCache, cancellationToken: cancellationToken);
        }

        // making it internal for unit testing
        internal IDnsQueryResponse ResolveQuery(IReadOnlyCollection<NameServer> servers, DnsMessageHandler handler, DnsRequestMessage request, bool useCache, LookupClientAudit continueAudit = null)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }
            if (servers.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(servers), "List of servers must not be empty.");
            }

            var audit = continueAudit ?? new LookupClientAudit();

            DnsResponseException lastDnsResponseException = null;
            Exception lastException = null;
            DnsQueryResponse lastQueryResponse = null;

            foreach (var serverInfo in servers)
            {
                var cacheKey = string.Empty;
                if (_cache.Enabled && useCache)
                {
                    cacheKey = ResponseCache.GetCacheKey(request.Question, serverInfo);
                    var item = _cache.Get(cacheKey);
                    if (item != null)
                    {
                        return item;
                    }
                }

                var tries = 0;
                do
                {
                    tries++;
                    lastDnsResponseException = null;
                    lastException = null;

                    try
                    {
                        if (EnableAuditTrail)
                        {
                            audit.StartTimer();
                        }

                        DnsResponseMessage response = handler.Query(serverInfo.Endpoint, request, Timeout);

                        response.Audit = audit;

                        if (response.Header.ResultTruncated && UseTcpFallback && !handler.GetType().Equals(typeof(DnsTcpMessageHandler)))
                        {
                            if (EnableAuditTrail)
                            {
                                audit.AuditTruncatedRetryTcp();
                            }

                            return ResolveQuery(new[] { serverInfo }, _tcpFallbackHandler, request, useCache, audit);
                        }

                        if (EnableAuditTrail)
                        {
                            audit.AuditResolveServers(servers.Count);
                            audit.AuditResponseHeader(response.Header);
                        }

                        if (response.Header.ResponseCode != DnsResponseCode.NoError && EnableAuditTrail)
                        {
                            audit.AuditResponseError(response.Header.ResponseCode);
                        }

                        HandleOptRecords(audit, serverInfo, response);

                        DnsQueryResponse queryResponse = response.AsQueryResponse(serverInfo.Clone());

                        if (EnableAuditTrail)
                        {
                            audit.AuditResponse();
                            audit.AuditEnd(queryResponse);
                        }

                        serverInfo.Enabled = true;
                        serverInfo.LastSuccessfulRequest = request;
                        lastQueryResponse = queryResponse;

                        if (response.Header.ResponseCode != DnsResponseCode.NoError &&
                            (ThrowDnsErrors || ContinueOnDnsError))
                        {
                            throw new DnsResponseException(response.Header.ResponseCode);
                        }

                        if (_cache.Enabled && useCache)
                        {
                            _cache.Add(cacheKey, queryResponse);
                        }

                        return queryResponse;
                    }
                    catch (DnsResponseException ex)
                    {
                        ////audit.AuditException(ex);
                        ex.AuditTrail = audit.Build(null);
                        lastDnsResponseException = ex;

                        if (ContinueOnDnsError)
                        {
                            break; // don't retry this server, response was kinda valid
                        }

                        throw ex;
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressFamilyNotSupported)
                    {
                        // this socket error might indicate the server endpoint is actually bad and should be ignored in future queries.
                        DisableServer(serverInfo);
                        break;
                    }
                    catch (Exception ex) when (
                        ex is TimeoutException
                        || handler.IsTransientException(ex)
                        || ex is OperationCanceledException
                        || ex is TaskCanceledException)
                    {
                        DisableServer(serverInfo);
                        continue;
                        // retrying the same server...
                    }
                    catch (Exception ex)
                    {
                        DisableServer(serverInfo);

                        audit.AuditException(ex);

                        lastException = ex;

                        // not retrying the same server, use next or return
                        break;
                    }
                } while (tries <= Retries && serverInfo.Enabled);

                if (servers.Count > 1 && serverInfo != servers.Last())
                {
                    audit.AuditRetryNextServer(serverInfo);
                }
            }

            if (lastDnsResponseException != null && ThrowDnsErrors)
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
                    AuditTrail = audit.Build(null)
                };
            }

            throw new DnsResponseException(DnsResponseCode.ConnectionTimeout, $"No connection could be established to any of the following name servers: {string.Join(", ", NameServers)}.")
            {
                AuditTrail = audit.Build(null)
            };
        }

        internal async Task<IDnsQueryResponse> ResolveQueryAsync(IReadOnlyCollection<NameServer> servers, DnsMessageHandler handler, DnsRequestMessage request, bool useCache, LookupClientAudit continueAudit = null, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }
            if (servers.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(servers), "List of servers must not be empty.");
            }

            var audit = continueAudit ?? new LookupClientAudit();

            DnsResponseException lastDnsResponseException = null;
            Exception lastException = null;
            DnsQueryResponse lastQueryResponse = null;

            foreach (var serverInfo in servers)
            {
                var cacheKey = string.Empty;
                if (_cache.Enabled && useCache)
                {
                    cacheKey = ResponseCache.GetCacheKey(request.Question, serverInfo);
                    var item = _cache.Get(cacheKey);
                    if (item != null)
                    {
                        return item;
                    }
                }

                var tries = 0;
                do
                {
                    tries++;
                    lastDnsResponseException = null;
                    lastException = null;

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

                        response.Audit = audit;

                        if (response.Header.ResultTruncated && UseTcpFallback && !handler.GetType().Equals(typeof(DnsTcpMessageHandler)))
                        {
                            if (EnableAuditTrail)
                            {
                                audit.AuditTruncatedRetryTcp();
                            }

                            return await ResolveQueryAsync(new[] { serverInfo }, _tcpFallbackHandler, request, useCache, audit, cancellationToken).ConfigureAwait(false);
                        }

                        if (EnableAuditTrail)
                        {
                            audit.AuditResolveServers(servers.Count);
                            audit.AuditResponseHeader(response.Header);
                        }

                        if (response.Header.ResponseCode != DnsResponseCode.NoError && EnableAuditTrail)
                        {
                            audit.AuditResponseError(response.Header.ResponseCode);
                        }

                        HandleOptRecords(audit, serverInfo, response);

                        DnsQueryResponse queryResponse = response.AsQueryResponse(serverInfo.Clone());

                        if (EnableAuditTrail)
                        {
                            audit.AuditResponse();
                            audit.AuditEnd(queryResponse);
                        }

                        // got a valid result, lets enabled the server again if it was disabled
                        serverInfo.Enabled = true;
                        lastQueryResponse = queryResponse;
                        serverInfo.LastSuccessfulRequest = request;

                        if (response.Header.ResponseCode != DnsResponseCode.NoError &&
                            (ThrowDnsErrors || ContinueOnDnsError))
                        {
                            throw new DnsResponseException(response.Header.ResponseCode);
                        }

                        if (_cache.Enabled && useCache)
                        {
                            _cache.Add(cacheKey, queryResponse);
                        }

                        return queryResponse;
                    }
                    catch (DnsResponseException ex)
                    {
                        ex.AuditTrail = audit.Build(null);
                        lastDnsResponseException = ex;

                        if (ContinueOnDnsError)
                        {
                            break; // don't retry this server, response was kinda valid
                        }

                        throw;
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressFamilyNotSupported)
                    {
                        // this socket error might indicate the server endpoint is actually bad and should be ignored in future queries.
                        DisableServer(serverInfo);
                        break;
                    }
                    catch (Exception ex) when (
                        ex is TimeoutException timeoutEx
                        || handler.IsTransientException(ex)
                        || ex is OperationCanceledException
                        || ex is TaskCanceledException)
                    {
                        // user's token got canceled, throw right away...
                        if (cancellationToken.IsCancellationRequested)
                        {
                            throw new OperationCanceledException(cancellationToken);
                        }

                        DisableServer(serverInfo);
                    }
                    catch (Exception ex)
                    {
                        DisableServer(serverInfo);

                        if (ex is AggregateException agg)
                        {
                            agg.Handle((e) =>
                            {
                                if (e is TimeoutException
                                    || handler.IsTransientException(e)
                                    || e is OperationCanceledException
                                    || e is TaskCanceledException)
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

                        audit.AuditException(ex);
                        lastException = ex;

                        // try next server (this is actually a change and is not configurable, but should be a good thing I guess)
                        break;
                    }
                } while (tries <= Retries && !cancellationToken.IsCancellationRequested && serverInfo.Enabled);

                if (servers.Count > 1 && serverInfo != servers.Last())
                {
                    audit.AuditRetryNextServer(serverInfo);
                }
            }

            if (lastDnsResponseException != null && ThrowDnsErrors)
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
                    AuditTrail = audit.Build(null)
                };
            }

            throw new DnsResponseException(DnsResponseCode.ConnectionTimeout, $"No connection could be established to any of the following name servers: {string.Join(", ", NameServers)}.")
            {
                AuditTrail = audit.Build(null)
            };
        }

        private void HandleOptRecords(LookupClientAudit audit, NameServer serverInfo, DnsResponseMessage response)
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

        // internal for unit testing
        internal IReadOnlyCollection<NameServer> GetNextServers()
        {
            IReadOnlyCollection<NameServer> servers = null;
            if (_endpoints.Count > 1)
            {
                servers = _endpoints.Where(p => p.Enabled).ToArray();

                // if all servers are disabled, retry all of them
                if (servers.Count == 0)
                {
                    servers = _endpoints.ToArray();
                }

                // shuffle servers only if we do not have to preserve the order
                if (UseRandomNameServer)
                {
                    if (_endpoints.TryDequeue(out NameServer server))
                    {
                        _endpoints.Enqueue(server);
                    }
                }

                RunHealthCheck();
            }
            else
            {
                servers = _endpoints.ToArray();
            }

            return servers;
        }

        private void RunHealthCheck()
        {
            // TickCount jump every 25days to int.MinValue, adjusting...
            var currentTicks = Environment.TickCount & int.MaxValue;
            if (_lastHealthCheck + s_serverHealthCheckInterval < 0 || currentTicks + s_serverHealthCheckInterval < 0) _lastHealthCheck = 0;
            if (!_healthCheckRunning && _lastHealthCheck + s_serverHealthCheckInterval < currentTicks)
            {
                _lastHealthCheck = currentTicks;

                var source = new CancellationTokenSource(TimeSpan.FromMinutes(1));

                Task.Factory.StartNew(
                    state => DoHealthCheck((CancellationToken)state),
                    source.Token,
                    source.Token,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);
            }
        }

        private async Task DoHealthCheck(CancellationToken cancellationToken)
        {
            _healthCheckRunning = true;

            foreach (var server in NameServers)
            {
                if (!server.Enabled && server.LastSuccessfulRequest != null)
                {
                    try
                    {
                        var result = await QueryInternalAsync(new[] { server }, server.LastSuccessfulRequest.Question, cancellationToken, useCache: false);
                    }
                    catch { }
                }
            }

            _healthCheckRunning = false;
        }

        private void DisableServer(NameServer server)
        {
            if (NameServers.Count > 1)
            {
                server.Enabled = false;
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
    }

    internal class LookupClientAudit
    {
        private const string c_placeHolder = "$$REPLACEME$$";
        private static readonly int s_printOffset = -32;
        private StringBuilder _auditWriter = new StringBuilder();
        private Stopwatch _swatch;

        public LookupClientAudit()
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

        public string Build(IDnsQueryResponse queryResponse)
        {
            var writer = new StringBuilder();

            if (queryResponse != null)
            {
                if (queryResponse.Questions.Count > 0)
                {
                    writer.AppendLine(";; QUESTION SECTION:");
                    foreach (var question in queryResponse.Questions)
                    {
                        writer.AppendLine(question.ToString(s_printOffset));
                    }
                    writer.AppendLine();
                }

                if (queryResponse.Answers.Count > 0)
                {
                    writer.AppendLine(";; ANSWER SECTION:");
                    foreach (var answer in queryResponse.Answers)
                    {
                        writer.AppendLine(answer.ToString(s_printOffset));
                    }
                    writer.AppendLine();
                }

                if (queryResponse.Authorities.Count > 0)
                {
                    writer.AppendLine(";; AUTHORITIES SECTION:");
                    foreach (var auth in queryResponse.Authorities)
                    {
                        writer.AppendLine(auth.ToString(s_printOffset));
                    }
                    writer.AppendLine();
                }

                if (queryResponse.Additionals.Count > 0)
                {
                    writer.AppendLine(";; ADDITIONALS SECTION:");
                    foreach (var additional in queryResponse.Additionals)
                    {
                        writer.AppendLine(additional.ToString(s_printOffset));
                    }
                    writer.AppendLine();
                }
            }

            var all = _auditWriter.ToString();
            var dynamic = writer.ToString();

            return all.Replace(c_placeHolder, dynamic);
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

        public void AuditResponse()
        {
            _auditWriter.AppendLine(c_placeHolder);
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
            _auditWriter.AppendLine();
            _auditWriter.AppendLine($"; SERVER: {current.Endpoint.Address}#{current.Endpoint.Port} failed; Retrying with the next server.");
        }
    }
}