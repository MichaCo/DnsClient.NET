using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace DnsClient
{
    public class NameServer
    {
        /// <summary>
        /// The default DNS server port.
        /// </summary>
        public const int DefaultPort = 53;

        /// <summary>
        /// Gets a list of name servers by iterating over the available network interfaces.
        /// </summary>
        /// <returns>The list of name servers.</returns>
        public static ICollection<IPEndPoint> ResolveNameServers()
        {
            var result = new HashSet<IPEndPoint>();

            var adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface networkInterface in adapters.Where(p=>p.OperationalStatus == OperationalStatus.Up))
            {
                foreach (IPAddress dnsAddress in networkInterface
                    .GetIPProperties()
                    .DnsAddresses
                    .Where(i => i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork || i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6))
                {
                    result.Add(new IPEndPoint(dnsAddress, DefaultPort));
                }
            }

            return result.ToArray();
        }
    }
}