using System;
using System.Collections.Generic;

namespace DnsClient
{
    /// <summary>
    /// Full contract for the DNS LookupClient including all the options.
    /// </summary>
    public interface ILookupClient : IDnsQuery
    {
        bool EnableAuditTrail { get; set; }

        TimeSpan? MimimumCacheTimeout { get; set; }

        IReadOnlyCollection<NameServer> NameServers { get; }

        bool Recursion { get; set; }

        int Retries { get; set; }

        bool ThrowDnsErrors { get; set; }

        TimeSpan Timeout { get; set; }

        bool UseCache { get; set; }

        bool UseTcpFallback { get; set; }

        bool UseTcpOnly { get; set; }
    }
}