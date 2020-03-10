﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using DnsClient.Internal;

namespace DnsClient
{
    /// <summary>
    /// Represents a name server instance used by <see cref="ILookupClient"/>.
    /// Also, comes with some static methods to resolve name servers from the local network configuration.
    /// </summary>
    public class NameServer : IEquatable<NameServer>
    {
        /// <summary>
        /// The default DNS server port.
        /// </summary>
        public const int DefaultPort = 53;

        /// <summary>
        /// The public google DNS IPv4 endpoint.
        /// </summary>
        public static readonly IPEndPoint GooglePublicDns = new IPEndPoint(IPAddress.Parse("8.8.4.4"), DefaultPort);

        /// <summary>
        /// The second public google DNS IPv6 endpoint.
        /// </summary>
        public static readonly IPEndPoint GooglePublicDns2 = new IPEndPoint(IPAddress.Parse("8.8.8.8"), DefaultPort);

        /// <summary>
        /// The public google DNS IPv6 endpoint.
        /// </summary>
        public static readonly IPEndPoint GooglePublicDnsIPv6 = new IPEndPoint(IPAddress.Parse("2001:4860:4860::8844"), DefaultPort);

        /// <summary>
        /// The second public google DNS IPv6 endpoint.
        /// </summary>
        public static readonly IPEndPoint GooglePublicDns2IPv6 = new IPEndPoint(IPAddress.Parse("2001:4860:4860::8888"), DefaultPort);

        /// <summary>
        /// A public Cloudflare DNS endpoint.
        /// </summary>
        public static readonly IPEndPoint Cloudflare = new IPEndPoint(IPAddress.Parse("1.1.1.1"), DefaultPort);

        /// <summary>
        /// A public Cloudflare DNS endpoint.
        /// </summary>
        public static readonly IPEndPoint Cloudflare2 = new IPEndPoint(IPAddress.Parse("1.0.0.1"), DefaultPort);

        /// <summary>
        /// A public Cloudflare DNS IPv6 endpoint.
        /// </summary>
        public static readonly IPEndPoint CloudflareIPv6 = new IPEndPoint(IPAddress.Parse("2606:4700:4700::1111"), DefaultPort);

        /// <summary>
        /// A public Cloudflare DNS IPv6 endpoint.
        /// </summary>
        public static readonly IPEndPoint Cloudflare2IPv6 = new IPEndPoint(IPAddress.Parse("2606:4700:4700::1001"), DefaultPort);

