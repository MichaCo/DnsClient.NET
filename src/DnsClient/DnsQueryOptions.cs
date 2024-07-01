// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using DnsClient.Protocol;

namespace DnsClient
{
    /// <summary>
    /// The options used to override the defaults of <see cref="LookupClient"/> per query.
    /// </summary>
    public class DnsQueryOptions
    {
        /// <summary>
        /// The minimum payload size. Anything equal or less than that will default back to this value and might disable EDNS.
        /// </summary>
        public const int MinimumBufferSize = 512;

        /// <summary>
        /// The maximum reasonable payload size.
        /// </summary>
        public const int MaximumBufferSize = 4096;

        private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan s_infiniteTimeout = System.Threading.Timeout.InfiniteTimeSpan;
        private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);
        private TimeSpan _timeout = s_defaultTimeout;
        private int _ednsBufferSize = MaximumBufferSize;
        private TimeSpan _failedResultsCacheDuration = s_defaultTimeout;

        /// <summary>
        /// Gets or sets a flag indicating whether each <see cref="IDnsQueryResponse"/> will contain a full documentation of the response(s).
        /// Default is <c>False</c>.
        /// </summary>
        /// <seealso cref="IDnsQueryResponse.AuditTrail"/>
        public bool EnableAuditTrail { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether DNS queries should use response caching or not.
        /// The cache duration is calculated by the resource record of the response. Usually, the lowest TTL is used.
        /// Default is <c>True</c>.
        /// </summary>
        /// <remarks>
        /// In case the DNS Server returns records with a TTL of zero. The response cannot be cached.
        /// </remarks>
        public bool UseCache { get; set; } = true;

        /// <summary>
        /// Gets or sets a flag indicating whether DNS queries should instruct the DNS server to do recursive lookups, or not.
        /// Default is <c>True</c>.
        /// </summary>
        /// <value>The flag indicating if recursion should be used or not.</value>
        public bool Recursion { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of tries to get a response from one name server before trying the next one.
        /// Only transient errors, like network or connection errors will be retried.
        /// Default is <c>2</c> which will be three tries total.
        /// <para>
        /// If all configured <see cref="DnsQueryAndServerOptions.NameServers"/> error out after retries, an exception will be thrown at the end.
        /// </para>
        /// </summary>
        /// <value>The number of retries.</value>
        public int Retries { get; set; } = 2;

        /// <summary>
        /// Gets or sets a flag indicating whether the <see cref="ILookupClient"/> should throw a <see cref="DnsResponseException"/>
        /// in case the query result has a <see cref="DnsResponseCode"/> other than <see cref="DnsResponseCode.NoError"/>.
        /// Default is <c>False</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If set to <c>False</c>, the query will return a result with an <see cref="IDnsQueryResponse.ErrorMessage"/>
        /// which contains more information.
        /// </para>
        /// <para>
        /// If set to <c>True</c>, any query method of <see cref="IDnsQuery"/> will throw an <see cref="DnsResponseException"/> if
        /// the response header indicates an error.
        /// </para>
        /// <para>
        /// If both, <see cref="ContinueOnDnsError"/> and <see cref="ThrowDnsErrors"/> are set to <c>True</c>,
        /// <see cref="ILookupClient"/> will continue to query all configured <see cref="DnsQueryAndServerOptions.NameServers"/>.
        /// If none of the servers yield a valid response, a <see cref="DnsResponseException"/> will be thrown
        /// with the error of the last response.
        /// </para>
        /// </remarks>
        /// <seealso cref="DnsResponseCode"/>
        /// <seealso cref="ContinueOnDnsError"/>
        public bool ThrowDnsErrors { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether the <see cref="ILookupClient"/> can cycle through all
        /// configured <see cref="DnsQueryAndServerOptions.NameServers"/> on each consecutive request, basically using a random server, or not.
        /// Default is <c>True</c>.
        /// If only one <see cref="NameServer"/> is configured, this setting is not used.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If <c>False</c>, configured endpoint will be used in random order.
        /// If <c>True</c>, the order will be preserved.
        /// </para>
        /// <para>
        /// Even if <see cref="UseRandomNameServer"/> is set to <c>True</c>, the endpoint might still get
        /// disabled and might not being used for some time if it errors out, e.g. no connection can be established.
        /// </para>
        /// </remarks>
        public bool UseRandomNameServer { get; set; } = true;

        /// <summary>
        /// Gets or sets a flag indicating whether to query the next configured <see cref="DnsQueryAndServerOptions.NameServers"/> in case the response of the last query
        /// returned a <see cref="DnsResponseCode"/> other than <see cref="DnsResponseCode.NoError"/>.
        /// Default is <c>True</c>.
        /// </summary>
        /// <remarks>
        /// If <c>True</c>, lookup client will continue until a server returns a valid result, or,
        /// if no <see cref="DnsQueryAndServerOptions.NameServers"/> yield a valid result, the last response with the error will be returned.
        /// In case no server yields a valid result and <see cref="ThrowDnsErrors"/> is also enabled, an exception
        /// will be thrown containing the error of the last response.
        /// <para>
        /// If  <c>True</c> and <see cref="ThrowDnsErrors"/> is enabled, the exception will be thrown on first encounter without trying any other servers.
        /// </para>
        /// </remarks>
        /// <seealso cref="ThrowDnsErrors"/>
        public bool ContinueOnDnsError { get; set; } = true;

        /// <summary>
        /// Gets or sets a flag indicating whether to query the next configured <see cref="DnsQueryAndServerOptions.NameServers"/>
        /// if the response does not have an error <see cref="DnsResponseCode"/> but the query was not answered by the response.
        /// Default is <c>True</c>.
        /// </summary>
        /// <remarks>
        /// The query is answered if there is at least one <see cref="DnsResourceRecord"/> in the answers section
        /// matching the <see cref="DnsQuestion"/>'s <see cref="QueryType"/>.
        /// <para>
        /// If there are zero answers in the response, the query is not answered, independent of the <see cref="QueryType"/>.
        /// If there are answers in the response, the <see cref="QueryType"/> is used to find a matching record,
        /// query types <see cref="QueryType.ANY"/> and <see cref="QueryType.AXFR"/> will be ignored by this check.
        /// </para>
        /// </remarks>
        public bool ContinueOnEmptyResponse { get; set; } = true;

        /// <summary>
        /// Gets or sets the request timeout in milliseconds. <see cref="Timeout"/> is used for limiting the connection and request time for one operation.
        /// Timeout must be greater than zero and less than <see cref="int.MaxValue"/>.
        /// If <see cref="Timeout.InfiniteTimeSpan"/> (or -1) is used, no timeout will be applied.
        /// Default is 5 seconds.
        /// </summary>
        /// <remarks>
        /// If a very short timeout is configured, queries will more likely result in <see cref="TimeoutException"/>s.
        /// <para>
        /// Important to note, <see cref="TimeoutException"/>s will be retried, if <see cref="Retries"/> are not disabled (set to <c>0</c>).
        /// This should help in case one or more configured DNS servers are not reachable or under load for example.
        /// </para>
        /// </remarks>
        public TimeSpan Timeout
        {
            get { return _timeout; }
            set
            {
                if ((value <= TimeSpan.Zero || value > s_maxTimeout) && value != s_infiniteTimeout)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _timeout = value;
            }
        }

        /// <summary>
        /// Gets or sets a flag indicating whether TCP should be used in case a UDP response is truncated.
        /// Default is <c>True</c>.
        /// <para>
        /// If <c>False</c>, truncated results will potentially yield no or incomplete answers.
        /// </para>
        /// </summary>
        public bool UseTcpFallback { get; set; } = true;

        /// <summary>
        /// Gets or sets a flag indicating whether UDP should not be used at all.
        /// Default is <c>False</c>.
        /// <para>
        /// Enable this only if UDP cannot be used because of your firewall rules for example.
        /// Also, zone transfers (see <see cref="QueryType.AXFR"/>) must use TCP only.
        /// </para>
        /// </summary>
        public bool UseTcpOnly { get; set; }

        /// <summary>
        /// Gets or sets the maximum buffer used for UDP requests.
        /// Defaults to <c>4096</c>.
        /// <para>
        /// If this value is less or equal to <c>512</c> bytes, EDNS might be disabled.
        /// </para>
        /// </summary>
        public int ExtendedDnsBufferSize
        {
            get
            {
                return _ednsBufferSize;
            }
            set
            {
                _ednsBufferSize =
                    value < MinimumBufferSize ? MinimumBufferSize :
                    value > MaximumBufferSize ? MaximumBufferSize : value;
            }
        }

        /// <summary>
        /// Gets or sets a flag indicating whether EDNS should be enabled and the <c>DO</c> flag should be set.
        /// Defaults to <c>False</c>.
        /// </summary>
        public bool RequestDnsSecRecords { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether the DNS failures are being cached. The purpose of caching 
        /// failures is to reduce repeated lookup attempts within a short space of time.
        /// Defaults to <c>False</c>.
        /// </summary>
        public bool CacheFailedResults { get; set; }

        /// <summary>
        /// Gets or sets the duration to cache failed lookups. Does not apply if failed lookups are not being cached.
        /// Defaults to <c>5 seconds</c>.
        /// </summary>
        public TimeSpan FailedResultsCacheDuration
        {
            get { return _failedResultsCacheDuration; }
            set
            {
                if ((value <= TimeSpan.Zero || value > s_maxTimeout) && value != s_infiniteTimeout)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _failedResultsCacheDuration = value;
            }
        }

        /// <summary>
        /// Converts the query options into readonly settings.
        /// </summary>
        /// <param name="fromOptions">The options.</param>
        public static implicit operator DnsQuerySettings(DnsQueryOptions fromOptions)
        {
            if (fromOptions == null)
            {
                return null;
            }

            return new DnsQuerySettings(fromOptions);
        }
    }

    /// <summary>
    /// The options used to override the defaults of <see cref="LookupClient"/> per query.
    /// </summary>
    public class DnsQueryAndServerOptions : DnsQueryOptions
    {
        /// <summary>
        /// Creates a new instance of <see cref="DnsQueryAndServerOptions"/> without name servers.
        /// If no nameservers are configured, a query will fallback to the nameservers already configured on the <see cref="LookupClient"/> instance.
        /// </summary>
        public DnsQueryAndServerOptions()
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="DnsQueryAndServerOptions"/>.
        /// </summary>
        /// <param name="nameServers">A collection of name servers.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="nameServers"/> is null.</exception>
        public DnsQueryAndServerOptions(params NameServer[] nameServers)
            : base()
        {
            if (nameServers != null && nameServers.Length > 0)
            {
                NameServers = nameServers.ToList();
            }
            else
            {
                throw new ArgumentNullException(nameof(nameServers));
            }
        }

        // TODO: remove overloads in favor of implicit conversion to NameServer?
        /// <summary>
        /// Creates a new instance of <see cref="DnsQueryAndServerOptions"/>.
        /// </summary>
        /// <param name="nameServers">A collection of name servers.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="nameServers"/> is null.</exception>
        public DnsQueryAndServerOptions(params IPEndPoint[] nameServers)
           : this(nameServers?.Select(p => (NameServer)p).ToArray())
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="DnsQueryAndServerOptions"/>.
        /// </summary>
        /// <param name="nameServers">A collection of name servers.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="nameServers"/> is null.</exception>
        public DnsQueryAndServerOptions(params IPAddress[] nameServers)
            : this(nameServers?.Select(p => (NameServer)p).ToArray())
        {
        }

        /// <summary>
        /// Gets a list of name servers which should be used to query.
        /// </summary>
        public IReadOnlyList<NameServer> NameServers { get; } = new NameServer[0];

        /// <summary>
        /// Converts the query options into readonly settings.
        /// </summary>
        /// <param name="fromOptions">The options.</param>
        public static implicit operator DnsQueryAndServerSettings(DnsQueryAndServerOptions fromOptions)
        {
            if (fromOptions == null)
            {
                return null;
            }

            return new DnsQueryAndServerSettings(fromOptions);
        }
    }

    /// <summary>
    /// The options used to configure defaults in <see cref="LookupClient"/> and to optionally use specific settings per query.
    /// </summary>
    public class LookupClientOptions : DnsQueryAndServerOptions
    {
        private static readonly TimeSpan s_infiniteTimeout = System.Threading.Timeout.InfiniteTimeSpan;

        // max is 24 days
        private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

        private TimeSpan? _minimumCacheTimeout;
        private TimeSpan? _maximumCacheTimeout;

        /// <summary>
        /// Creates a new instance of <see cref="LookupClientOptions"/> with default settings.
        /// </summary>
        public LookupClientOptions()
            : base()
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="LookupClientOptions"/>.
        /// </summary>
        /// <param name="nameServers">A collection of name servers.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="nameServers"/> is null.</exception>
        public LookupClientOptions(params NameServer[] nameServers)
            : base(nameServers)
        {
            AutoResolveNameServers = false;
        }

        /// <summary>
        /// Creates a new instance of <see cref="LookupClientOptions"/>.
        /// </summary>
        /// <param name="nameServers">A collection of name servers.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="nameServers"/> is null.</exception>
        public LookupClientOptions(params IPEndPoint[] nameServers)
            : base(nameServers)
        {
            AutoResolveNameServers = false;
        }

        /// <summary>
        /// Creates a new instance of <see cref="LookupClientOptions"/>.
        /// </summary>
        /// <param name="nameServers">A collection of name servers.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="nameServers"/> is null.</exception>
        public LookupClientOptions(params IPAddress[] nameServers)
            : base(nameServers)
        {
            AutoResolveNameServers = false;
        }

        /// <summary>
        /// Gets or sets a flag indicating whether the name server collection should be automatically resolved.
        /// Default is <c>True</c>.
        /// </summary>
        /// <remarks>
        /// If name servers are configured manually via the constructor, this flag is set to false.
        /// If you want both, your manually configured servers and auto resolved name servers,
        /// you can use both (ctor or) <see cref="DnsQueryAndServerOptions.NameServers"/> and <see cref="AutoResolveNameServers"/> set to <c>True</c>.
        /// </remarks>
        public bool AutoResolveNameServers { get; set; } = true;

        /// <summary>
        /// Gets or sets a <see cref="TimeSpan"/> which can override the TTL of a resource record in case the
        /// TTL of the record is lower than this minimum value.
        /// Default is <c>Null</c>.
        /// <para>
        /// This is useful in case the server returns records with zero TTL.
        /// </para>
        /// </summary>
        /// <remarks>
        /// This setting gets ignored in case <see cref="DnsQueryOptions.UseCache"/> is set to <c>False</c>,
        /// or the value is set to <c>Null</c> or <see cref="TimeSpan.Zero"/>.
        /// The maximum value is 24 days or <see cref="Timeout.Infinite"/> (choose a wise setting).
        /// </remarks>
        public TimeSpan? MinimumCacheTimeout
        {
            get { return _minimumCacheTimeout; }
            set
            {
                if (value.HasValue &&
                    (value < TimeSpan.Zero || value > s_maxTimeout) && value != s_infiniteTimeout)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                if (value == TimeSpan.Zero)
                {
                    _minimumCacheTimeout = null;
                }
                else
                {
                    _minimumCacheTimeout = value;
                }
            }
        }

        /// <summary>
        /// Gets a <see cref="TimeSpan"/> which can override the TTL of a resource record in case the
        /// TTL of the record is higher than this maximum value.
        /// Default is <c>Null</c>.
        /// </summary>
        /// <remarks>
        /// This setting gets ignored in case <see cref="DnsQueryOptions.UseCache"/> is set to <c>False</c>,
        /// or the value is set to <c>Null</c>, <see cref="Timeout.Infinite"/> or <see cref="TimeSpan.Zero"/>.
        /// The maximum value is 24 days (which shouldn't be used).
        /// </remarks>
        public TimeSpan? MaximumCacheTimeout
        {
            get { return _maximumCacheTimeout; }
            set
            {
                if (value.HasValue &&
                    (value < TimeSpan.Zero || value > s_maxTimeout) && value != s_infiniteTimeout)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                if (value == TimeSpan.Zero)
                {
                    _maximumCacheTimeout = null;
                }
                else
                {
                    _maximumCacheTimeout = value;
                }
            }
        }
    }

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode() - intentionally, Equals is only used by unit tests

    /// <summary>
    /// The options used to override the defaults of <see cref="LookupClient"/> per query.
    /// </summary>
    public class DnsQuerySettings : IEquatable<DnsQuerySettings>
    {
        /// <summary>
        /// Gets a flag indicating whether each <see cref="IDnsQueryResponse"/> will contain a full documentation of the response(s).
        /// Default is <c>False</c>.
        /// </summary>
        /// <seealso cref="IDnsQueryResponse.AuditTrail"/>
        public bool EnableAuditTrail { get; }

        /// <summary>
        /// Gets a flag indicating whether DNS queries should use response caching or not.
        /// The cache duration is calculated by the resource record of the response. Usually, the lowest TTL is used.
        /// Default is <c>True</c>.
        /// </summary>
        /// <remarks>
        /// In case the DNS Server returns records with a TTL of zero. The response cannot be cached.
        /// </remarks>
        public bool UseCache { get; }

        /// <summary>
        /// Gets a flag indicating whether DNS queries should instruct the DNS server to do recursive lookups, or not.
        /// Default is <c>True</c>.
        /// </summary>
        /// <value>The flag indicating if recursion should be used or not.</value>
        public bool Recursion { get; }

        /// <summary>
        /// Gets the number of tries to get a response from one name server before trying the next one.
        /// Only transient errors, like network or connection errors will be retried.
        /// Default is <c>5</c>.
        /// <para>
        /// If all configured <see cref="DnsQueryAndServerSettings.NameServers"/> error out after retries, an exception will be thrown at the end.
        /// </para>
        /// </summary>
        /// <value>The number of retries.</value>
        public int Retries { get; }

        /// <summary>
        /// Gets a flag indicating whether the <see cref="ILookupClient"/> should throw a <see cref="DnsResponseException"/>
        /// in case the query result has a <see cref="DnsResponseCode"/> other than <see cref="DnsResponseCode.NoError"/>.
        /// Default is <c>False</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If set to <c>False</c>, the query will return a result with an <see cref="IDnsQueryResponse.ErrorMessage"/>
        /// which contains more information.
        /// </para>
        /// <para>
        /// If set to <c>True</c>, any query method of <see cref="IDnsQuery"/> will throw an <see cref="DnsResponseException"/> if
        /// the response header indicates an error.
        /// </para>
        /// <para>
        /// If both, <see cref="ContinueOnDnsError"/> and <see cref="ThrowDnsErrors"/> are set to <c>True</c>,
        /// <see cref="ILookupClient"/> will continue to query all configured <see cref="DnsQueryAndServerSettings.NameServers"/>.
        /// If none of the servers yield a valid response, a <see cref="DnsResponseException"/> will be thrown
        /// with the error of the last response.
        /// </para>
        /// </remarks>
        /// <seealso cref="DnsResponseCode"/>
        /// <seealso cref="ContinueOnDnsError"/>
        public bool ThrowDnsErrors { get; }

        /// <summary>
        /// Gets a flag indicating whether the <see cref="ILookupClient"/> can cycle through all
        /// configured <see cref="DnsQueryAndServerSettings.NameServers"/> on each consecutive request, basically using a random server, or not.
        /// Default is <c>True</c>.
        /// If only one <see cref="NameServer"/> is configured, this setting is not used.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If <c>False</c>, configured endpoint will be used in random order.
        /// If <c>True</c>, the order will be preserved.
        /// </para>
        /// <para>
        /// Even if <see cref="UseRandomNameServer"/> is set to <c>True</c>, the endpoint might still get
        /// disabled and might not being used for some time if it errors out, e.g. no connection can be established.
        /// </para>
        /// </remarks>
        public bool UseRandomNameServer { get; }

        /// <summary>
        /// Gets a flag indicating whether to query the next configured <see cref="DnsQueryAndServerSettings.NameServers"/> in case the response of the last query
        /// returned a <see cref="DnsResponseCode"/> other than <see cref="DnsResponseCode.NoError"/>.
        /// Default is <c>True</c>.
        /// </summary>
        /// <remarks>
        /// If <c>True</c>, lookup client will continue until a server returns a valid result, or,
        /// if no <see cref="DnsQueryAndServerSettings.NameServers"/> yield a valid result, the last response with the error will be returned.
        /// In case no server yields a valid result and <see cref="ThrowDnsErrors"/> is also enabled, an exception
        /// will be thrown containing the error of the last response.
        /// <para>
        /// If  <c>True</c> and <see cref="ThrowDnsErrors"/> is enabled, the exception will be thrown on first encounter without trying any other servers.
        /// </para>
        /// </remarks>
        /// <seealso cref="ThrowDnsErrors"/>
        public bool ContinueOnDnsError { get; }

        /// <summary>
        /// Gets or sets a flag indicating whether to query the next configured <see cref="DnsQueryAndServerOptions.NameServers"/>
        /// if the response does not have an error <see cref="DnsResponseCode"/> but the query was not answered by the response.
        /// Default is <c>True</c>.
        /// </summary>
        /// <remarks>
        /// The query is answered if there is at least one <see cref="DnsResourceRecord"/> in the answers section
        /// matching the <see cref="DnsQuestion"/>'s <see cref="QueryType"/>.
        /// <para>
        /// If there are zero answers in the response, the query is not answered, independent of the <see cref="QueryType"/>.
        /// If there are answers in the response, the <see cref="QueryType"/> is used to find a matching record,
        /// query types <see cref="QueryType.ANY"/> and <see cref="QueryType.AXFR"/> will be ignored by this check.
        /// </para>
        /// </remarks>
        public bool ContinueOnEmptyResponse { get; }

        /// <summary>
        /// Gets the request timeout in milliseconds. <see cref="Timeout"/> is used for limiting the connection and request time for one operation.
        /// Timeout must be greater than zero and less than <see cref="int.MaxValue"/>.
        /// If <see cref="Timeout.InfiniteTimeSpan"/> (or -1) is used, no timeout will be applied.
        /// Default is 5 seconds.
        /// </summary>
        /// <remarks>
        /// If a very short timeout is configured, queries will more likely result in <see cref="TimeoutException"/>s.
        /// <para>
        /// Important to note, <see cref="TimeoutException"/>s will be retried, if <see cref="Retries"/> are not disabled (set to <c>0</c>).
        /// This should help in case one or more configured DNS servers are not reachable or under load for example.
        /// </para>
        /// </remarks>
        public TimeSpan Timeout { get; }

        /// <summary>
        /// Gets a flag indicating whether TCP should be used in case a UDP response is truncated.
        /// Default is <c>True</c>.
        /// <para>
        /// If <c>False</c>, truncated results will potentially yield no or incomplete answers.
        /// </para>
        /// </summary>
        public bool UseTcpFallback { get; }

        /// <summary>
        /// Gets a flag indicating whether UDP should not be used at all.
        /// Default is <c>False</c>.
        /// <para>
        /// Enable this only if UDP cannot be used because of your firewall rules for example.
        /// Also, zone transfers (see <see cref="QueryType.AXFR"/>) must use TCP only.
        /// </para>
        /// </summary>
        public bool UseTcpOnly { get; }

        /// <summary>
        /// Gets a flag indicating whether EDNS is enabled based on the values
        /// of <see cref="ExtendedDnsBufferSize"/> and <see cref="RequestDnsSecRecords"/>.
        /// </summary>
        public bool UseExtendedDns => ExtendedDnsBufferSize > DnsQueryOptions.MinimumBufferSize || RequestDnsSecRecords;

        /// <summary>
        /// Gets the maximum buffer used for UDP requests.
        /// Defaults to <c>4096</c>.
        /// <para>
        /// If this value is less or equal to <c>512</c> bytes, EDNS might be disabled.
        /// </para>
        /// </summary>
        public int ExtendedDnsBufferSize { get; }

        /// <summary>
        /// Gets a flag indicating whether EDNS should be enabled and the <c>DO</c> flag should be set.
        /// Defaults to <c>False</c>.
        /// </summary>
        public bool RequestDnsSecRecords { get; }

        /// <summary>
        /// Gets a flag indicating whether the DNS failures are being cached. The purpose of caching 
        /// failures is to reduce repeated lookup attempts within a short space of time.
        /// Defaults to <c>False</c>.
        /// </summary>
        public bool CacheFailedResults { get; }

        /// <summary>
        /// If failures are being cached this value indicates how long they will be held in the cache for.
        /// Defaults to <c>5 seconds</c>.
        /// </summary>
        public TimeSpan FailedResultsCacheDuration { get; }

        /// <summary>
        /// Creates a new instance of <see cref="DnsQueryAndServerSettings"/>.
        /// </summary>
        public DnsQuerySettings(DnsQueryOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ContinueOnDnsError = options.ContinueOnDnsError;
            ContinueOnEmptyResponse = options.ContinueOnEmptyResponse;
            EnableAuditTrail = options.EnableAuditTrail;
            Recursion = options.Recursion;
            Retries = options.Retries;
            ThrowDnsErrors = options.ThrowDnsErrors;
            Timeout = options.Timeout;
            UseCache = options.UseCache;
            UseRandomNameServer = options.UseRandomNameServer;
            UseTcpFallback = options.UseTcpFallback;
            UseTcpOnly = options.UseTcpOnly;
            ExtendedDnsBufferSize = options.ExtendedDnsBufferSize;
            RequestDnsSecRecords = options.RequestDnsSecRecords;
            CacheFailedResults = options.CacheFailedResults;
            FailedResultsCacheDuration = options.FailedResultsCacheDuration;
        }

        /// <inheritdocs />
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return Equals(obj as DnsQuerySettings);
        }

        /// <inheritdocs />
        public bool Equals(DnsQuerySettings other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return EnableAuditTrail == other.EnableAuditTrail &&
                   UseCache == other.UseCache &&
                   Recursion == other.Recursion &&
                   Retries == other.Retries &&
                   ThrowDnsErrors == other.ThrowDnsErrors &&
                   UseRandomNameServer == other.UseRandomNameServer &&
                   ContinueOnDnsError == other.ContinueOnDnsError &&
                   ContinueOnEmptyResponse == other.ContinueOnEmptyResponse &&
                   Timeout.Equals(other.Timeout) &&
                   UseTcpFallback == other.UseTcpFallback &&
                   UseTcpOnly == other.UseTcpOnly &&
                   ExtendedDnsBufferSize == other.ExtendedDnsBufferSize &&
                   RequestDnsSecRecords == other.RequestDnsSecRecords &&
                   CacheFailedResults == other.CacheFailedResults &&
                   FailedResultsCacheDuration.Equals(other.FailedResultsCacheDuration);
        }
    }

    /// <summary>
    /// The readonly version of <see cref="DnsQueryOptions"/> used to customize settings per query.
    /// </summary>
    public class DnsQueryAndServerSettings : DnsQuerySettings, IEquatable<DnsQueryAndServerSettings>
    {
        private readonly NameServer[] _endpoints;
        private readonly Random _rnd = new();

        /// <summary>
        /// Gets a collection of name servers which should be used to query.
        /// </summary>
        public IReadOnlyList<NameServer> NameServers => _endpoints;


        /// <summary>
        /// Creates a new instance of <see cref="DnsQueryAndServerSettings"/>.
        /// </summary>
        public DnsQueryAndServerSettings(DnsQueryAndServerOptions options)
            : base(options)
        {
            _endpoints = options.NameServers?.ToArray() ?? new NameServer[0];
        }

        /// <summary>
        /// Creates a new instance of <see cref="DnsQueryAndServerSettings"/>.
        /// </summary>
        public DnsQueryAndServerSettings(DnsQueryAndServerOptions options, IReadOnlyCollection<NameServer> overrideServers)
            : this(options)
        {
            _endpoints = overrideServers?.ToArray() ?? throw new ArgumentNullException(nameof(overrideServers));
        }

        /// <inheritdocs />
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return Equals(obj as DnsQueryAndServerSettings);
        }

        /// <inheritdocs />
        public bool Equals(DnsQueryAndServerSettings other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return NameServers.SequenceEqual(other.NameServers)
                   && base.Equals(other);
        }

        internal IReadOnlyList<NameServer> ShuffleNameServers()
        {
            if (_endpoints.Length > 1 && UseRandomNameServer)
            {
                var servers = _endpoints.ToArray();

                for (var i = servers.Length; i > 0; i--)
                {
#pragma warning disable CA5394 // Do not use insecure randomness
                    var j = _rnd.Next(0, i);
#pragma warning restore CA5394 // Do not use insecure randomness
                    var temp = servers[j];
                    servers[j] = servers[i - 1];
                    servers[i - 1] = temp;
                }

                return servers;
            }

            return NameServers;
        }
    }

    /// <summary>
    /// The readonly version of <see cref="LookupClientOptions"/> used as default settings in <see cref="LookupClient"/>.
    /// </summary>
    public class LookupClientSettings : DnsQueryAndServerSettings, IEquatable<LookupClientSettings>
    {
        /// <summary>
        /// Creates a new instance of <see cref="LookupClientSettings"/>.
        /// </summary>
        public LookupClientSettings(LookupClientOptions options)
            : base(options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            MinimumCacheTimeout = options.MinimumCacheTimeout;
            MaximumCacheTimeout = options.MaximumCacheTimeout;
        }

        internal LookupClientSettings(LookupClientOptions options, IReadOnlyCollection<NameServer> overrideServers)
            : base(options, overrideServers)
        {
            MinimumCacheTimeout = options.MinimumCacheTimeout;
            MaximumCacheTimeout = options.MaximumCacheTimeout;
        }

        /// <summary>
        /// Gets a <see cref="TimeSpan"/> which can override the TTL of a resource record in case the
        /// TTL of the record is lower than this minimum value.
        /// Default is <c>Null</c>.
        /// <para>
        /// This is useful in cases where the server returns records with zero TTL.
        /// </para>
        /// </summary>
        /// <remarks>
        /// This setting gets ignored in case <see cref="DnsQueryOptions.UseCache"/> is set to <c>False</c>.
        /// The maximum value is 24 days or <see cref="Timeout.Infinite"/>.
        /// </remarks>
        public TimeSpan? MinimumCacheTimeout { get; }

        /// <summary>
        /// Gets a <see cref="TimeSpan"/> which can override the TTL of a resource record in case the
        /// TTL of the record is higher than this maximum value.
        /// Default is <c>Null</c>.
        /// </summary>
        /// <remarks>
        /// This setting gets ignored in case <see cref="DnsQueryOptions.UseCache"/> is set to <c>False</c>.
        /// The maximum value is 24 days.
        /// Setting it to <see cref="Timeout.Infinite"/> would be equal to not providing a value.
        /// </remarks>
        public TimeSpan? MaximumCacheTimeout { get; }

        /// <inheritdocs />
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return Equals(obj as LookupClientSettings);
        }

        /// <inheritdocs />
        public bool Equals(LookupClientSettings other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(MinimumCacheTimeout, other.MinimumCacheTimeout)
                && Equals(MaximumCacheTimeout, other.MaximumCacheTimeout)
                && base.Equals(other);
        }
    }

#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
}
