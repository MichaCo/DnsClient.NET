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
        /// Creates a SRV lookup for <code>[name1.[nameN]].[baseDomain].</code> and aggregates the results
        /// of returned host name, port and list of <see cref="IPAddress"/>s.
        /// </summary>
        /// <remarks>
        /// List of IPAddresses can be empty if no matching additional records are returned.
        /// In case no result was found, an empty list will be returned.
        /// </remarks>
        /// <param name="query">The lookup instance.</param>
        /// <param name="baseDomain">The base domain, will be attached to the end of the query string.</param>
        /// <param name="names">List of tokens to identify the service. Will be concatinated in the given order.</param>
        /// <returns></returns>
        public static Task<ServiceHostEntry[]> ResolveServiceAsync(this IDnsQuery query, string baseDomain, string serviceName, ProtocolType protocol)
        {
            if (protocol == ProtocolType.Unspecified || protocol == ProtocolType.Unknown)
            {
                return ResolveServiceAsync(query, baseDomain, serviceName, null);
            }

            return ResolveServiceAsync(query, baseDomain, serviceName, protocol.ToString());
        }

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
            var result = await query.QueryAsync(queryString, QueryType.SRV);

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

        public class ServiceHostEntry : IPHostEntry
        {
            public int Port { get; set; }
        }
    }
}