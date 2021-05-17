using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;

// TODO: Remove if fixed
// This code is from https://github.com/dotnet/corefx
// Will be removed whenever the bugs reading network information on Linux are fixed and
// I can use the Managed version.

namespace DnsClient.Linux
{
    internal static partial class StringParsingHelpers
    {
        internal static string ParseDnsSuffixFromResolvConfFile(string filePath)
        {
            string data = File.ReadAllText(filePath);
            RowConfigReader rcr = new RowConfigReader(data);

            return rcr.TryGetNextValue("search", out var dnsSuffix) ? dnsSuffix : string.Empty;
        }

        internal static List<NameServer> ParseDnsAddressesFromResolvConfFile(string filePath)
        {
            // Parse /etc/resolv.conf for all of the "nameserver" entries.
            // These are the DNS servers the machine is configured to use.
            // On OSX, this file is not directly used by most processes for DNS
            // queries/routing, but it is automatically generated instead, with
            // the machine's DNS servers listed in it.
            string data = File.ReadAllText(filePath);
            RowConfigReader rcr = new RowConfigReader(data);
            List<NameServer> addresses = new List<NameServer>();

            while (rcr.TryGetNextValue("nameserver", out var addressString))
            {
                if (IPAddress.TryParse(addressString, out IPAddress parsedAddress))
                {
                    addresses.Add(parsedAddress);
                }
            }

            return addresses;
        }
    }
}
