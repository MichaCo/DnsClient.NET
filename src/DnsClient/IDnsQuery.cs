using System;
using System.Collections.Generic;
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
        /// Performs a DNS lookup for the given <paramref name="query" />, <paramref name="queryType" /> and <paramref name="queryClass" />.
        /// </summary>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <param name="queryClass">The <see cref="QueryClass"/>.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        IDnsQueryResponse Query(string query, QueryType queryType, QueryClass queryClass = QueryClass.IN);

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="question" />.
        /// </summary>
        /// <param name="question">The domain name query.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="question"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        IDnsQueryResponse Query(DnsQuestion question);

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="question" />.
        /// </summary>
        /// <param name="question">The domain name query.</param>
        /// <param name="queryOptions">Query options to be used instead of <see cref="LookupClient"/>'s settings.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="question"/> or <paramref name="queryOptions"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        IDnsQueryResponse Query(DnsQuestion question, DnsQueryAndServerOptions queryOptions);

        /// <summary>
        /// Returns cached results for the given <paramref name="question" /> from the in-memory cache, if available, or <c>Null</c> otherwise.
        /// </summary>
        /// <remarks>
        /// This method will not perform a full lookup if there is nothing found in cache or the cache is disabled!
        /// </remarks>
        /// <param name="question">The domain name query.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the cached response headers and lists of resource records.
        /// If no matching cache entry is found <c>Null</c> is returned.
        /// </returns>
        IDnsQueryResponse QueryCache(DnsQuestion question);

        /// <summary>
        /// Returns cached results for the given <paramref name="query" />, <paramref name="queryType" /> and <paramref name="queryClass" />
        /// against the in-memory cache, if available, or <c>Null</c> otherwise.
        /// </summary>
        /// <remarks>
        /// This method will not perform a full lookup if there is nothing found in cache or the cache is disabled!
        /// </remarks>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <param name="queryClass">The <see cref="QueryClass"/>.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the cached response headers and lists of resource records.
        /// If no matching cache entry is found <c>Null</c> is returned.
        /// </returns>        
        IDnsQueryResponse QueryCache(string query, QueryType queryType, QueryClass queryClass = QueryClass.IN);

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="query" />, <paramref name="queryType" /> and <paramref name="queryClass" />
        /// </summary>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <param name="queryClass">The <see cref="QueryClass" />.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="OperationCanceledException">If cancellation has been requested for the passed in <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        Task<IDnsQueryResponse> QueryAsync(string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="question" />.
        /// </summary>
        /// <param name="question">The domain name query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="question"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        Task<IDnsQueryResponse> QueryAsync(DnsQuestion question, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="question" />.
        /// </summary>
        /// <param name="question">The domain name query.</param>
        /// <param name="queryOptions">Query options to be used instead of <see cref="LookupClient"/>'s settings.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="question"/> or <paramref name="queryOptions"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        Task<IDnsQueryResponse> QueryAsync(DnsQuestion question, DnsQueryAndServerOptions queryOptions, CancellationToken cancellationToken = default);

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress"/>.
        /// </summary>
        /// <param name="ipAddress">The <see cref="IPAddress"/>.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="ipAddress"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        IDnsQueryResponse QueryReverse(IPAddress ipAddress);

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress"/>.
        /// </summary>
        /// <param name="ipAddress">The <see cref="IPAddress"/>.</param>
        /// <param name="queryOptions">Query options to be used instead of <see cref="LookupClient"/>'s settings.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="ipAddress"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        IDnsQueryResponse QueryReverse(IPAddress ipAddress, DnsQueryAndServerOptions queryOptions);

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress" />.
        /// </summary>
        /// <param name="ipAddress">The <see cref="IPAddress" />.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="ipAddress"/> is null.</exception>
        /// <exception cref="OperationCanceledException">If cancellation has been requested for the passed in <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        Task<IDnsQueryResponse> QueryReverseAsync(IPAddress ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress" />.
        /// </summary>
        /// <param name="ipAddress">The <see cref="IPAddress" />.</param>
        /// <param name="queryOptions">Query options to be used instead of <see cref="LookupClient"/>'s settings.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="ipAddress"/> is null.</exception>
        /// <exception cref="OperationCanceledException">If cancellation has been requested for the passed in <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        Task<IDnsQueryResponse> QueryReverseAsync(IPAddress ipAddress, DnsQueryAndServerOptions queryOptions, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="query" />, <paramref name="queryType" /> and <paramref name="queryClass" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <remarks>
        /// To query specific servers can be useful in cases where you have to use a different DNS server than initially configured
        /// (without creating a new instance of <see cref="ILookupClient"/> for example).
        /// </remarks>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <param name="queryClass">The <see cref="QueryClass" />.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        IDnsQueryResponse QueryServer(IReadOnlyCollection<NameServer> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN);

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="question" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <remarks>
        /// To query specific servers can be useful in cases where you have to use a different DNS server than initially configured
        /// (without creating a new instance of <see cref="ILookupClient"/> for example).
        /// </remarks>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="question">The domain name query.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="servers"/> or <paramref name="question"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        IDnsQueryResponse QueryServer(IReadOnlyCollection<NameServer> servers, DnsQuestion question);

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="question" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <remarks>
        /// To query specific servers can be useful in cases where you have to use a different DNS server than initially configured
        /// (without creating a new instance of <see cref="ILookupClient"/> for example).
        /// </remarks>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="question">The domain name query.</param>
        /// <param name="queryOptions">Query options to be used instead of <see cref="LookupClient"/>'s settings.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="servers"/>, <paramref name="question"/> or <paramref name="queryOptions"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        IDnsQueryResponse QueryServer(IReadOnlyCollection<NameServer> servers, DnsQuestion question, DnsQueryOptions queryOptions);

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="query" />, <paramref name="queryType" /> and <paramref name="queryClass" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <remarks>
        /// To query specific servers can be useful in cases where you have to use a different DNS server than initially configured
        /// (without creating a new instance of <see cref="ILookupClient"/> for example).
        /// </remarks>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <param name="queryClass">The <see cref="QueryClass" />.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        IDnsQueryResponse QueryServer(IReadOnlyCollection<IPEndPoint> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN);

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="query" />, <paramref name="queryType" /> and <paramref name="queryClass" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <remarks>
        /// To query specific servers can be useful in cases where you have to use a different DNS server than initially configured
        /// (without creating a new instance of <see cref="ILookupClient"/> for example).
        /// </remarks>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <param name="queryClass">The <see cref="QueryClass" />.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        IDnsQueryResponse QueryServer(IReadOnlyCollection<IPAddress> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN);

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="query" />, <paramref name="queryType" /> and <paramref name="queryClass" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <remarks>
        /// To query specific servers can be useful in cases where you have to use a different DNS server than initially configured
        /// (without creating a new instance of <see cref="ILookupClient"/> for example).
        /// </remarks>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <param name="queryClass">The <see cref="QueryClass" />.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="OperationCanceledException">If cancellation has been requested for the passed in <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<NameServer> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="question" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <remarks>
        /// To query specific servers can be useful in cases where you have to use a different DNS server than initially configured
        /// (without creating a new instance of <see cref="ILookupClient"/> for example).
        /// </remarks>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="question">The domain name query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="servers"/> or <paramref name="question"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<NameServer> servers, DnsQuestion question, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="question" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <remarks>
        /// To query specific servers can be useful in cases where you have to use a different DNS server than initially configured
        /// (without creating a new instance of <see cref="ILookupClient"/> for example).
        /// </remarks>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="question">The domain name query.</param>
        /// <param name="queryOptions">Query options to be used instead of <see cref="LookupClient"/>'s settings.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="servers"/>, <paramref name="question"/> or <paramref name="queryOptions"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<NameServer> servers, DnsQuestion question, DnsQueryOptions queryOptions, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="query" />, <paramref name="queryType" /> and <paramref name="queryClass" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <remarks>
        /// To query specific servers can be useful in cases where you have to use a different DNS server than initially configured
        /// (without creating a new instance of <see cref="ILookupClient"/> for example).
        /// </remarks>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <param name="queryClass">The <see cref="QueryClass" />.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="OperationCanceledException">If cancellation has been requested for the passed in <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<IPAddress> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a DNS lookup for the given <paramref name="query" />, <paramref name="queryType" /> and <paramref name="queryClass" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <remarks>
        /// To query specific servers can be useful in cases where you have to use a different DNS server than initially configured
        /// (without creating a new instance of <see cref="ILookupClient"/> for example).
        /// </remarks>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="query">The domain name query.</param>
        /// <param name="queryType">The <see cref="QueryType" />.</param>
        /// <param name="queryClass">The <see cref="QueryClass" />.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which contains the response headers and lists of resource records.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        /// <exception cref="OperationCanceledException">If cancellation has been requested for the passed in <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        Task<IDnsQueryResponse> QueryServerAsync(IReadOnlyCollection<IPEndPoint> servers, string query, QueryType queryType, QueryClass queryClass = QueryClass.IN, CancellationToken cancellationToken = default);

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="ipAddress">The <see cref="IPAddress" />.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="servers"/> or <paramref name="ipAddress"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        IDnsQueryResponse QueryServerReverse(IReadOnlyCollection<IPAddress> servers, IPAddress ipAddress);

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="ipAddress">The <see cref="IPAddress" />.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="servers"/> or <paramref name="ipAddress"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        IDnsQueryResponse QueryServerReverse(IReadOnlyCollection<IPEndPoint> servers, IPAddress ipAddress);

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="ipAddress">The <see cref="IPAddress" />.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="servers"/> or <paramref name="ipAddress"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        IDnsQueryResponse QueryServerReverse(IReadOnlyCollection<NameServer> servers, IPAddress ipAddress);

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="ipAddress">The <see cref="IPAddress" />.</param>
        /// <param name="queryOptions">Query options to be used instead of <see cref="LookupClient"/>'s settings.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="servers"/>, <paramref name="ipAddress"/> or <paramref name="queryOptions"/> is null.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        IDnsQueryResponse QueryServerReverse(IReadOnlyCollection<NameServer> servers, IPAddress ipAddress, DnsQueryOptions queryOptions);

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="ipAddress">The <see cref="IPAddress" />.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="servers"/> or <paramref name="ipAddress"/> is null.</exception>
        /// <exception cref="OperationCanceledException">If cancellation has been requested for the passed in <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        Task<IDnsQueryResponse> QueryServerReverseAsync(IReadOnlyCollection<IPAddress> servers, IPAddress ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="ipAddress">The <see cref="IPAddress" />.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="servers"/> or <paramref name="ipAddress"/> is null.</exception>
        /// <exception cref="OperationCanceledException">If cancellation has been requested for the passed in <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        Task<IDnsQueryResponse> QueryServerReverseAsync(IReadOnlyCollection<IPEndPoint> servers, IPAddress ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="ipAddress">The <see cref="IPAddress" />.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="servers"/> or <paramref name="ipAddress"/> is null.</exception>
        /// <exception cref="OperationCanceledException">If cancellation has been requested for the passed in <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        Task<IDnsQueryResponse> QueryServerReverseAsync(IReadOnlyCollection<NameServer> servers, IPAddress ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Does a reverse lookup for the <paramref name="ipAddress" />
        /// using only the passed in <paramref name="servers"/>.
        /// </summary>
        /// <param name="servers">The list of one or more server(s) which should be used for the lookup.</param>
        /// <param name="ipAddress">The <see cref="IPAddress" />.</param>
        /// <param name="queryOptions">Query options to be used instead of <see cref="LookupClient"/>'s settings.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="IDnsQueryResponse" /> which might contain the <see cref="DnsClient.Protocol.PtrRecord" /> for the <paramref name="ipAddress"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="servers"/> collection doesn't contain any elements.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="servers"/>, <paramref name="ipAddress"/> or <paramref name="queryOptions"/> is null.</exception>
        /// <exception cref="OperationCanceledException">If cancellation has been requested for the passed in <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="DnsResponseException">After retries and fallbacks, if none of the servers were accessible, timed out or (if <see cref="DnsQueryOptions.ThrowDnsErrors"/> is enabled) returned error results.</exception>
        Task<IDnsQueryResponse> QueryServerReverseAsync(IReadOnlyCollection<NameServer> servers, IPAddress ipAddress, DnsQueryOptions queryOptions, CancellationToken cancellationToken = default);
    }
}