using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using DnsClient.Protocol;

namespace DnsClient
{
    public static class DnsQueryExtensions
    {
        /// <summary>
        /// The <c>GetHostEntry</c> method queries a DNS server for the IP addresses and aliases associated with the <paramref name="hostNameOrAddress"/>.
        /// In case <paramref name="hostNameOrAddress"/> is an <see cref="IPAddress"/>, <c>GetHostEntry</c> does a reverse lookup on that first to determine the hostname.
        /// <para>
        /// IP addresses found are returned in <see cref="IPHostEntry.AddressList"/>.
        /// <c>CNAME</c> records are used to populate the <see cref="IPHostEntry.Aliases"/>.<br/>
        /// The <see cref="IPHostEntry.HostName"/> property will be set to the resolved hostname of the <paramref name="address"/>.
        /// </para>
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// public static void PrintHostEntry(string hostOrIp)
        /// {
        ///     var lookup = new LookupClient();
        ///
        ///     IPHostEntry hostEntry = lookup.GetHostEntry(hostOrIp);
        ///
        ///     Console.WriteLine(hostEntry.HostName);
        ///
        ///     foreach (var ip in hostEntry.AddressList)
        ///     {
        ///         Console.WriteLine(ip);
        ///     }
        ///     foreach (var alias in hostEntry.Aliases)
        ///     {
        ///         Console.WriteLine(alias);
        ///     }
        /// }
        /// ]]>
        /// </code>
        /// </example>
        /// <remarks>
        /// <list type="bullet"><item>
        /// In case of sub-domain queries or similar, there might be multiple <c>CNAME</c> records for one <see cref="IPAddress"/>,
        /// if only one <see cref="IPAddress"/> is in the result set, the returned <see cref="IPHostEntry"/> will contain all the aliases.
        /// </item><item>
        /// If all <see cref="IPAddress"/> found by this query do not have any unique aliases / <c>CNAME</c> records, the <see cref="IPHostEntry.Aliases"/> list will be empty.
        /// </item></list>
        /// </remarks>
        /// <param name="query">The <see cref="IDnsQuery"/> instance.</param>
        /// <param name="address">The <see cref="IPAddress"/> to query for.</param>
        /// <returns>
        /// An <see cref="IPHostEntry"/> instance that contains address information about the host specified in <paramref name="address"/>.
        /// In case the <paramref name="address"/> could not be resolved to a domain name, this method returns <c>null</c>,
        /// unless <see cref="ILookupClient.ThrowDnsErrors"/> is set to true, then it might throw a <see cref="DnsResponseException"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="address"/>address is null.</exception>
        /// <exception cref="DnsResponseException">In case <see cref="ILookupClient.ThrowDnsErrors"/> is set to true and a DNS error occurs.</exception>
        public static IPHostEntry GetHostEntry(this IDnsQuery query, string hostNameOrAddress)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }
            if (string.IsNullOrWhiteSpace(hostNameOrAddress))
            {
                throw new ArgumentNullException(nameof(hostNameOrAddress));
            }

            if (IPAddress.TryParse(hostNameOrAddress, out IPAddress address))
            {
                return query.GetHostEntry(address);
            }

