using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DnsClient
{
    /// <summary>
    /// Generic contract to query DNS endpoints. Implemented by <see cref="LookupClient"/>.
    /// </summary>
    public interface IDnsQuery
    {
        /// <summary>
        /// Performs a DNS lookup for <paramref name="query" /> and <paramref name="queryType" />.
        /// </summary>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        IDnsQueryResponse Query(string query, QueryType queryType);

        /// <summary>
        /// Performs a DNS lookup for <paramref name="query" />, <paramref name="queryType" /> and <paramref name="queryClass"/>.
        /// </summary>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <param name="queryClass">The <see cref="QueryClass"/>.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        IDnsQueryResponse Query(string query, QueryType queryType, QueryClass queryClass);

        /// <summary>
        /// Performs a DNS lookup for <paramref name="query" /> and <paramref name="queryType" />.
        /// </summary>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType);

        /// <summary>
        /// Performs a DNS lookup for <paramref name="query" />, <paramref name="queryType" /> and <paramref name="queryClass"/>.
        /// </summary>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <param name="queryClass">The <see cref="QueryClass"/>.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType, QueryClass queryClass);

        /// <summary>
        /// Performs a DNS lookup for <paramref name="query" /> and <paramref name="queryType" />.
        /// </summary>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType, CancellationToken cancellationToken);

        /// <summary>
        /// Performs a DNS lookup for <paramref name="query" />, <paramref name="queryType" /> and <paramref name="queryClass" />.
        /// </summary>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <param name="queryClass">The <see cref="QueryClass" />.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType, QueryClass queryClass, CancellationToken cancellationToken);

        /// <summary>
        /// Does a reverse lookup of the <paramref name="ipAddress"/>.
        /// </summary>
        /// <param name="ipAddress">The <see cref="IPAddress"/>.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which should contain the <see cref="DnsClient.Protocol.PtrRecord"/>.
        /// </returns>
        IDnsQueryResponse QueryReverse(IPAddress ipAddress);

        /// <summary>
        /// Does a reverse lookup of the <paramref name="ipAddress"/>.
        /// </summary>
        /// <param name="ipAddress">The <see cref="IPAddress"/>.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which should contain the <see cref="DnsClient.Protocol.PtrRecord"/>.
        /// </returns>
        Task<IDnsQueryResponse> QueryReverseAsync(IPAddress ipAddress);

        /// <summary>
        /// Does a reverse lookup of the <paramref name="ipAddress" />.
        /// </summary>
        /// <param name="ipAddress">The <see cref="IPAddress" />.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which should contain the <see cref="DnsClient.Protocol.PtrRecord" />.
        /// </returns>
        Task<IDnsQueryResponse> QueryReverseAsync(IPAddress ipAddress, CancellationToken cancellationToken);
    }
}