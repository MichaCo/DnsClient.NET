using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace DnsClient
{
    public class NameServer
    {
        /// <summary>
        /// The default DNS server port.
        /// </summary>
        public const int DefaultPort = 53;

        public static readonly IPEndPoint GooglePublicDns = new IPEndPoint(IPAddress.Parse("8.8.4.4"), DefaultPort);
        public static readonly IPEndPoint GooglePublicDns2 = new IPEndPoint(IPAddress.Parse("8.8.8.8"), DefaultPort);
        public static readonly IPEndPoint GooglePublicDnsIPv6 = new IPEndPoint(IPAddress.Parse("2001:4860:4860::8844"), DefaultPort);
        public static readonly IPEndPoint GooglePublicDns2IPv6 = new IPEndPoint(IPAddress.Parse("2001:4860:4860::8888"), DefaultPort);

        internal const string EtcResolvConfFile = "/etc/resolv.conf";

        public NameServer(IPAddress endpoint)
            : this(new IPEndPoint(endpoint, DefaultPort))
        {
        }

        public NameServer(IPEndPoint endpoint)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            Endpoint = endpoint;
        }

        public bool Enabled { get; internal set; } = true;

        public IPEndPoint Endpoint { get; }

        public int? SupportedUdpPayloadSize { get; internal set; }

        public override string ToString()
        {
            return $"{Endpoint} (Udp: {SupportedUdpPayloadSize ?? 512})";
        }

        /// <summary>
        /// Gets a list of name servers by iterating over the available network interfaces.
        /// </summary>
        /// <returns>The list of name servers.</returns>
        public static ICollection<IPEndPoint> ResolveNameServers(bool skipIPv6SiteLocal = true)
        {
            var endpoints = ResolveNameServersInternal(skipIPv6SiteLocal);
            if (endpoints.Count == 0)
            {
                return new[]
                {
                    GooglePublicDnsIPv6,
                    GooglePublicDns2IPv6,
                    GooglePublicDns,
                    GooglePublicDns2,
                };
            }

            return endpoints;
        }

        private static ICollection<IPEndPoint> ResolveNameServersInternal(bool skipIPv6SiteLocal)
        {
            try
            {
                return QueryNetworkInterfaces(skipIPv6SiteLocal);
            }
            catch (Exception ex) when (ex is PlatformNotSupportedException || ex is NotImplementedException)
            {
                // well... hope this never happens on NET45 ^^
#if !PORTABLE
                throw;
#endif
            }
#if PORTABLE
            catch (NetworkInformationException ex)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // continue, try reading the resolv.conf...
                }
                else
                {
                    throw new InvalidOperationException($"Error resolving name servers.\n{ex.Message} Code: {ex.ErrorCode} HResult: {ex.HResult}.", ex);
                }
            }

            //
            try
            {
                return GetDnsEndpointsNative();
            }
            catch (Exception)
            {
                // log etc?
                throw;
            }
#endif
        }

#if PORTABLE

        private static IPEndPoint[] GetDnsEndpointsNative()
        {
            IPAddress[] addresses = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var fixedInfo = Windows.IpHlpApi.FixedNetworkInformation.GetFixedInformation();

                addresses = fixedInfo.DnsAddresses.ToArray();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // TODO: Remove if fixed in dotnet core runtim 1.2.x?
                addresses = Linux.StringParsingHelpers.ParseDnsAddressesFromResolvConfFile(EtcResolvConfFile).ToArray();
            }

            return addresses?.Select(p => new IPEndPoint(p, DefaultPort)).ToArray();
        }

#endif

        private static IPEndPoint[] QueryNetworkInterfaces(bool skipIPv6SiteLocal)
        {
            var result = new HashSet<IPEndPoint>();

            var adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface networkInterface in
                adapters
                    .Where(p => p.OperationalStatus == OperationalStatus.Up
                    && p.NetworkInterfaceType != NetworkInterfaceType.Loopback))
            {
                foreach (IPAddress dnsAddress in networkInterface
                    .GetIPProperties()
                    .DnsAddresses
                    .Where(i =>
                        i.AddressFamily == AddressFamily.InterNetwork
                        || i.AddressFamily == AddressFamily.InterNetworkV6))
                {
                    if (dnsAddress.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        if (skipIPv6SiteLocal && dnsAddress.IsIPv6SiteLocal)
                        {
                            continue;
                        }
                    }

                    result.Add(new IPEndPoint(dnsAddress, DefaultPort));
                }
            }

            return result.ToArray();
        }

        internal NameServer Clone()
        {
            return new NameServer(Endpoint)
            {
                Enabled = Enabled,
                SupportedUdpPayloadSize = SupportedUdpPayloadSize
            };
        }
    }
}