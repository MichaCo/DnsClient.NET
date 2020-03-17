using System;
using System.Collections.Generic;

namespace DnsClient
{
    /// <summary>
    /// The contract for the LookupClient.
    /// <para>
    /// The interfaces for the query methods and the lookup client properties are separated so that one can
    /// inject or expose only the <see cref="IDnsQuery"/> without exposing the configuration options.
    /// </para>
    /// </summary>
    public interface ILookupClient : IDnsQuery
    {
        /// <summary>
        /// Gets the list of configured or resolved name servers of the <see cref="ILookupClient"/> instance.
        /// </summary>
        IReadOnlyCollection<NameServer> NameServers { get; }

        /// <summary>
        /// Gets the configured settings of the <see cref="ILookupClient"/> instance.
        /// </summary>
        LookupClientSettings Settings { get; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        // all settings will be moved into DnsQueryOptions/LookupClientOptions
        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        TimeSpan? MinimumCacheTimeout { get; set; }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        bool EnableAuditTrail { get; set; }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        bool UseCache { get; set; }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        bool Recursion { get; set; }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        int Retries { get; set; }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        bool ThrowDnsErrors { get; set; }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        bool UseRandomNameServer { get; set; }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        bool ContinueOnDnsError { get; set; }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        TimeSpan Timeout { get; set; }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        bool UseTcpFallback { get; set; }

        [Obsolete("This property will be removed from LookupClient in the next version. Use LookupClientOptions to initialize LookupClient instead.")]
        bool UseTcpOnly { get; set; }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}