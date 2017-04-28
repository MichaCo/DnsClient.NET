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
        /// If enabled, each <see cref="IDnsQueryResponse"/> will contain a full documentation of the response in zone file format.
        /// </summary>
        /// <seealso cref="IDnsQueryResponse.AuditTrail"/>
        bool EnableAuditTrail { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating if the <see cref="LookupClient"/> should use response caching or not.
        /// The cache duration is calculated by the resource record of the response. Usually, the lowest TTL is used.
        /// </summary>
        /// <remarks>
        /// In case the DNS Server returns records with a TTL of zero. The response cannot be cached.
        /// Setting <see cref="MinimumCacheTimeout"/> can overwrite this behavior and cache those responses anyways for at least the given duration.
        /// </remarks>
        bool UseCache { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="TimeSpan"/> which can override the TTL of a resource record in case the
        /// TTL of the record is lower than this minimum value.
        /// <para>
        /// This is useful in cases where the server retruns records with zero TTL.
        /// </para>
        /// This setting gets igonred in case <see cref="UseCache"/> is set to <c>false</c>.
        /// </summary>
        TimeSpan? MinimumCacheTimeout { get; set; }

        /// <summary>
        /// Gets the list of configured name servers.
        /// </summary>
        IReadOnlyCollection<NameServer> NameServers { get; }

        /// <summary>
        /// Gets or sets a flag indicating if DNS queries should instruct the DNS server to do recursive lookups, or not.
        /// </summary>
        /// <value>The flag indicating if recursion should be used or not.</value>
        bool Recursion { get; set; }

        /// <summary>
        /// Gets or sets number of tries to connect to one name server before trying the next one or throwing an exception.
        /// </summary>
        /// <summary>
        /// Gets or sets the number of retries the client can perform in connection or timeout errors during query operations.
        /// <para>
        /// If set to <c>0</c>, no retries will be performed.
        /// </para>
        /// </summary>
        /// <value>The number of alloed retries before exceptions will bubble up.</value>
        int Retries { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating if the <see cref="LookupClient"/> should throw an <see cref="DnsResponseException"/> if
        /// a query result has a <see cref="DnsResponseCode"/> other than <see cref="DnsResponseCode.NoError"/>.
        /// <para>
        /// If set to <c>false</c>, the <see cref="IDnsQueryResponse"/>'s <see cref="IDnsQueryResponse.HasError"/> flag can be expected.
        /// The <see cref="IDnsQueryResponse.ErrorMessage"/> will contain more information and the <see cref="IDnsQueryResponse.Header"/> transports the
        /// original <see cref="DnsResponseCode"/>.
        /// </para>
        /// <para>
        /// If set to <c>true</c>, any query method of <see cref="IDnsQuery"/> will throw an <see cref="DnsResponseException"/> if
        /// the response header indicates an error. The actual code and message can be accessed via <see cref="DnsResponseException.Code"/> and <see cref="DnsResponseException.DnsError"/> of the <see cref="DnsResponseException"/>.
        /// </para>
        /// </summary>
        /// <seealso cref="DnsResponseCode"/>
        bool ThrowDnsErrors { get; set; }

        /// <summary>
        /// Gets or sets the timeout in milliseconds. <see cref="Timeout"/> is used for limiting the connection and request time for one operation.
        /// Timeout must be greater than zero and less than <see cref="int.MaxValue"/>.
        /// If set to <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> (or -1), no timeout will be applied.
        /// </summary>
        /// <remarks>
        /// If set too short, queries will more likely result in <see cref="TimeoutException"/>s.
        /// <para>
        /// Important to note, <see cref="TimeoutException"/>s will be retried, if <see cref="Retries"/> are not disabled.
        /// This should help in case one or more configured DNS servers are not reachable or under load for example.
        /// </para>
        /// </remarks>
        TimeSpan Timeout { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating if Tcp should not be used in case a Udp response is truncated.
        /// If <c>true</c>, truncated results will potentially yield no answers.
        /// </summary>
        bool UseTcpFallback { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating if Udp should not be used at all.
        /// <para>
        /// Use this only if Udp cannot be used because of your firewall rules for example.
        /// </para>
        /// </summary>
        bool UseTcpOnly { get; set; }
    }
}