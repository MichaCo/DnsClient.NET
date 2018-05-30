using System;
using System.Collections.Generic;

namespace DnsClient
{
    /// <summary>
    /// The contract for the LookupClient including all the options.
    /// <para>
    /// The interfaces for the query methods and the lookup client properties are separated so that one can
    /// inject or expose only the <see cref="IDnsQuery"/> without exposing the configuration options.
    /// </para>
    /// </summary>
    public interface ILookupClient : IDnsQuery
    {
        /// <summary>
        /// If enabled, each <see cref="IDnsQueryResponse"/> will contain a full documentation of the response(s).
        /// Default is <c>False</c>.
        /// </summary>
        /// <seealso cref="IDnsQueryResponse.AuditTrail"/>
        bool EnableAuditTrail { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating if the <see cref="LookupClient"/> should use response caching or not.
        /// The cache duration is calculated by the resource record of the response. Usually, the lowest TTL is used.
        /// Default is <c>True</c>.
        /// </summary>
        /// <remarks>
        /// In case the DNS Server returns records with a TTL of zero. The response cannot be cached.
        /// Setting <see cref="MinimumCacheTimeout"/> can overwrite this behavior and cache those responses anyways for at least the given duration.
        /// </remarks>
        bool UseCache { get; set; }

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
        /// </remarks>
        TimeSpan? MinimumCacheTimeout { get; set; }

        /// <summary>
        /// Gets the list of configured name servers.
        /// </summary>
        IReadOnlyCollection<NameServer> NameServers { get; }

        /// <summary>
        /// Gets or sets a flag indicating whether DNS queries should instruct the DNS server to do recursive lookups, or not.
        /// Default is <c>True</c>.
        /// </summary>
        /// <value>The flag indicating if recursion should be used or not.</value>
        bool Recursion { get; set; }

        /// <summary>
        /// Gets or sets the number of tries to get a response from one name server before trying the next one.
        /// Only transient errors, like network or connection errors will be retried.
        /// Default is <c>5</c>.
        /// <para>
        /// If all configured <see cref="NameServers"/> error out after retries, an exception will be thrown at the end.
        /// </para>
        /// </summary>
        /// <value>The number of retries.</value>
        int Retries { get; set; }

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
        bool ThrowDnsErrors { get; set; }

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
        bool UseRandomNameServer { get; set; }

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
        bool ContinueOnDnsError { get; set; }

        /// <summary>
        /// Gets or sets the request timeout in milliseconds. <see cref="Timeout"/> is used for limiting the connection and request time for one operation.
        /// Timeout must be greater than zero and less than <see cref="int.MaxValue"/>.
        /// If <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> (or -1) is used, no timeout will be applied.
        /// </summary>
        /// <remarks>
        /// If a very short timeout is configured, queries will more likely result in <see cref="TimeoutException"/>s.
        /// <para>
        /// Important to note, <see cref="TimeoutException"/>s will be retried, if <see cref="Retries"/> are not disabled (set to <c>0</c>).
        /// This should help in case one or more configured DNS servers are not reachable or under load for example.
        /// </para>
        /// </remarks>
        TimeSpan Timeout { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether Tcp should be used in case a Udp response is truncated.
        /// Default is <c>True</c>.
        /// <para>
        /// If <c>False</c>, truncated results will potentially yield no answers.
        /// </para>
        /// </summary>
        bool UseTcpFallback { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether Udp should not be used at all.
        /// Default is <c>False</c>.
        /// <para>
        /// Enable this only if Udp cannot be used because of your firewall rules for example.
        /// </para>
        /// </summary>
        bool UseTcpOnly { get; set; }
    }
}