            return GetHostEntryFromName(query, hostNameOrAddress);
        }

        /// <summary>
        /// The <c>GetHostEntry</c> method does a reverse lookup on the IP <paramref name="address"/>,
        /// and queries a DNS server for the IP addresses and aliases associated with the resolved hostname.
        /// <para>
        /// IP addresses found are returned in <see cref="IPHostEntry.AddressList"/>.
        /// <c>CNAME</c> records are used to populate the <see cref="IPHostEntry.Aliases"/>.<br/>
        /// The <see cref="IPHostEntry.HostName"/> property will be set to the resolved hostname of the <paramref name="address"/>.
        /// </para>
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// public static void PrintHostEntry(IPAddress address)
        /// {
        ///     var lookup = new LookupClient();
        ///
        ///     IPHostEntry hostEntry = lookup.GetHostEntry(address);
        ///
        ///     Console.WriteLine(hostEntry.HostName);
        ///
        ///     foreach (var ip in hostEntry.AddressList)
        ///     {
        ///         Console.WriteLine(ip);
        ///     }
        ///     foreach (var alias in hostEntry.Aliases)
        ///     {
        ///         Console.WriteLine(alias);
        ///     }
        /// }
        /// ]]>
        /// </code>
        /// </example>
        /// <remarks>
        /// <list type="bullet"><item>
        /// In case of sub-domain queries or similar, there might be multiple <c>CNAME</c> records for one <see cref="IPAddress"/>,
        /// if only one <see cref="IPAddress"/> is in the result set, the returned <see cref="IPHostEntry"/> will contain all the aliases.
        /// </item><item>
        /// If all <see cref="IPAddress"/> found by this query do not have any unique aliases / <c>CNAME</c> records, the <see cref="IPHostEntry.Aliases"/> list will be empty.
        /// </item></list>
        /// </remarks>
        /// <param name="query">The <see cref="IDnsQuery"/> instance.</param>
        /// <param name="address">The <see cref="IPAddress"/> to query for.</param>
        /// <returns>
        /// An <see cref="IPHostEntry"/> instance that contains address information about the host specified in <paramref name="address"/>.
        /// In case the <paramref name="address"/> could not be resolved to a domain name, this method returns <c>null</c>,
        /// unless <see cref="ILookupClient.ThrowDnsErrors"/> is set to true, then it might throw a <see cref="DnsResponseException"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="address"/>address is null.</exception>
        /// <exception cref="DnsResponseException">In case <see cref="ILookupClient.ThrowDnsErrors"/> is set to true and a DNS error occurs.</exception>
        public static IPHostEntry GetHostEntry(this IDnsQuery query, IPAddress address)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            var hostName = query.GetHostName(address);
            if (string.IsNullOrWhiteSpace(hostName))
            {
                return null;
            }

            return GetHostEntryFromName(query, hostName);
        }

        private static IPHostEntry GetHostEntryFromName(IDnsQuery query, string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName))
            {
                throw new ArgumentNullException(nameof(hostName));
            }

            var hostString = DnsString.FromResponseQueryString(hostName);
            var ipv4Result = query.Query(hostString, QueryType.A);
            var ipv6Result = query.Query(hostString, QueryType.AAAA);

            var allRecords = ipv4Result
                .Answers.Concat(ipv6Result.Answers)
                .ToArray();

            var addressRecords = allRecords
                .OfType<AddressRecord>()
                .Select(p => new { Address = p.Address, Alias = DnsString.FromResponseQueryString(p.DomainName) }).ToArray();

            var hostEntry = new IPHostEntry()
            {
                Aliases = new string[0],
                AddressList = addressRecords
                    .Select(p => p.Address)
                    .ToArray()
            };

            if (addressRecords.Length > 1)
            {
                if (addressRecords.Any(p => !p.Alias.Equals(hostString)))
                {
                    hostEntry.Aliases = addressRecords
                        .Select(p => p.Alias.ToString())
                        .Select(p => p.Substring(0, p.Length - 1))
                        .Distinct()
                        .ToArray();
                }
            }
            else if (addressRecords.Length == 1)
            {
                if (allRecords.Any(p => !DnsString.FromResponseQueryString(p.DomainName).Equals(hostString)))
                {
                    hostEntry.Aliases = allRecords
                        .Select(p => p.DomainName.ToString())
                        .Select(p => p.Substring(0, p.Length - 1))
                        .Distinct()
                        .ToArray();
                }
            }

            hostEntry.HostName = hostString.Value.Substring(0, hostString.Value.Length - 1);

            return hostEntry;
        }

        /// <summary>
        /// The <c>GetHostName</c> method queries a DNS server to resolve the hostname of the <paramref name="address"/> via reverse lookup.
        /// </summary>
        /// <param name="query">The <see cref="IDnsQuery"/> instance.</param>
        /// <param name="address">The <see cref="IPAddress"/> to resolve.</param>
        /// <returns>
        /// The hostname if the reverse lookup was successful or <c>null</c>, in case the host was not found.
        /// If <see cref="ILookupClient.ThrowDnsErrors"/> is set to <c>true</c>, this method will throw an <see cref="DnsResponseException"/> instead of returning <c>null</c>!
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="address"/>is null.</exception>
        /// <exception cref="DnsResponseException">If no host has been found and <see cref="ILookupClient.ThrowDnsErrors"/> is <c>true</c>.</exception>
        public static string GetHostName(this IDnsQuery query, IPAddress address)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            var result = query.QueryReverse(address);
            if (result.HasError)
            {
                return null;
            }

            var hostName = result.Answers.PtrRecords().FirstOrDefault()?.PtrDomainName;
            if (string.IsNullOrWhiteSpace(hostName))
            {
                return null;
            }

            // removing the . at the end
            return hostName.Value.Substring(0, hostName.Value.Length - 1);
        }

        /// <summary>
        /// Creates a <c>SRV</c> lookup for <c>_{serviceName}[._{protocol}].{baseDomain}.</c> and aggregates the results
        /// of returned hostname, port and list of <see cref="IPAddress"/>s.
        /// <para>
        /// This method expects matching A or AAAA records to populate the <see cref="IPHostEntry.AddressList"/>,
        /// and/or a <c>CNAME</c> record to populate the <see cref="IPHostEntry.HostName"/> property of the result.
        /// </para>
        /// </summary>
        /// <remarks>
        /// List of IPAddresses and/or hostname can be empty if no matching additional records are returned.
        /// In case no result was found, an empty list will be returned.
        /// </remarks>
        /// <param name="query">The lookup instance.</param>
        /// <param name="baseDomain">The base domain, will be attached to the end of the query string.</param>
        /// <param name="serviceName">The name of the service to look for, without any prefix.</param>
        /// <param name="protocol">
        /// The protocol of the service to query for.
        /// Set it to <see cref="ProtocolType.Unknown"/> or <see cref="ProtocolType.Unspecified"/> to not pass any protocol.
        /// </param>
        /// <returns>Collection of <see cref="ServiceHostEntry"/>s.</returns>
        public static Task<ServiceHostEntry[]> ResolveServiceAsync(this IDnsQuery query, string baseDomain, string serviceName, ProtocolType protocol)
        {
            if (protocol == ProtocolType.Unspecified || protocol == ProtocolType.Unknown)
            {
                return ResolveServiceAsync(query, baseDomain, serviceName, null);
            }

            return ResolveServiceAsync(query, baseDomain, serviceName, protocol.ToString());
        }

        /// <summary>
        /// Creates a <c>SRV</c> lookup for <c>_{serviceName}[._{tag}].{baseDomain}.</c> and aggregates the results
        /// of returned hostname, port and list of <see cref="IPAddress"/>s.
        /// <para>
        /// This method expects matching A or AAAA records to populate the <see cref="IPHostEntry.AddressList"/>,
        /// and/or a <c>CNAME</c> record to populate the <see cref="IPHostEntry.HostName"/> property of the result.
        /// </para>
        /// </summary>
        /// <remarks>
        /// List of IPAddresses and/or hostname can be empty if no matching additional records are returned.
        /// In case no result was found, an empty list will be returned.
        /// </remarks>
        /// <param name="query">The lookup instance.</param>
        /// <param name="baseDomain">The base domain, will be attached to the end of the query string.</param>
        /// <param name="serviceName">The name of the service to look for, without any prefix.</param>
        /// <param name="tag">An optional tag.</param>
        /// <returns>Collection of <see cref="ServiceHostEntry"/>s.</returns>
        public static async Task<ServiceHostEntry[]> ResolveServiceAsync(this IDnsQuery query, string baseDomain, string serviceName, string tag = null)
        {
            if (baseDomain == null)
            {
                throw new ArgumentNullException(nameof(baseDomain));
            }
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName));
            }

            string queryString;
            if (string.IsNullOrWhiteSpace(tag))
            {
                queryString = $"{serviceName}.{baseDomain}.";
            }
            else
            {
                queryString = $"_{serviceName}._{tag}.{baseDomain}.";
            }

            var hosts = new List<ServiceHostEntry>();
            var result = await query.QueryAsync(queryString, QueryType.SRV).ConfigureAwait(false);

            if (result.HasError)
            {
                return hosts.ToArray();
            }

            foreach (var entry in result.Answers.SrvRecords())
            {
                var addresses = result.Additionals
                    .OfType<AddressRecord>()
                    .Where(p => p.DomainName.Equals(entry.Target))
                    .Select(p => p.Address);

                var hostName = result.Additionals
                    .OfType<CNameRecord>()
                    .Where(p => p.DomainName.Equals(entry.Target))
                    .Select(p => p.CanonicalName).FirstOrDefault();

                hosts.Add(new ServiceHostEntry()
                {
                    AddressList = addresses.ToArray(),
                    HostName = hostName,
                    Port = entry.Port
                });
            }

            return hosts.ToArray();
        }
    }

    public class ServiceHostEntry : IPHostEntry
    {
        public int Port { get; set; }
    }
}