﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DnsClient.Tests
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class LookupConfigurationTest
    {
        [Fact]
        public void DnsQueryOptions_Implicit_Null()
        {
            Assert.Null((DnsQuerySettings)(DnsQueryOptions)null);
        }

        [Fact]
        public void DnsQueryAndServerOptions_Implicit_Null()
        {
            Assert.Null((DnsQueryAndServerSettings)(DnsQueryAndServerOptions)null);
        }

        [Fact]
        public void LookupClientOptions_Implicit_Null()
        {
            Assert.Null((LookupClientSettings)(LookupClientOptions)null);
        }

        [Fact]
        public void DnsQuerySettings_Equals_Ref()
        {
            var opt = (DnsQuerySettings)new DnsQueryOptions();

            // typed overload
            Assert.True(opt.Equals(opt));

            // object overload
            Assert.True(opt.Equals((object)opt));
        }

        [Fact]
        public void DnsQuerySettings_Equals_Null()
        {
            var opt = (DnsQuerySettings)new DnsQueryOptions();

            // typed overload
            Assert.False(opt.Equals((DnsQuerySettings)null));

            // object overload
            Assert.False(opt.Equals((object)null));
        }

        [Fact]
        public void DnsQuerySettings_Equals_Other()
        {
            var opt = (DnsQuerySettings)new DnsQueryOptions();
            var opt2 = (DnsQuerySettings)new DnsQueryOptions();

            // typed overload
            Assert.True(opt.Equals(opt2));

            // object overload
            Assert.True(opt.Equals((object)opt2));
        }

        [Fact]
        public void DnsQueryAndServerSettings_Equals_Ref()
        {
            var opt = (DnsQueryAndServerSettings)new DnsQueryAndServerOptions();

            // typed overload
            Assert.True(opt.Equals(opt));

            // object overload
            Assert.True(opt.Equals((object)opt));
        }

        [Fact]
        public void DnsQueryAndServerSettings_Equals_Null()
        {
            var opt = (DnsQueryAndServerSettings)new DnsQueryAndServerOptions();

            // typed overload
            Assert.False(opt.Equals((DnsQueryAndServerSettings)null));

            // object overload
            Assert.False(opt.Equals((object)null));
        }

        [Fact]
        public void DnsQueryAndServerSettings_Equals_Other()
        {
            var opt = (DnsQueryAndServerSettings)new DnsQueryAndServerOptions();
            var opt2 = (DnsQueryAndServerSettings)new DnsQueryAndServerOptions();

            // typed overload
            Assert.True(opt.Equals(opt2));

            // object overload
            Assert.True(opt.Equals((object)opt2));
        }

        [Fact]
        public void LookupClientSettings_Equals_Ref()
        {
            var opt = (LookupClientSettings)new LookupClientOptions(resolveNameServers: false);

            // typed overload
            Assert.True(opt.Equals(opt));

            // object overload
            Assert.True(opt.Equals((object)opt));
        }

        [Fact]
        public void LookupClientSettings_Equals_Null()
        {
            var opt = (LookupClientSettings)new LookupClientOptions(resolveNameServers: false);

            // typed overload
            Assert.False(opt.Equals((LookupClientSettings)null));

            // object overload
            Assert.False(opt.Equals((object)null));
        }

        [Fact]
        public void LookupClientSettings_Equals_Other()
        {
            var opt = (LookupClientSettings)new LookupClientOptions(resolveNameServers: false);
            var opt2 = (LookupClientSettings)new LookupClientOptions(resolveNameServers: false);

            // typed overload
            Assert.True(opt.Equals(opt2));

            // object overload
            Assert.True(opt.Equals((object)opt2));
        }

        public static IEnumerable<object[]> AllNonDefaultConfigurations
        {
            get
            {
                yield return new object[] { new LookupClientOptions(resolveNameServers: false) { ContinueOnDnsError = false } };
                yield return new object[] { new LookupClientOptions(resolveNameServers: false) { EnableAuditTrail = true } };
                yield return new object[] { new LookupClientOptions(resolveNameServers: false) { ExtendedDnsBufferSize = 2222 } };
                yield return new object[] { new LookupClientOptions(resolveNameServers: false) { MaximumCacheTimeout = TimeSpan.FromSeconds(5) } };
                yield return new object[] { new LookupClientOptions(resolveNameServers: false) { MinimumCacheTimeout = TimeSpan.FromSeconds(5) } };
                yield return new object[] { new LookupClientOptions(resolveNameServers: false) { NameServers = new List<NameServer> { NameServer.Cloudflare } } };
                yield return new object[] { new LookupClientOptions(resolveNameServers: false) { Recursion = false } };
                yield return new object[] { new LookupClientOptions(resolveNameServers: false) { RequestDnsSecRecords = true } };
                yield return new object[] { new LookupClientOptions(resolveNameServers: false) { Retries = 3 } };
                yield return new object[] { new LookupClientOptions(resolveNameServers: false) { ThrowDnsErrors = true } };
                yield return new object[] { new LookupClientOptions(resolveNameServers: false) { Timeout = TimeSpan.FromSeconds(1) } };
                yield return new object[] { new LookupClientOptions(resolveNameServers: false) { UseCache = false } };
                yield return new object[] { new LookupClientOptions(resolveNameServers: false) { UseRandomNameServer = false } };
                yield return new object[] { new LookupClientOptions(resolveNameServers: false) { UseTcpFallback = false } };
                yield return new object[] { new LookupClientOptions(resolveNameServers: false) { UseTcpOnly = true } };
                yield return new object[] { new LookupClientOptions(NameServer.CloudflareIPv6) };
                yield return new object[] { new LookupClientOptions(IPAddress.Loopback) };
                yield return new object[] { new LookupClientOptions(new IPEndPoint(IPAddress.Loopback, 1111)) };
                yield return new object[] { new LookupClientOptions(new[] { NameServer.CloudflareIPv6 }) };
            }
        }

        [Theory]
        [MemberData(nameof(AllNonDefaultConfigurations))]
        public void LookupClientSettings_NotEqual(LookupClientOptions otherOptions)
        {
            var opt = (LookupClientSettings)new LookupClientOptions(resolveNameServers: false);
            var opt2 = (LookupClientSettings)otherOptions;

            Assert.NotStrictEqual(opt, opt2);

            // typed overload
            Assert.False(opt.Equals(opt2));

            // object overload
            Assert.False(opt.Equals((object)opt2));
        }

        [Theory]
        [MemberData(nameof(AllNonDefaultConfigurations))]
        public void LookupClientSettings_Equal(LookupClientOptions options)
        {
            var opt = (LookupClientSettings)options;
            var opt2 = (LookupClientSettings)options;

            Assert.StrictEqual(opt, opt2);

            // typed overload
            Assert.True(opt.Equals(opt2));

            // object overload
            Assert.True(opt.Equals((object)opt2));
        }

        [Fact]
        public void LookupClientOptions_Defaults()
        {
            var options = new LookupClientOptions();

            Assert.True(options.NameServers.Count > 0);
            Assert.True(options.UseCache);
            Assert.False(options.EnableAuditTrail);
            Assert.Null(options.MinimumCacheTimeout);
            Assert.Null(options.MaximumCacheTimeout);
            Assert.True(options.Recursion);
            Assert.False(options.ThrowDnsErrors);
            Assert.Equal(5, options.Retries);
            Assert.Equal(options.Timeout, TimeSpan.FromSeconds(5));
            Assert.True(options.UseTcpFallback);
            Assert.False(options.UseTcpOnly);
            Assert.True(options.ContinueOnDnsError);
            Assert.True(options.UseRandomNameServer);
            Assert.Equal(DnsQueryOptions.MaximumBufferSize, options.ExtendedDnsBufferSize);
            Assert.False(options.RequestDnsSecRecords);
        }

        [Fact]
        public void LookupClientOptions_DefaultsNoResolve()
        {
            var options = new LookupClientOptions(resolveNameServers: false);

            Assert.Equal(0, options.NameServers.Count);
            Assert.True(options.UseCache);
            Assert.False(options.EnableAuditTrail);
            Assert.Null(options.MinimumCacheTimeout);
            Assert.Null(options.MaximumCacheTimeout);
            Assert.True(options.Recursion);
            Assert.False(options.ThrowDnsErrors);
            Assert.Equal(5, options.Retries);
            Assert.Equal(options.Timeout, TimeSpan.FromSeconds(5));
            Assert.True(options.UseTcpFallback);
            Assert.False(options.UseTcpOnly);
            Assert.True(options.ContinueOnDnsError);
            Assert.True(options.UseRandomNameServer);
            Assert.Equal(DnsQueryOptions.MaximumBufferSize, options.ExtendedDnsBufferSize);
            Assert.False(options.RequestDnsSecRecords);
        }

        [Fact]
        public void LookupClient_SettingsValid()
        {
            var defaultOptions = new LookupClientOptions(resolveNameServers: true);

            var options = new LookupClientOptions(resolveNameServers: true)
            {
                ContinueOnDnsError = !defaultOptions.ContinueOnDnsError,
                EnableAuditTrail = !defaultOptions.EnableAuditTrail,
                MinimumCacheTimeout = TimeSpan.FromMinutes(1),
                MaximumCacheTimeout = TimeSpan.FromMinutes(42),
                Recursion = !defaultOptions.Recursion,
                Retries = defaultOptions.Retries * 2,
                ThrowDnsErrors = !defaultOptions.ThrowDnsErrors,
                Timeout = TimeSpan.FromMinutes(10),
                UseCache = !defaultOptions.UseCache,
                UseRandomNameServer = !defaultOptions.UseRandomNameServer,
                UseTcpFallback = !defaultOptions.UseTcpFallback,
                UseTcpOnly = !defaultOptions.UseTcpOnly,
                ExtendedDnsBufferSize = 1234,
                RequestDnsSecRecords = true
            };

            var client = new LookupClient(options);

            Assert.Equal(defaultOptions.NameServers, client.NameServers);
            Assert.Equal(!defaultOptions.ContinueOnDnsError, client.Settings.ContinueOnDnsError);
            Assert.Equal(!defaultOptions.EnableAuditTrail, client.Settings.EnableAuditTrail);
            Assert.Equal(TimeSpan.FromMinutes(1), client.Settings.MinimumCacheTimeout);
            Assert.Equal(TimeSpan.FromMinutes(42), client.Settings.MaximumCacheTimeout);
            Assert.Equal(!defaultOptions.Recursion, client.Settings.Recursion);
            Assert.Equal(defaultOptions.Retries * 2, client.Settings.Retries);
            Assert.Equal(!defaultOptions.ThrowDnsErrors, client.Settings.ThrowDnsErrors);
            Assert.Equal(TimeSpan.FromMinutes(10), client.Settings.Timeout);
            Assert.Equal(!defaultOptions.UseCache, client.Settings.UseCache);
            Assert.Equal(!defaultOptions.UseRandomNameServer, client.Settings.UseRandomNameServer);
            Assert.Equal(!defaultOptions.UseTcpFallback, client.Settings.UseTcpFallback);
            Assert.Equal(!defaultOptions.UseTcpOnly, client.Settings.UseTcpOnly);
            Assert.Equal(1234, client.Settings.ExtendedDnsBufferSize);
            Assert.Equal(!defaultOptions.RequestDnsSecRecords, client.Settings.RequestDnsSecRecords);

            Assert.Equal(options, client.Settings);
        }

        [Fact]
        public void QueryOptions_EdnsDisabled_WithSmallBuffer()
        {
            var options = (DnsQuerySettings)new DnsQueryOptions()
            {
                ExtendedDnsBufferSize = DnsQueryOptions.MinimumBufferSize,
                RequestDnsSecRecords = false
            };

            Assert.False(options.UseExtendedDns);
        }

        [Fact]
        public void QueryOptions_Edns_SmallerBufferFallback()
        {
            var options = (DnsQuerySettings)new DnsQueryOptions()
            {
                // Anything less then mimimum falls back to minimum.
                ExtendedDnsBufferSize = DnsQueryOptions.MinimumBufferSize - 1,
                RequestDnsSecRecords = false
            };

            Assert.False(options.UseExtendedDns);
            Assert.Equal(DnsQueryOptions.MinimumBufferSize, options.ExtendedDnsBufferSize);
        }

        [Fact]
        public void QueryOptions_Edns_LargerBufferFallback()
        {
            var options = (DnsQuerySettings)new DnsQueryOptions()
            {
                // Anything more then max falls back to max.
                ExtendedDnsBufferSize = DnsQueryOptions.MaximumBufferSize + 1,
                RequestDnsSecRecords = false
            };

            Assert.True(options.UseExtendedDns);
            Assert.Equal(DnsQueryOptions.MaximumBufferSize, options.ExtendedDnsBufferSize);
        }

        [Fact]
        public void QueryOptions_EdnsEnabled_ByLargeBuffer()
        {
            var options = (DnsQuerySettings)new DnsQueryOptions()
            {
                ExtendedDnsBufferSize = DnsQueryOptions.MinimumBufferSize + 1,
                RequestDnsSecRecords = false
            };

            Assert.True(options.UseExtendedDns);
        }

        [Fact]
        public void QueryOptions_EdnsEnabled_ByRequestDnsSec()
        {
            var options = (DnsQuerySettings)new DnsQueryOptions()
            {
                ExtendedDnsBufferSize = DnsQueryOptions.MinimumBufferSize,
                RequestDnsSecRecords = true
            };

            Assert.True(options.UseExtendedDns);
        }

        [Fact]
        public void LookupClientOptions_InvalidTimeout()
        {
            var options = new LookupClientOptions();

            Action act = () => options.Timeout = TimeSpan.FromMilliseconds(0);

            Assert.ThrowsAny<ArgumentOutOfRangeException>(act);
        }

        [Fact]
        public void LookupClientOptions_InvalidTimeout2()
        {
            var options = new LookupClientOptions();

            Action act = () => options.Timeout = TimeSpan.FromMilliseconds(-23);

            Assert.ThrowsAny<ArgumentOutOfRangeException>(act);
        }

        [Fact]
        public void LookupClientOptions_InvalidTimeout3()
        {
            var options = new LookupClientOptions();

            Action act = () => options.Timeout = TimeSpan.FromDays(25);

            Assert.ThrowsAny<ArgumentOutOfRangeException>(act);
        }

        [Fact]
        public void LookupClientOptions_MinimumCacheTimeout_ZeroIgnored()
        {
            var options = new LookupClientOptions();

            options.MinimumCacheTimeout = TimeSpan.Zero;

            Assert.Null(options.MinimumCacheTimeout);
        }

        [Fact]
        public void LookupClientOptions_MinimumCacheTimeout_AcceptsInfinite()
        {
            var options = new LookupClientOptions()
            {
                MinimumCacheTimeout = TimeSpan.Zero
            };

            options.MinimumCacheTimeout = Timeout.InfiniteTimeSpan;

            Assert.Equal(Timeout.InfiniteTimeSpan, options.MinimumCacheTimeout);
        }

        [Fact]
        public void LookupClientOptions_MinimumCacheTimeout_AcceptsNull()
        {
            var options = new LookupClientOptions()
            {
                MinimumCacheTimeout = Timeout.InfiniteTimeSpan
            };

            options.MinimumCacheTimeout = null;

            Assert.Null(options.MinimumCacheTimeout);
        }

        [Fact]
        public void LookupClientOptions_InvalidMinimumCacheTimeout1()
        {
            var options = new LookupClientOptions();

            Action act = () => options.MinimumCacheTimeout = TimeSpan.FromMilliseconds(-23);

            Assert.ThrowsAny<ArgumentOutOfRangeException>(act);
        }

        [Fact]
        public void LookupClientOptions_InvalidMinimumCacheTimeout2()
        {
            var options = new LookupClientOptions();

            Action act = () => options.MinimumCacheTimeout = TimeSpan.FromDays(25);

            Assert.ThrowsAny<ArgumentOutOfRangeException>(act);
        }

        [Fact]
        public void LookupClientOptions_MaximumCacheTimeout_ZeroIgnored()
        {
            var options = new LookupClientOptions();

            options.MaximumCacheTimeout = TimeSpan.Zero;

            Assert.Null(options.MaximumCacheTimeout);
        }

        [Fact]
        public void LookupClientOptions_MaximumCacheTimeout_AcceptsNull()
        {
            var options = new LookupClientOptions()
            {
                MaximumCacheTimeout = Timeout.InfiniteTimeSpan
            };

            options.MaximumCacheTimeout = null;

            Assert.Null(options.MaximumCacheTimeout);
        }

        [Fact]
        public void LookupClientOptions_MaximumCacheTimeout_AcceptsInfinite()
        {
            var options = new LookupClientOptions();

            options.MaximumCacheTimeout = Timeout.InfiniteTimeSpan;

            Assert.Equal(Timeout.InfiniteTimeSpan, options.MaximumCacheTimeout);
        }

        [Fact]
        public void LookupClientOptions_InvalidMaximumCacheTimeout1()
        {
            var options = new LookupClientOptions();

            Action act = () => options.MaximumCacheTimeout = TimeSpan.FromMilliseconds(-23);

            Assert.ThrowsAny<ArgumentOutOfRangeException>(act);
        }

        [Fact]
        public void LookupClientOptions_InvalidMaximumCacheTimeout2()
        {
            var options = new LookupClientOptions();

            Action act = () => options.MaximumCacheTimeout = TimeSpan.FromDays(25);

            Assert.ThrowsAny<ArgumentOutOfRangeException>(act);
        }

        public static IEnumerable<object[]> All
        {
            get
            {
                // question doesn't matter
                var question = new DnsQuestion("something.com", QueryType.A);

                // standard
                yield return new object[] { new TestMatrixItem("Query(q)", (client) => client.Query(question)) };
                yield return new object[] { new TestMatrixItem("Query(n,t,c)", (client) => client.Query(question.QueryName, question.QuestionType, question.QuestionClass)) };
                yield return new object[] { new TestMatrixItem("QueryAsync(q)", (client) => client.QueryAsync(question).GetAwaiter().GetResult()) };
                yield return new object[] { new TestMatrixItem("QueryAsync(n,t,c)", (client) => client.QueryAsync(question.QueryName, question.QuestionType, question.QuestionClass).GetAwaiter().GetResult()) };
                yield return new object[] { new TestMatrixItem("QueryReverse(ip)", (client) => client.QueryReverse(IPAddress.Any)) };
                yield return new object[] { new TestMatrixItem("QueryReverseAsync(ip)", (client) => client.QueryReverseAsync(IPAddress.Any).GetAwaiter().GetResult()) };

                // by server
                yield return new object[] { new TestMatrixItem("QueryServer(s,n,t,c)", (client, servers) => client.QueryServer(servers, question.QueryName, question.QuestionType, question.QuestionClass)) };
                yield return new object[] { new TestMatrixItem("QueryServerAsync(s,n,t,c)", (client, servers) => client.QueryServerAsync(servers, question.QueryName, question.QuestionType, question.QuestionClass).GetAwaiter().GetResult()) };
                yield return new object[] { new TestMatrixItem("QueryServerReverse(s,ip)", (client, servers) => client.QueryServerReverse(servers, IPAddress.Any)) };
                yield return new object[] { new TestMatrixItem("QueryServerReverseAsync(s,ip)", (client, servers) => client.QueryServerReverseAsync(servers, IPAddress.Any).GetAwaiter().GetResult()) };

                // with query options
                yield return new object[] { new TestMatrixItem("Query(q,o)", (client, options) => client.Query(question, options)) };
                yield return new object[] { new TestMatrixItem("Query(n,t,o,c)", (client, options) => client.Query(question.QueryName, question.QuestionType, options, question.QuestionClass)) };
                yield return new object[] { new TestMatrixItem("QueryAsync(q,o)", (client, options) => client.QueryAsync(question, options).GetAwaiter().GetResult()) };
                yield return new object[] { new TestMatrixItem("QueryAsync(n,t,o,c)", (client, options) => client.QueryAsync(question.QueryName, question.QuestionType, options, question.QuestionClass).GetAwaiter().GetResult()) };
                yield return new object[] { new TestMatrixItem("QueryReverse(ip,o)", (client, options) => client.QueryReverse(IPAddress.Any, options)) };
                yield return new object[] { new TestMatrixItem("QueryReverseAsync(ip,o)", (client, options) => client.QueryReverseAsync(IPAddress.Any, options).GetAwaiter().GetResult()) };
            }
        }

        public static IEnumerable<object[]> AllWithoutServerQueries
            => All.Where(p => !p.Any(a => a is TestMatrixItem m && m.UsesServers));

        public static IEnumerable<object[]> AllWithServers
            => All.Where(p => p.Any(a => a is TestMatrixItem m && m.UsesServers));

        public static IEnumerable<object[]> AllWithQueryOptions
            => All.Where(p => p.Any(a => a is TestMatrixItem m && m.UsesQueryOptions));

        public static IEnumerable<object[]> AllWithoutQueryOptionsOrServerQueries
            => All.Where(p => !p.Any(a => a is TestMatrixItem m && (m.UsesQueryOptions || m.UsesServers)));

        [Theory]
        [MemberData(nameof(AllWithoutServerQueries))]
        public void ConfigMatrix_NoServersConfiguredThrows(TestMatrixItem test)
        {
            var unresolvedOptions = new LookupClientOptions(resolveNameServers: false);
            Assert.Throws<ArgumentOutOfRangeException>("servers", () => test.Invoke(lookupClientOptions: unresolvedOptions));
        }

        [Theory]
        [MemberData(nameof(AllWithServers))]
        public void ConfigMatrix_ServersQueriesExpectServers(TestMatrixItem test)
        {
            var unresolvedOptions = new LookupClientOptions(resolveNameServers: false);
            Assert.Throws<ArgumentOutOfRangeException>("servers", () => test.Invoke(lookupClientOptions: unresolvedOptions, useServers: new NameServer[0]));
        }

        [Theory]
        [MemberData(nameof(AllWithServers))]
        public void ConfigMatrix_ServersCannotBeNull(TestMatrixItem test)
        {
            var unresolvedOptions = new LookupClientOptions(resolveNameServers: true);
            Assert.Throws<ArgumentNullException>("servers", () => test.InvokeNoDefaults(lookupClientOptions: unresolvedOptions, useOptions: null, useServers: null));
        }

        [Theory]
        [MemberData(nameof(All))]
        public void ConfigMatrix_ValidSettingsResponse(TestMatrixItem test)
        {
            var unresolvedOptions = new LookupClientOptions(NameServer.GooglePublicDns);
            var queryOptions = new DnsQueryAndServerOptions(NameServer.GooglePublicDns2);
            var servers = new NameServer[] { NameServer.GooglePublicDns2IPv6 };

            var result = test.Invoke(lookupClientOptions: unresolvedOptions, useOptions: queryOptions, useServers: servers);

            Assert.Null(result.TestClient.TcpHandler.LastRequest);
            Assert.NotNull(result.TestClient.UdpHandler.LastRequest);
            Assert.StrictEqual(new LookupClientSettings(unresolvedOptions), result.TestClient.Client.Settings);

            if (test.UsesQueryOptions)
            {
                Assert.Equal(NameServer.GooglePublicDns2, result.TestClient.UdpHandler.LastServer);
                Assert.Equal(NameServer.GooglePublicDns2, result.Response.NameServer);
                Assert.Equal(new DnsQueryAndServerSettings(queryOptions), result.Response.Settings);
            }
            else if (test.UsesServers)
            {
                Assert.Equal(NameServer.GooglePublicDns2IPv6, result.TestClient.UdpHandler.LastServer);
                Assert.Equal(NameServer.GooglePublicDns2IPv6, result.Response.NameServer);
                // by server overrules settings, but doesn't change the servers collection in settings..
                Assert.Equal(new DnsQueryAndServerSettings(unresolvedOptions), result.Response.Settings);
            }
            else
            {
                Assert.Equal(NameServer.GooglePublicDns, result.TestClient.UdpHandler.LastServer);
                Assert.Equal(NameServer.GooglePublicDns, result.Response.NameServer);
                Assert.Equal(new DnsQueryAndServerSettings(unresolvedOptions), result.Response.Settings);
            }
        }

        [Theory]
        [MemberData(nameof(AllWithQueryOptions))]
        public void ConfigMatrix_VerifyOverride(TestMatrixItem test)
        {
            var defaultOptions = new LookupClientOptions(resolveNameServers: false);
            var queryOptions = new DnsQueryAndServerOptions(NameServer.GooglePublicDns2IPv6)
            {
                ContinueOnDnsError = !defaultOptions.ContinueOnDnsError,
                EnableAuditTrail = !defaultOptions.EnableAuditTrail,
                Recursion = !defaultOptions.Recursion,
                Retries = defaultOptions.Retries * 2,
                ThrowDnsErrors = !defaultOptions.ThrowDnsErrors,
                Timeout = TimeSpan.FromMinutes(10),
                UseCache = !defaultOptions.UseCache,
                UseRandomNameServer = !defaultOptions.UseRandomNameServer,
                UseTcpFallback = !defaultOptions.UseTcpFallback,
                UseTcpOnly = !defaultOptions.UseTcpOnly
            };

            var result = test.Invoke(lookupClientOptions: defaultOptions, useOptions: queryOptions);

            Assert.Null(result.TestClient.UdpHandler.LastRequest);
            Assert.Equal(NameServer.GooglePublicDns2IPv6, result.TestClient.TcpHandler.LastServer);
            Assert.Equal(NameServer.GooglePublicDns2IPv6, result.Response.NameServer);
            Assert.Equal(new LookupClientSettings(defaultOptions), result.TestClient.Client.Settings);
            Assert.Equal(new DnsQuerySettings(queryOptions), result.Response.Settings);
        }

        [Theory]
        [MemberData(nameof(AllWithQueryOptions))]
        public void ConfigMatrix_VerifyOverrideWithServerFallback(TestMatrixItem test)
        {
            var defaultOptions = new LookupClientOptions(NameServer.GooglePublicDns2IPv6);
            var queryOptions = new DnsQueryAndServerOptions(resolveNameServers: false)
            {
                ContinueOnDnsError = !defaultOptions.ContinueOnDnsError,
                EnableAuditTrail = !defaultOptions.EnableAuditTrail,
                Recursion = !defaultOptions.Recursion,
                Retries = defaultOptions.Retries * 2,
                ThrowDnsErrors = !defaultOptions.ThrowDnsErrors,
                Timeout = TimeSpan.FromMinutes(10),
                UseCache = !defaultOptions.UseCache,
                UseRandomNameServer = !defaultOptions.UseRandomNameServer,
                UseTcpFallback = !defaultOptions.UseTcpFallback,
                UseTcpOnly = !defaultOptions.UseTcpOnly,
                RequestDnsSecRecords = true,
                ExtendedDnsBufferSize = 3333
            };

            var result = test.Invoke(lookupClientOptions: defaultOptions, useOptions: queryOptions);

            // verify that override settings also control cache
            var cacheKey = ResponseCache.GetCacheKey(result.TestClient.TcpHandler.LastRequest.Question);
            Assert.Null(result.TestClient.Client.Cache.Get(cacheKey));

            Assert.Equal(NameServer.GooglePublicDns2IPv6, result.TestClient.TcpHandler.LastServer);
            Assert.Equal(NameServer.GooglePublicDns2IPv6, result.Response.NameServer);
            // make sure we don't alter the original object
            Assert.Empty(queryOptions.NameServers);
            Assert.Equal(new DnsQuerySettings(queryOptions), result.Response.Settings);
        }

        [Theory]
        [MemberData(nameof(AllWithQueryOptions))]
        public void ConfigMatrix_VerifyCacheUsed(TestMatrixItem test)
        {
            var defaultOptions = new LookupClientOptions(NameServer.GooglePublicDns2IPv6)
            {
                UseCache = false
            };
            var queryOptions = new DnsQueryAndServerOptions(resolveNameServers: false)
            {
                UseCache = true
            };

            var result = test.Invoke(lookupClientOptions: defaultOptions, useOptions: queryOptions);

            // verify that override settings also control cache
            var cacheKey = ResponseCache.GetCacheKey(result.TestClient.UdpHandler.LastRequest.Question);
            Assert.NotNull(result.TestClient.Client.Cache.Get(cacheKey));
        }

        [Theory]
        [MemberData(nameof(All))]
        public void ConfigMatrix_VerifyCacheNotUsed(TestMatrixItem test)
        {
            var defaultOptions = new LookupClientOptions(NameServer.GooglePublicDns2IPv6)
            {
                UseCache = false,
                MinimumCacheTimeout = TimeSpan.FromMilliseconds(1000)
            };
            var queryOptions = new DnsQueryAndServerOptions(resolveNameServers: false)
            {
                UseCache = false
            };

            var result = test.Invoke(lookupClientOptions: defaultOptions, useOptions: queryOptions);

            // verify that override settings also control cache
            var cacheKey = ResponseCache.GetCacheKey(result.TestClient.UdpHandler.LastRequest.Question);
            Assert.Null(result.TestClient.Client.Cache.Get(cacheKey));
        }

        public class TestMatrixItem
        {
            public bool UsesServers { get; }

            public Func<ILookupClient, IDnsQueryResponse> ResolverSimple { get; }

            public Func<ILookupClient, IReadOnlyCollection<NameServer>, IDnsQueryResponse> ResolverServers { get; }

            public string Name { get; }

            public Func<ILookupClient, DnsQueryAndServerOptions, IDnsQueryResponse> ResolverQueryOptions { get; }

            public bool UsesQueryOptions { get; }

            private TestMatrixItem(string name)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }

            public TestMatrixItem(string name, Func<ILookupClient, DnsQueryAndServerOptions, IDnsQueryResponse> resolver)
                : this(name)
            {
                ResolverQueryOptions = resolver;
                UsesQueryOptions = true;
                UsesServers = false;
            }

            public TestMatrixItem(string name, Func<ILookupClient, IReadOnlyCollection<NameServer>, IDnsQueryResponse> resolver)
                : this(name)
            {
                ResolverServers = resolver;
                UsesQueryOptions = false;
                UsesServers = true;
            }

            public TestMatrixItem(string name, Func<ILookupClient, IDnsQueryResponse> resolver)
                : this(name)
            {
                ResolverSimple = resolver;
                UsesQueryOptions = false;
                UsesServers = false;
            }

            public TestResponse Invoke(LookupClientOptions lookupClientOptions = null, DnsQueryAndServerOptions useOptions = null, IReadOnlyCollection<NameServer> useServers = null)
            {
                var testClient = new TestClient(lookupClientOptions);
                var servers = useServers ?? new NameServer[] { IPAddress.Loopback };
                var queryOptions = useOptions ?? new DnsQueryAndServerOptions();

                IDnsQueryResponse response = null;
                if (ResolverServers != null)
                {
                    response = ResolverServers(testClient.Client, servers);
                }
                else if (ResolverQueryOptions != null)
                {
                    response = ResolverQueryOptions(testClient.Client, queryOptions);
                }
                else
                {
                    response = ResolverSimple(testClient.Client);
                }

                return new TestResponse(testClient, response);
            }

            public TestResponse InvokeNoDefaults(LookupClientOptions lookupClientOptions, DnsQueryAndServerOptions useOptions, IReadOnlyCollection<NameServer> useServers)
            {
                var testClient = new TestClient(lookupClientOptions);

                IDnsQueryResponse response = null;
                if (ResolverServers != null)
                {
                    response = ResolverServers(testClient.Client, useServers);
                }
                else if (ResolverQueryOptions != null)
                {
                    response = ResolverQueryOptions(testClient.Client, useOptions);
                }
                else
                {
                    response = ResolverSimple(testClient.Client);
                }

                return new TestResponse(testClient, response);
            }

            public override string ToString()
            {
                return $"{Name} => s:{UsesServers} q:{UsesQueryOptions}";
            }
        }

        public class TestResponse
        {
            public TestResponse(TestClient testClient, IDnsQueryResponse response)
            {
                TestClient = testClient;
                Response = response;
            }

            public TestClient TestClient { get; }

            public IDnsQueryResponse Response { get; }
        }

        public class TestClient
        {
            public TestClient(LookupClientOptions options)
            {
                UdpHandler = new ConfigurationTrackingMessageHandler(false);
                TcpHandler = new ConfigurationTrackingMessageHandler(true);
                Client = new LookupClient(options, UdpHandler, TcpHandler);
            }

            internal ConfigurationTrackingMessageHandler UdpHandler { get; }

            internal ConfigurationTrackingMessageHandler TcpHandler { get; }

            public LookupClient Client { get; }
        }

        internal class ConfigurationTrackingMessageHandler : DnsMessageHandler
        {
            // raw bytes from mcnet.com
            private static readonly byte[] ZoneData = new byte[]
            {
               95, 207, 129, 128, 0, 1, 0, 11, 0, 0, 0, 1, 6, 103, 111, 111, 103, 108, 101, 3, 99, 111, 109, 0, 0, 255, 0, 1, 192, 12, 0, 1, 0, 1, 0, 0, 1, 8, 0, 4, 172, 217, 17, 238, 192, 12, 0, 28, 0, 1, 0, 0, 0, 71, 0, 16, 42, 0, 20, 80, 64, 22, 8, 13, 0, 0, 0, 0, 0, 0, 32, 14, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 17, 0, 50, 4, 97, 108, 116, 52, 5, 97, 115, 112, 109, 120, 1, 108, 192, 12, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 4, 0, 10, 192, 91, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 9, 0, 30, 4, 97, 108, 116, 50, 192, 91, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 9, 0, 20, 4, 97, 108, 116, 49, 192, 91, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 9, 0, 40, 4, 97, 108, 116, 51, 192, 91, 192, 12, 0, 2, 0, 1, 0, 4, 31, 116, 0, 6, 3, 110, 115, 51, 192, 12, 192, 12, 0, 2, 0, 1, 0, 4, 31, 116, 0, 6, 3, 110, 115, 50, 192, 12, 192, 12, 0, 2, 0, 1, 0, 4, 31, 116, 0, 6, 3, 110, 115, 52, 192, 12, 192, 12, 0, 2, 0, 1, 0, 4, 31, 116, 0, 6, 3, 110, 115, 49, 192, 12, 0, 0, 41, 16, 0, 0, 0, 0, 0, 0, 0
            };

            public IReadOnlyList<DnsQueryAndServerSettings> UsedSettings { get; }

            public bool IsTcp { get; }

            public IPEndPoint LastServer { get; private set; }

            public DnsRequestMessage LastRequest { get; private set; }

            public ConfigurationTrackingMessageHandler(bool isTcp)
            {
                IsTcp = isTcp;
            }

            public override bool IsTransientException<T>(T exception)
            {
                return false;
            }

            public override DnsResponseMessage Query(
                IPEndPoint server,
                DnsRequestMessage request,
                TimeSpan timeout)
            {
                LastServer = server;
                LastRequest = request;
                var response = GetResponseMessage(new ArraySegment<byte>(ZoneData, 0, ZoneData.Length));

                return response;
            }

            public override Task<DnsResponseMessage> QueryAsync(
                IPEndPoint server,
                DnsRequestMessage request,
                CancellationToken cancellationToken,
                Action<Action> cancelationCallback)
            {
                LastServer = server;
                LastRequest = request;
                // no need to run async here as we don't do any IO
                return Task.FromResult(Query(server, request, Timeout.InfiniteTimeSpan));
            }
        }
    }
}