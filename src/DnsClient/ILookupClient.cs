// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

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
    }
}
