using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace DnsClient
{
    public class NameServer
    {
        /// <summary>
        /// The default DNS server port.
        /// </summary>
        public const int DefaultPort = 53;

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

        /// <summary>
        /// Gets a list of name servers by iterating over the available network interfaces.
        /// </summary>
        /// <returns>The list of name servers.</returns>
        public static ICollection<IPEndPoint> ResolveNameServers()
        {
            try
            {
                return QueryNetworkInterfaces();
            }
            catch (Exception ex) when (ex is PlatformNotSupportedException || ex is NotImplementedException)
            {
                // well... hope this never happens on NET45 ^^
#if !PORTABLE
                throw;
#endif
            }

#if PORTABLE
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
            IPEndPoint[] endpoints = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var fixedInfo = Windows.IpHlpApi.FixedNetworkInformation.GetFixedInformation();

                endpoints = fixedInfo.DnsAddresses.Select(p => new IPEndPoint(p, DefaultPort)).ToArray();
            }

            return endpoints;
        }

#endif

        private static IPEndPoint[] QueryNetworkInterfaces()
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
                        i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                        || i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6))
                {
                    result.Add(new IPEndPoint(dnsAddress, DefaultPort));
                }
            }

            return result.ToArray();
        }

        public override string ToString()
        {
            return $"{Endpoint} (Udp: {SupportedUdpPayloadSize ?? 512})";
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