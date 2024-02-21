using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using DnsClient.Internal;
using DnsClient.Windows;

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
        /// The public Google DNS IPv4 endpoint.
        /// </summary>
        public static readonly IPEndPoint GooglePublicDns = new IPEndPoint(IPAddress.Parse("8.8.4.4"), DefaultPort);

        /// <summary>
        /// The second public Google DNS IPv6 endpoint.
        /// </summary>
        public static readonly IPEndPoint GooglePublicDns2 = new IPEndPoint(IPAddress.Parse("8.8.8.8"), DefaultPort);

        /// <summary>
        /// The public Google DNS IPv6 endpoint.
        /// </summary>
        public static readonly IPEndPoint GooglePublicDnsIPv6 = new IPEndPoint(IPAddress.Parse("2001:4860:4860::8844"), DefaultPort);

        /// <summary>
        /// The second public Google DNS IPv6 endpoint.
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
        /// <exception cref="ArgumentNullException">If <paramref name="endPoint"/>is <c>null</c>.</exception>
        public NameServer(IPAddress endPoint)
            : this(new IPEndPoint(endPoint, DefaultPort))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class.
        /// </summary>
        /// <param name="endPoint">The name server endpoint.</param>
        /// <param name="port">The name server port.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="endPoint"/>is <c>null</c>.</exception>
        public NameServer(IPAddress endPoint, int port)
            : this(new IPEndPoint(endPoint, port))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class.
        /// </summary>
        /// <param name="endPoint">The name server endpoint.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="endPoint"/>is <c>null</c>.</exception>
        public NameServer(IPEndPoint endPoint)
        {
            IPEndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class.
        /// </summary>
        /// <param name="endPoint">The name server endpoint.</param>
        /// <param name="dnsSuffix">An optional DNS suffix (can be null).</param>
        /// <exception cref="ArgumentNullException">If <paramref name="endPoint"/>is <c>null</c>.</exception>
        public NameServer(IPAddress endPoint, string dnsSuffix)
            : this(new IPEndPoint(endPoint, DefaultPort), dnsSuffix)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class.
        /// </summary>
        /// <param name="endPoint">The name server endpoint.</param>
        /// <param name="port">The name server port.</param>
        /// <param name="dnsSuffix">An optional DNS suffix (can be null).</param>
        /// <exception cref="ArgumentNullException">If <paramref name="endPoint"/>is <c>null</c>.</exception>
        public NameServer(IPAddress endPoint, int port, string dnsSuffix)
            : this(new IPEndPoint(endPoint, port), dnsSuffix)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class.
        /// </summary>
        /// <param name="endPoint">The name server endpoint.</param>
        /// <param name="dnsSuffix">An optional DNS suffix (can be null).</param>
        /// <exception cref="ArgumentNullException">If <paramref name="endPoint"/>is <c>null</c>.</exception>
        public NameServer(IPEndPoint endPoint, string dnsSuffix)
            : this(endPoint)
        {
            DnsSuffix = string.IsNullOrWhiteSpace(dnsSuffix) ? null : dnsSuffix;
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

        /// <summary>
        /// Gets an optional DNS suffix which a resolver can use to append to queries or to find servers suitable for a query.
        /// </summary>
        public string DnsSuffix { get; }

        internal static NameServer[] Convert(IReadOnlyCollection<IPAddress> addresses)
            => addresses?.Select(p => (NameServer)p).ToArray();

        internal static NameServer[] Convert(IReadOnlyCollection<IPEndPoint> addresses)
            => addresses?.Select(p => (NameServer)p).ToArray();

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return IPEndPoint.ToString();
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
        /// If <paramref name="fallbackToGooglePublicDns" /> is enabled, this method will return the Google public DNS endpoints if no
        /// local DNS server was found.
        /// </para>
        /// </summary>
        /// <param name="skipIPv6SiteLocal">If set to <c>true</c> local IPv6 sites are skipped.</param>
        /// <param name="fallbackToGooglePublicDns">If set to <c>true</c> the public Google DNS servers are returned if no other servers could be found.</param>
        /// <returns>
        /// The list of name servers.
        /// </returns>
        public static IReadOnlyCollection<NameServer> ResolveNameServers(bool skipIPv6SiteLocal = true, bool fallbackToGooglePublicDns = true)
        {
            // TODO: Use Array.Empty after dropping NET45
            IReadOnlyCollection<NameServer> nameServers = new NameServer[0];

            var exceptions = new List<Exception>();

            var logger = Logging.LoggerFactory?.CreateLogger("DnsClient.NameServer");

            logger?.LogDebug("Starting to resolve NameServers, skipIPv6SiteLocal:{0}.", skipIPv6SiteLocal);
            try
            {
                nameServers = QueryNetworkInterfaces();
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Resolving name servers using .NET framework failed.");
                exceptions.Add(ex);
            }

#if !NET45
            if (exceptions.Count > 0)
            {
                logger?.LogDebug("Using native path to resolve servers.");

                try
                {
                    nameServers = ResolveNameServersNative();
                    exceptions.Clear();
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Resolving name servers using native implementation failed.");
                    exceptions.Add(ex);
                }
            }

            try
            {
                var nprt = ResolveNameResolutionPolicyServers();

                if (nprt.Count != 0)
                {
                    var servers = new HashSet<NameServer>();

                    foreach (var server in nprt)
                    {
                        servers.Add(server);
                    }

                    foreach (var server in nameServers)
                    {
                        servers.Add(server);
                    }

                    nameServers = servers;
                }
            }
            catch (Exception ex)
            {
                // Ignore the exception.
                // Turns out this can happen in Azure Functions. See #133
                // Turns out it can cause more errors, See #162, #149
                logger?.LogInformation(ex, "Resolving name servers from NRPT failed.");
            }

#endif
            IReadOnlyCollection<NameServer> filtered = nameServers
                .Where(p => (p.IPEndPoint.Address.AddressFamily == AddressFamily.InterNetwork
                            || p.IPEndPoint.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    && (!p.IPEndPoint.Address.IsIPv6SiteLocal || !skipIPv6SiteLocal))
                .ToArray();

            try
            {
                filtered = ValidateNameServers(filtered, logger);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "NameServer validation failed.");
                exceptions.Add(ex);
            }

            if (filtered.Count == 0)
            {
                if (!fallbackToGooglePublicDns && exceptions.Count > 0)
                {
                    throw new InvalidOperationException("Could not resolve any NameServers.", exceptions.First());
                }
                else if (fallbackToGooglePublicDns)
                {
                    logger?.LogWarning("Could not resolve any NameServers, falling back to Google public servers.");
                    return new NameServer[]
                    {
                        GooglePublicDns,
                        GooglePublicDns2,
                        GooglePublicDnsIPv6,
                        GooglePublicDns2IPv6
                    };
                }
            }

            logger?.LogDebug("Resolved {0} name servers: [{1}].", filtered.Count, string.Join(",", filtered.AsEnumerable()));
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
        public static IReadOnlyCollection<NameServer> ResolveNameServersNative()
        {
            List<NameServer> addresses = new List<NameServer>();

#if NET6_0_OR_GREATER
            if (OperatingSystem.IsWindows())
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
            {
                try
                {
                    var fixedInfo = Windows.IpHlpApi.FixedNetworkInformation.GetFixedInformation();

                    foreach (var ip in fixedInfo.DnsAddresses)
                    {
                        addresses.Add(new NameServer(ip, DefaultPort, fixedInfo.DomainName));
                    }
                }
                catch { }
            }
#if NET6_0_OR_GREATER
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
#else
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
#endif
            {
                try
                {
                    addresses = Linux.StringParsingHelpers.ParseDnsAddressesFromResolvConfFile(EtcResolvConfFile);
                }
                catch (Exception e) when (e is FileNotFoundException || e is UnauthorizedAccessException)
                {
                }
            }

            return addresses;
        }

        /// <summary>
        /// On a Windows machine query the Name Resolution Policy table for a list of policy-defined name servers.
        /// </summary>
        /// <returns>Returns a collection of name servers from the policy table</returns>
        public static IReadOnlyCollection<NameServer> ResolveNameResolutionPolicyServers()
        {
            return NameResolutionPolicy.Resolve();
        }

#endif

        internal static IReadOnlyCollection<NameServer> ValidateNameServers(IReadOnlyCollection<NameServer> servers, ILogger logger = null)
        {
            // Right now, I'm only checking for ANY address, but might be more validation rules at some point...
            var validServers = servers.Where(p => !p.IPEndPoint.Address.Equals(IPAddress.Any) && !p.IPEndPoint.Address.Equals(IPAddress.IPv6Any)).ToArray();

            if (validServers.Length != servers.Count)
            {
                logger?.LogWarning("Unsupported ANY address cannot be used as name server.");

                if (validServers.Length == 0)
                {
                    throw new InvalidOperationException("Unsupported ANY address cannot be used as name server and no other servers are configured to fall back to.");
                }
            }

            return validServers;
        }

        private static IReadOnlyCollection<NameServer> QueryNetworkInterfaces()
        {
            var result = new HashSet<NameServer>();

            var adapters = NetworkInterface.GetAllNetworkInterfaces();
            if (adapters == null)
            {
                return result.ToArray();
            }

            foreach (NetworkInterface networkInterface in
                adapters
                    .Where(p => p != null && (p.OperationalStatus == OperationalStatus.Up || p.OperationalStatus == OperationalStatus.Unknown)
                    && p.NetworkInterfaceType != NetworkInterfaceType.Loopback))
            {
                var properties = networkInterface?.GetIPProperties();

                // Can be null under mono for whatever reason...
                if (properties?.DnsAddresses == null)
                {
                    continue;
                }

                foreach (var ip in properties.DnsAddresses)
                {
                    result.Add(new NameServer(ip, DefaultPort, properties.DnsSuffix));
                }
            }

            return result.ToArray();
        }
    }
}
