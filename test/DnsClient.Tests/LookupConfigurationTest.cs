using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DnsClient.Tests
{
    [ExcludeFromCodeCoverage]
    public class LookupConfigurationTest
    {
        [Fact]
        public void LookupClientOptions_Defaults()
        {
            var options = new LookupClientOptions();

            Assert.True(options.NameServers.Count > 0);
            Assert.True(options.UseCache);
            Assert.False(options.EnableAuditTrail);
            Assert.Null(options.MinimumCacheTimeout);
            Assert.True(options.Recursion);
            Assert.False(options.ThrowDnsErrors);
            Assert.Equal(5, options.Retries);
            Assert.Equal(options.Timeout, TimeSpan.FromSeconds(5));
            Assert.True(options.UseTcpFallback);
            Assert.False(options.UseTcpOnly);
            Assert.True(options.ContinueOnDnsError);
            Assert.True(options.UseRandomNameServer);
        }

        [Fact]
        public void LookupClientOptions_DefaultsNoResolve()
        {
            var options = new LookupClientOptions(resolveNameServers: false);

            Assert.Equal(0, options.NameServers.Count);
            Assert.True(options.UseCache);
            Assert.False(options.EnableAuditTrail);
            Assert.Null(options.MinimumCacheTimeout);
            Assert.True(options.Recursion);
            Assert.False(options.ThrowDnsErrors);
            Assert.Equal(5, options.Retries);
            Assert.Equal(options.Timeout, TimeSpan.FromSeconds(5));
            Assert.True(options.UseTcpFallback);
            Assert.False(options.UseTcpOnly);
            Assert.True(options.ContinueOnDnsError);
            Assert.True(options.UseRandomNameServer);
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
                Recursion = !defaultOptions.Recursion,
                Retries = defaultOptions.Retries * 2,
                ThrowDnsErrors = !defaultOptions.ThrowDnsErrors,
                Timeout = TimeSpan.FromMinutes(10),
                UseCache = !defaultOptions.UseCache,
                UseRandomNameServer = !defaultOptions.UseRandomNameServer,
                UseTcpFallback = !defaultOptions.UseTcpFallback,
                UseTcpOnly = !defaultOptions.UseTcpOnly
            };

            var client = new LookupClient(options);

            Assert.Equal(defaultOptions.NameServers, client.NameServers);
            Assert.Equal(!defaultOptions.ContinueOnDnsError, client.Settings.ContinueOnDnsError);
            Assert.Equal(!defaultOptions.EnableAuditTrail, client.Settings.EnableAuditTrail);
            Assert.Equal(TimeSpan.FromMinutes(1), client.Settings.MinimumCacheTimeout);
            Assert.Equal(!defaultOptions.Recursion, client.Settings.Recursion);
            Assert.Equal(defaultOptions.Retries * 2, client.Settings.Retries);
            Assert.Equal(!defaultOptions.ThrowDnsErrors, client.Settings.ThrowDnsErrors);
            Assert.Equal(TimeSpan.FromMinutes(10), client.Settings.Timeout);
            Assert.Equal(!defaultOptions.UseCache, client.Settings.UseCache);
            Assert.Equal(!defaultOptions.UseRandomNameServer, client.Settings.UseRandomNameServer);
            Assert.Equal(!defaultOptions.UseTcpFallback, client.Settings.UseTcpFallback);
            Assert.Equal(!defaultOptions.UseTcpOnly, client.Settings.UseTcpOnly);
        }

        [Fact]
        public void Lookup_Query_InvalidTimeout()
        {
            var options = new LookupClientOptions();

            Action act = () => options.Timeout = TimeSpan.FromMilliseconds(0);

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
                yield return new object[] { new TestMatrixItem("Query(n,t,c,o)", (client, options) => client.Query(question.QueryName, question.QuestionType, question.QuestionClass, options)) };
                yield return new object[] { new TestMatrixItem("QueryAsync(q,o)", (client, options) => client.QueryAsync(question, options).GetAwaiter().GetResult()) };
                yield return new object[] { new TestMatrixItem("QueryAsync(n,t,c,o)", (client, options) => client.QueryAsync(question.QueryName, question.QuestionType, question.QuestionClass, options).GetAwaiter().GetResult()) };
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
        [MemberData(nameof(All))]
        public void ConfigMatrix_ValidSettingsResponse(TestMatrixItem test)
        {
            var unresolvedOptions = new LookupClientOptions(NameServer.GooglePublicDns);
            var queryOptions = new DnsQueryOptions(NameServer.GooglePublicDns2);
            var servers = new NameServer[] { NameServer.GooglePublicDns2IPv6 };

            var result = test.Invoke(lookupClientOptions: unresolvedOptions, useOptions: queryOptions, useServers: servers);

            Assert.Null(result.TestClient.TcpHandler.LastRequest);
            Assert.NotNull(result.TestClient.UdpHandler.LastRequest);

            if (test.UsesQueryOptions)
            {
                Assert.Equal(NameServer.GooglePublicDns2, result.TestClient.UdpHandler.LastServer);
                Assert.Equal(NameServer.GooglePublicDns2, result.Response.NameServer);
                Assert.Equal(new[] { NameServer.GooglePublicDns2 }, result.Response.Settings.NameServers);
                Assert.Equal(new DnsQuerySettings(queryOptions), result.Response.Settings);
            }
            else if (test.UsesServers)
            {
                Assert.Equal(NameServer.GooglePublicDns2IPv6, result.TestClient.UdpHandler.LastServer);
                Assert.Equal(NameServer.GooglePublicDns2IPv6, result.Response.NameServer);
                // by server overrules settings, but doesn't change the servers collection in settings..
                Assert.Equal(new[] { NameServer.GooglePublicDns }, result.Response.Settings.NameServers);
                Assert.Equal(new DnsQuerySettings(unresolvedOptions), result.Response.Settings);
            }
            else
            {
                Assert.Equal(NameServer.GooglePublicDns, result.TestClient.UdpHandler.LastServer);
                Assert.Equal(NameServer.GooglePublicDns, result.Response.NameServer);
                Assert.Equal(new[] { NameServer.GooglePublicDns }, result.Response.Settings.NameServers);
                Assert.Equal(new DnsQuerySettings(unresolvedOptions), result.Response.Settings);
            }
        }

        public class TestMatrixItem
        {
            public bool UsesServers { get; }

            public Func<ILookupClient, IDnsQueryResponse> ResolverSimple { get; }

            public Func<ILookupClient, IReadOnlyCollection<NameServer>, IDnsQueryResponse> ResolverServers { get; }

            public string Name { get; }

            public Func<ILookupClient, DnsQueryOptions, IDnsQueryResponse> ResolverQueryOptions { get; }

            public bool UsesQueryOptions { get; }

            private TestMatrixItem(string name)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }

            public TestMatrixItem(string name, Func<ILookupClient, DnsQueryOptions, IDnsQueryResponse> resolver)
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

            public TestMatrixItem(string name, Func<ILookupClient, IDnsQueryResponse> resolver, bool usesQueryOptions = false, bool usesServers = false)
                : this(name)
            {
                ResolverSimple = resolver;
                UsesQueryOptions = usesQueryOptions;
                UsesServers = usesServers;
            }

            public TestResponse Invoke(LookupClientOptions lookupClientOptions = null, DnsQueryOptions useOptions = null, IReadOnlyCollection<NameServer> useServers = null)
            {
                var testClient = new TestClient(lookupClientOptions);
                var servers = useServers ?? new NameServer[] { IPAddress.Loopback };
                var queryOptions = useOptions ?? new DnsQueryOptions();

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
            private const int MaxSize = 4096;

            // raw bytes from mcnet.com
            private static readonly byte[] ZoneData = new byte[]
            {
                92,159,132,128,0,1,0,65,0,0,0,1,5,109,99,110,101,116,3,99,111,109,0,0,252,0,1,192,12,0,6,0,1,0,0,0,100,0,39,3,110,115,49,192,12,10,104,111,115,116,109,97,115,116,101,114,192,12,120,57,35,168,0,9,58,128,0,1,81,128,0,36,234,0,0,9,58,128,192,12,0,16,0,1,0,0,0,100,0,68,4,109,111,114,101,11,40,116,101,120,116,32,119,105,116,104,41,17,34,115,112,101,99,105,97,108,34,32,195,182,195,164,195,156,33,6,194,167,36,37,92,47,6,64,115,116,117,102,102,4,59,97,110,100,13,59,102,97,107,101,32,99,111,109,109,101,110,116,192,12,0,16,0,1,0,0,0,100,0,53,4,115,111,109,101,4,116,101,120,116,12,115,101,112,97,114,97,116,101,100,32,98,121,5,115,112,97,99,101,4,119,105,116,104,3,110,101,119,4,108,105,110,101,5,115,116,117,102,102,3,116,111,111,192,12,0,16,0,1,0,0,0,100,0,76,7,97,110,111,116,104,101,114,4,109,111,114,101,11,40,116,101,120,116,32,119,105,116,104,41,17,34,115,112,101,99,105,97,108,34,32,195,182,195,164,195,156,33,6,194,167,36,37,92,47,6,64,115,116,117,102,102,4,59,97,110,100,13,59,102,97,107,101,32,99,111,109,109,101,110,116,192,12,0,16,0,1,0,0,0,100,0,49,48,115,111,109,101,32,108,111,110,103,32,116,101,120,116,32,119,105,116,104,32,115,111,109,101,32,115,116,117,102,102,32,105,110,32,105,116,32,123,108,97,108,97,58,98,108,117,98,125,192,12,0,7,0,1,0,0,0,100,0,6,3,115,114,118,192,12,192,12,0,7,0,1,0,0,0,100,0,62,15,108,195,156,195,164,39,108,97,195,188,195,182,35,50,120,22,88,78,45,45,67,76,67,72,67,48,69,65,48,66,50,71,50,65,57,71,67,68,11,88,78,45,45,48,90,87,77,53,54,68,5,109,99,110,101,116,3,99,111,109,0,192,12,0,8,0,1,0,0,0,100,0,9,6,104,105,100,100,101,110,192,12,192,12,0,9,0,1,0,0,0,100,0,10,7,104,105,100,100,101,110,50,192,12,192,12,0,9,0,1,0,0,0,100,0,60,10,120,110,45,45,52,103,98,114,105,109,26,120,110,45,45,45,45,121,109,99,98,97,97,97,106,108,99,54,100,106,55,98,120,110,101,50,99,10,120,110,45,45,119,103,98,104,49,99,5,109,99,110,101,116,3,99,111,109,0,192,12,0,33,0,1,0,0,0,100,0,22,0,1,0,1,31,144,4,115,114,118,52,5,109,99,110,101,116,3,99,111,109,0,194,90,0,12,0,1,0,0,1,244,0,2,194,90,194,90,0,12,0,1,0,0,1,244,0,2,193,220,194,90,0,14,0,1,0,0,0,100,0,4,193,220,193,241,194,90,1,1,0,1,0,0,1,244,0,31,1,6,112,111,108,105,99,121,49,46,51,46,54,46,49,46,52,46,49,46,51,53,52,48,53,46,54,54,54,46,49,194,90,1,1,0,1,0,0,1,244,0,77,129,3,116,98,115,77,68,73,71,65,49,85,69,74,81,89,74,89,73,90,73,65,87,85,68,66,65,73,66,66,67,65,88,122,74,103,80,97,111,84,55,70,101,88,97,80,122,75,118,54,109,73,50,68,48,121,105,108,105,102,43,55,87,104,122,109,104,77,71,76,101,47,111,66,65,61,61,194,90,1,1,0,1,0,0,1,244,0,54,255,16,115,111,109,101,116,104,105,110,103,115,116,114,97,110,103,101,84,104,101,118,97,108,117,101,83,116,105,110,103,119,105,116,104,195,156,98,101,114,83,112,101,99,105,195,182,108,118,97,108,117,101,46,194,90,1,0,0,1,0,0,1,94,0,30,0,10,0,1,102,116,112,58,47,47,115,114,118,46,109,99,110,101,116,46,99,111,109,47,112,117,98,108,105,99,194,90,0,10,0,1,0,0,1,94,0,117,101,109,115,48,49,46,121,111,117,114,45,102,114,101,101,100,111,109,46,100,101,59,85,83,59,49,57,56,46,50,53,53,46,51,48,46,50,52,50,59,48,59,53,55,51,48,59,100,101,102,97,117,108,116,44,118,111,108,117,109,101,44,110,111,114,116,104,97,109,101,114,105,99,97,44,105,110,116,101,114,97,99,116,105,118,101,44,118,111,105,112,44,111,112,101,110,118,112,110,44,112,112,116,112,44,115,111,99,107,115,53,44,102,114,101,101,59,194,90,0,44,0,1,0,0,0,200,0,22,1,1,157,186,85,206,163,184,225,85,40,102,90,103,129,202,124,53,25,12,240,236,194,90,0,17,0,1,0,0,0,100,0,37,3,109,105,97,1,99,5,109,99,110,101,116,3,99,111,109,0,4,97,100,100,114,3,109,105,97,5,109,99,110,101,116,3,99,111,109,0,194,90,0,17,0,1,0,0,0,100,0,39,4,97,108,101,120,1,98,5,109,99,110,101,116,3,99,111,109,0,4,97,100,100,114,4,97,108,101,120,5,109,99,110,101,116,3,99,111,109,0,194,90,0,17,0,1,0,0,0,100,0,19,4,108,117,99,121,1,99,5,109,99,110,101,116,3,99,111,109,0,0,194,90,0,17,0,1,0,0,0,100,0,36,5,109,105,99,104,97,1,99,5,109,99,110,101,116,3,99,111,109,0,5,109,105,99,104,97,5,109,99,110,101,116,3,99,111,109,0,196,204,0,18,0,1,0,0,0,100,0,18,0,1,4,115,114,118,50,5,109,99,110,101,116,3,99,111,109,0,196,234,0,18,0,1,0,0,0,100,0,18,0,2,4,115,114,118,51,5,109,99,110,101,116,3,99,111,109,0,197,8,0,11,0,1,0,0,0,100,0,72,192,168,178,25,6,0,84,64,64,0,0,0,0,0,0,0,0,0,0,5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,32,197,8,0,11,0,1,0,0,0,100,15,216,192,168,178,26,17,1,0,0,0,0,0,4,0,0,0,128,0,0,4,64,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,128,197,8,0,13,0,1,0,0,0,100,0,17,8,73,110,116,101,108,45,73,55,7,87,73,78,68,79,87,83,197,8,0,13,0,1,0,0,0,100,0,14,8,83,112,97,114,99,45,49,48,4,85,78,73,88,197,8,0,2,0,1,0,9,58,128,0,2,192,39,197,8,0,2,0,1,0,9,58,128,0,6,3,110,115,50,197,8,197,8,0,1,0,1,0,1,81,128,0,4,192,168,178,50,197,8,0,28,0,1,0,0,0,100,0,16,254,128,0,0,0,0,0,0,36,201,142,20,157,244,163,20,197,8,0,15,0,1,0,0,3,232,0,9,0,10,4,109,97,105,108,197,8,197,8,0,15,0,1,0,0,3,232,0,9,0,10,4,109,97,105,108,196,198,8,95,97,102,115,51,45,112,114,4,95,116,99,112,5,109,99,110,101,116,3,99,111,109,0,0,33,0,1,0,0,0,100,0,22,0,0,0,0,27,90,4,115,114,118,52,5,109,99,110,101,116,3,99,111,109,0,8,95,97,102,115,51,45,118,108,214,1,0,33,0,1,0,0,0,100,0,22,0,0,0,0,27,91,4,115,114,118,50,5,109,99,110,101,116,3,99,111,109,0,8,95,97,102,115,51,45,112,114,4,95,117,100,112,5,109,99,110,101,116,3,99,111,109,0,0,33,0,1,0,0,0,100,0,22,0,0,0,0,27,90,4,115,114,118,51,5,109,99,110,101,116,3,99,111,109,0,196,120,0,1,0,1,0,0,0,100,0,4,192,168,178,61,196,115,0,16,0,1,0,0,0,100,0,19,18,69,114,110,115,116,32,40,49,50,51,41,32,52,53,54,55,56,57,4,109,97,105,108,196,120,0,15,0,1,0,0,0,100,0,4,0,10,213,220,193,220,0,1,0,1,0,0,0,100,0,4,192,168,178,24,193,241,0,1,0,1,0,0,0,100,0,4,192,168,178,24,213,220,0,1,0,1,0,1,81,128,0,4,192,168,178,3,196,70,0,1,0,1,0,0,0,100,0,4,192,168,178,62,196,65,0,16,0,1,0,0,0,100,0,17,16,89,101,121,32,40,49,50,51,41,32,52,53,54,55,56,57,4,109,97,105,108,196,70,0,15,0,1,0,0,0,100,0,4,0,10,213,220,196,198,0,5,0,1,0,0,0,100,0,2,214,138,4,97,100,100,114,196,198,0,16,0,1,0,0,0,100,0,18,17,66,111,115,115,32,40,49,50,51,41,32,52,53,54,55,56,57,213,241,0,1,0,1,0,0,0,100,0,4,192,168,178,4,3,115,117,98,196,198,0,5,0,1,0,0,0,200,0,4,1,50,196,198,3,119,119,119,196,198,0,5,0,1,0,0,0,100,0,2,214,138,192,39,0,1,0,1,0,1,81,128,0,4,192,168,178,1,213,156,0,1,0,1,0,1,81,128,0,4,192,168,178,2,5,112,104,111,110,101,214,138,0,1,0,1,0,0,0,100,0,4,192,168,178,20,193,128,0,1,0,1,0,0,0,100,0,4,192,168,178,21,193,128,0,13,0,1,0,0,0,100,0,17,8,73,110,116,101,108,45,73,53,7,87,73,78,68,79,87,83,214,76,0,1,0,1,0,0,0,100,0,4,192,168,178,25,214,133,0,1,0,1,0,0,0,100,0,4,192,168,178,26,214,33,0,1,0,1,0,0,0,100,0,4,192,168,178,27,4,117,98,101,114,214,138,0,1,0,1,0,0,0,100,0,4,192,168,178,30,3,119,119,119,214,138,0,5,0,1,0,0,1,244,0,2,214,138,193,162,0,1,0,1,0,0,0,100,0,4,192,168,178,100,214,138,0,6,0,1,0,0,0,100,0,24,192,39,192,45,120,57,35,168,0,9,58,128,0,1,81,128,0,36,234,0,0,9,58,128,0,0,41,16,0,0,0,0,0,0,0
            };

            public IReadOnlyList<DnsQuerySettings> UsedSettings { get; }

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