        internal const string EtcResolvConfFile = "/etc/resolv.conf";

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class.
        /// </summary>
        /// <param name="endPoint">The name server endpoint.</param>
        public NameServer(IPAddress endPoint)
            : this(new IPEndPoint(endPoint, DefaultPort))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class.
        /// </summary>
        /// <param name="endPoint">The name server endpoint.</param>
        /// <param name="port">The name server port.</param>
        public NameServer(IPAddress endPoint, int port)
            : this(new IPEndPoint(endPoint, port))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class.
        /// </summary>
        /// <param name="endPoint">The name server endpoint.</param>
        /// <exception cref="System.ArgumentNullException">If <paramref name="endPoint"/> is null.</exception>
        public NameServer(IPEndPoint endPoint)
        {
            IPEndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class from a <see cref="IPEndPoint"/>.
        /// </summary>
        /// <param name="endPoint">The endpoint.</param>
        public static implicit operator NameServer(IPEndPoint endPoint)
        {
            if (endPoint == null)
            {
                return null;
            }

            return new NameServer(endPoint);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class from a <see cref="IPAddress"/>.
        /// </summary>
        /// <param name="address">The address.</param>
        public static implicit operator NameServer(IPAddress address)
        {
            if (address == null)
            {
                return null;
            }

            return new NameServer(address);
        }

        /// <summary>
        /// Gets the string representation of the configured <see cref="IPAddress"/>.
        /// </summary>
        public string Address => IPEndPoint.Address.ToString();

        /// <summary>
        /// Gets the port.
        /// </summary>
        public int Port => IPEndPoint.Port;

        /// <summary>
        /// Gets the address family.
        /// </summary>
        public AddressFamily AddressFamily => IPEndPoint.AddressFamily;

        /// <summary>
        /// Gets the size of the supported UDP payload.
        /// <para>
        /// This value might get updated by <see cref="ILookupClient"/> by reading the options records returned by a query.
        /// </para>
        /// </summary>
        /// <value>
        /// The size of the supported UDP payload.
        /// </value>
        public int? SupportedUdpPayloadSize { get; internal set; }

        internal IPEndPoint IPEndPoint { get; }

        internal static IReadOnlyCollection<NameServer> Convert(IReadOnlyCollection<IPAddress> addresses)
            => addresses.Select(p => (NameServer)p).ToArray();

        internal static IReadOnlyCollection<NameServer> Convert(IReadOnlyCollection<IPEndPoint> addresses)
            => addresses.Select(p => (NameServer)p).ToArray();

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{Address}:{Port} (Udp: {SupportedUdpPayloadSize ?? DnsQueryOptions.MinimumBufferSize})";
        }

        /// <inheritdocs />
        public override bool Equals(object obj)
        {
            return Equals(obj as NameServer);
        }

        /// <inheritdocs />
        public bool Equals(NameServer other)
        {
            return other != null
                && EqualityComparer<IPEndPoint>.Default.Equals(IPEndPoint, other.IPEndPoint);
        }

        /// <inheritdocs />
        public override int GetHashCode()
        {
            return EqualityComparer<IPEndPoint>.Default.GetHashCode(IPEndPoint);
        }

        /// <summary>
        /// Gets a list of name servers by iterating over the available network interfaces.
        /// <para>
        /// If <paramref name="fallbackToGooglePublicDns" /> is enabled, this method will return the google public dns endpoints if no
        /// local DNS server was found.
        /// </para>
        /// </summary>
        /// <param name="skipIPv6SiteLocal">If set to <c>true</c> local IPv6 sites are skiped.</param>
        /// <param name="fallbackToGooglePublicDns">If set to <c>true</c> the public Google DNS servers are returned if no other servers could be found.</param>
        /// <returns>
        /// The list of name servers.
        /// </returns>
        public static IReadOnlyCollection<NameServer> ResolveNameServers(bool skipIPv6SiteLocal = true, bool fallbackToGooglePublicDns = true)
        {
            IReadOnlyCollection<IPAddress> endPoints = new IPAddress[0];

            List<Exception> exceptions = new List<Exception>();

            var logger = Logging.LoggerFactory?.CreateLogger(nameof(NameServer));

            logger?.LogDebug("Starting to resolve NameServers, skipIPv6SiteLocal:{0}.", skipIPv6SiteLocal);
            try
            {
                endPoints = QueryNetworkInterfaces();
            }
            catch (Exception ex)
            {
                logger?.LogInformation(ex, "Resolving name servers using .NET framework failed.");
                exceptions.Add(ex);
            }

#if !NET45
            if (exceptions.Count > 0)
            {
                try
                {
                    endPoints = ResolveNameServersNative();
                    exceptions.Clear();
                }
                catch (Exception ex)
                {
                    logger?.LogInformation(ex, "Resolving name servers using native implementation failed.");
                    exceptions.Add(ex);
                }
            }
#endif

            if (!fallbackToGooglePublicDns && exceptions.Count > 0)
            {
                if (exceptions.Count > 1)
                {
                    throw new AggregateException("Error resolving name servers", exceptions);
                }
                else
                {
                    throw exceptions.First();
                }
            }

            var filtered = endPoints
                .Where(p => (p.AddressFamily == AddressFamily.InterNetwork || p.AddressFamily == AddressFamily.InterNetworkV6)
                    && (!p.IsIPv6SiteLocal || !skipIPv6SiteLocal))
                .Select(p => new NameServer(p))
                .ToArray();

            if (filtered.Length == 0 && fallbackToGooglePublicDns)
            {
                logger?.LogWarning("Could not resolve any NameServers, falling back to Google public servers.");
                return new NameServer[]
                {
                    GooglePublicDnsIPv6,
                    GooglePublicDns2IPv6,
                    GooglePublicDns,
                    GooglePublicDns2,
                };
            }

            logger?.LogDebug("Resolved {0} name servers: [{1}].", filtered.Length, string.Join(",", filtered.AsEnumerable()));
            return filtered;
        }

#if !NET45

        /// <summary>
        /// Using my custom native implementation to support UWP apps and such until <see cref="NetworkInterface.GetAllNetworkInterfaces"/>
        /// gets an implementation in netstandard2.1.
        /// </summary>
        /// <remarks>
        /// DnsClient has been changed in version 1.1.0.
        /// It will not invoke this when resolving default DNS servers. It is up to the user to decide what to do based on what platform the code is running on.
        /// </remarks>
        /// <returns>
        /// The list of name servers.
        /// </returns>
        public static IReadOnlyCollection<IPAddress> ResolveNameServersNative()
        {
            IPAddress[] addresses = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var fixedInfo = Windows.IpHlpApi.FixedNetworkInformation.GetFixedInformation();

                addresses = fixedInfo.DnsAddresses.ToArray();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                addresses = Linux.StringParsingHelpers.ParseDnsAddressesFromResolvConfFile(EtcResolvConfFile).ToArray();
            }

            return addresses;
        }

#endif

        private static IReadOnlyCollection<IPAddress> QueryNetworkInterfaces()
        {
            var result = new HashSet<IPAddress>();

            var adapters = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface networkInterface in
                adapters
                    .Where(p => (p.OperationalStatus == OperationalStatus.Up || p.OperationalStatus == OperationalStatus.Unknown)
                    && p.NetworkInterfaceType != NetworkInterfaceType.Loopback))
            {
                foreach (IPAddress dnsAddress in networkInterface
                    .GetIPProperties()
                    .DnsAddresses)
                {
                    result.Add(dnsAddress);
                }
            }

            return result.ToArray();
        }
    }
}