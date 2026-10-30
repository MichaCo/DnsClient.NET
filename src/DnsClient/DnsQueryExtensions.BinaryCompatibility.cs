// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DnsClient
{
    // Methods kept for binary compatibility
    public static partial class DnsQueryExtensions
    {
        /// <inheritdoc cref="GetHostEntryAsync(IDnsQuery, string, CancellationToken)" />
        public static async Task<IPHostEntry> GetHostEntryAsync(IDnsQuery query, string hostNameOrAddress)
            => await query.GetHostEntryAsync(hostNameOrAddress, CancellationToken.None).ConfigureAwait(false);

        /// <inheritdoc cref="GetHostEntryAsync(IDnsQuery, IPAddress, CancellationToken)" />
        public static async Task<IPHostEntry> GetHostEntryAsync(IDnsQuery query, IPAddress address)
            => await query.GetHostEntryAsync(address, CancellationToken.None).ConfigureAwait(false);

        /// <inheritdoc cref="GetHostNameAsync(IDnsQuery, IPAddress, CancellationToken)" />
        public static async Task<string> GetHostNameAsync(IDnsQuery query, IPAddress address)
            => await query.GetHostNameAsync(address, CancellationToken.None).ConfigureAwait(false);

        /// <inheritdoc cref="ResolveServiceAsync(IDnsQuery, string, string, ProtocolType, CancellationToken)" />
        public static async Task<ServiceHostEntry[]> ResolveServiceAsync(IDnsQuery query, string baseDomain, string serviceName, ProtocolType protocol)
            => await query.ResolveServiceAsync(baseDomain, serviceName, protocol, CancellationToken.None).ConfigureAwait(false);

        /// <inheritdoc cref="ResolveServiceAsync(IDnsQuery, string, string, string, CancellationToken)" />
        public static async Task<ServiceHostEntry[]> ResolveServiceAsync(IDnsQuery query, string baseDomain, string serviceName, string tag = null)
            => await query.ResolveServiceAsync(baseDomain, serviceName, tag, CancellationToken.None).ConfigureAwait(false);

        /// <inheritdoc cref="ResolveServiceAsync(IDnsQuery, string, CancellationToken)" />
        public static async Task<ServiceHostEntry[]> ResolveServiceAsync(IDnsQuery query, string service)
            => await query.ResolveServiceAsync(service, CancellationToken.None).ConfigureAwait(false);
    }
}
