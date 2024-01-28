using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace DnsClient.Windows
{
#if !NET45

    internal static class NameResolutionPolicy
    {
        private static readonly char[] s_splitOn = new char[] { ';' };

        /// <summary>
        /// Resolve all names from the Name Resolution policy in Windows.
        /// </summary>
        /// <returns>Returns a list of name servers</returns>
        internal static IReadOnlyCollection<NameServer> Resolve(bool includeGenericServers = true, bool includeDirectAccessServers = true)
        {
            var nameServers = new HashSet<NameServer>();

#if NET6_0_OR_GREATER
            if (!OperatingSystem.IsWindows())
#else
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
            {
                return nameServers;
            }

            // [MS-GPNRPT] dictates that the NRPT is stored in two separate registry keys.
            //
            //  - The Policy key is pushed down through Group Policy.
            //  - The Parameters key is configured out of band.
            //
            // Each key will contain one or more NRP rules where the key name is a unique GUID.
            // If the key exists in both Policy and Parameters, then Policy will take precedence.

            var policyRoot = Registry.LocalMachine.OpenSubKey(@"Software\Policies\Microsoft\Windows NT\DNSClient\DnsPolicyConfig\");
            var parametersRoot = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters\DnsPolicyConfig\");

            try
            {
                var policyNames = new HashSet<string>();

                if (policyRoot != null)
                {
                    foreach (var policy in policyRoot.GetSubKeyNames())
                    {
                        policyNames.Add(policy);
                    }
                }

                if (parametersRoot != null)
                {
                    foreach (var policy in parametersRoot.GetSubKeyNames())
                    {
                        policyNames.Add(policy);
                    }
                }

                foreach (var policyGuid in policyNames)
                {
                    var policyKey = policyRoot?.OpenSubKey(policyGuid) ?? parametersRoot?.OpenSubKey(policyGuid);

                    if (policyKey == null)
                    {
                        // shouldn't ever happen, but is a race condition
                        continue;
                    }

                    using (policyKey)
                    {
                        var name = policyKey.GetValue("Name") as string[];
                        var dnsServers = policyKey.GetValue("GenericDNSServers")?.ToString();
                        var directAccessDnsServers = policyKey.GetValue("DirectAccessDNSServers")?.ToString();

                        if (includeGenericServers)
                        {
                            AddServers(nameServers, name, dnsServers);
                        }

                        if (includeDirectAccessServers)
                        {
                            AddServers(nameServers, name, directAccessDnsServers);
                        }
                    }
                }
            }
            finally
            {
                policyRoot?.Dispose();
                parametersRoot?.Dispose();
            }

            return nameServers.ToArray();
        }

        private static void AddServers(HashSet<NameServer> nameServers, string[] names, string dnsServers)
        {
            if (string.IsNullOrWhiteSpace(dnsServers))
            {
                return;
            }

            var servers = dnsServers.Split(s_splitOn, StringSplitOptions.RemoveEmptyEntries);

            foreach (var s in servers)
            {
                // DNS servers are semicolon separated and can be IPv4, IPv6, or FQDN addresses
                // We're going to ignore FQDN addresses because resolving DNS servers by name is messy

                if (IPAddress.TryParse(s, out IPAddress address) &&
                    (address.AddressFamily == AddressFamily.InterNetwork ||
                     address.AddressFamily == AddressFamily.InterNetworkV6) &&
                     names != null)
                {
                    // Name can be a suffix (starts with .) or a prefix
                    // we want to ignore it if it's not a suffix

                    foreach (var name in names.Where(n => n.StartsWith(".")).Distinct())
                    {
                        nameServers.Add(new NameServer(address, name));
                    }
                }
            }
        }
    }
#endif
}
