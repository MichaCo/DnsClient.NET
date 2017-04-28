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
        /// Performs a DNS lookup by <paramref name="query"/> and <paramref name="queryType"/>.
        /// </summary>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType"/>.</param>
        /// <returns>The <see cref="IDnsQueryResponse"/> which contains the response headers and lists of resource records.</returns>
        IDnsQueryResponse Query(string query, QueryType queryType);

        IDnsQueryResponse Query(string query, QueryType queryType, QueryClass queryClass);

        Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType);

        Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType, QueryClass queryClass);

        Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType, CancellationToken cancellationToken);

        Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType, QueryClass queryClass, CancellationToken cancellationToken);

        IDnsQueryResponse QueryReverse(IPAddress ipAddress);

        Task<IDnsQueryResponse> QueryReverseAsync(IPAddress ipAddress);

        Task<IDnsQueryResponse> QueryReverseAsync(IPAddress ipAddress, CancellationToken cancellationToken);
    }
}