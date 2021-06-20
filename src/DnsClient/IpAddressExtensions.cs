using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using DnsClient;

namespace System.Net
{
    /// <summary>
    /// Extension methods for <see cref="IPAddress"/>.
    /// </summary>
    public static class IpAddressExtensions
    {
        /// <summary>
        /// Translates a IPv4 or IPv6 <see cref="IPAddress"/> into an <see href="https://en.wikipedia.org/wiki/.arpa">arpa address</see>.
        /// Used for reverse DNS lookup to get the domain name of the given <see cref="IPAddress"/>.
        /// </summary>
        /// <param name="ip">The address to translate.</param>
        /// <returns>The arpa representation of the address.</returns>
        /// <seealso cref="IDnsQuery.QueryReverse(IPAddress)"/>
        /// <seealso cref="IDnsQuery.QueryReverseAsync(IPAddress, Threading.CancellationToken)"/>
        /// <seealso href="https://en.wikipedia.org/wiki/.arpa"/>
        public static string GetArpaName(this IPAddress ip)
        {
            var bytes = ip.GetAddressBytes();
            Array.Reverse(bytes);

            // check IP6
            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // reversed bytes need to be split into 4 bit parts and separated by '.'
                var newBytes = bytes
                    .SelectMany(b => new[] { (b >> 0) & 0xf, (b >> 4) & 0xf })
                    .Aggregate(new StringBuilder(), (s, b) => s.Append(b.ToString("x")).Append(DnsString.Dot)) + "ip6.arpa.";

                return newBytes;
            }
            else if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                // else IP4
                return string.Join(".", bytes) + ".in-addr.arpa.";
            }

            // never happens anyways!?
            throw new ArgumentException($"Unsupported address family '{ip.AddressFamily}'.", nameof(ip));
        }
    }
}
