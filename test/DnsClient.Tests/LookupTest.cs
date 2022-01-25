using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DnsClient.Protocol;
using DnsClient.Protocol.Options;
using Moq;
using Xunit;

namespace DnsClient.Tests
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class LookupTest
    {
        private static readonly IPAddress s_doesNotExist = IPAddress.Parse("192.168.21.43");
        private static readonly TimeSpan s_timeout = TimeSpan.FromMilliseconds(1);

        static LookupTest()
        {
            Tracing.Source.Switch.Level = System.Diagnostics.SourceLevels.All;
        }

        [Fact]
        public async Task ResolveService_WithCnameRef()
        {
            var ns = new NameServer(IPAddress.Loopback, 8600);
            var serviceName = "myservice";
            var baseName = "service.consul";
            var fullQuery = $"{serviceName}.{baseName}";
            ushort prio = 99;
            ushort weight = 69;
            ushort port = 88;

            var response = new DnsResponseMessage(new DnsResponseHeader(1234, (int)DnsResponseCode.NoError, 0, 0, 0, 0), 0);
            response.AddAnswer(
                new SrvRecord(
                    new ResourceRecordInfo(fullQuery, ResourceRecordType.SRV, QueryClass.IN, 1000, 0),
                    prio,
                    weight,
                    port,
                    DnsString.Parse(serviceName)));

            var targetHost = DnsString.Parse("myservice.localhost.net");
            response.AddAdditional(
                new CNameRecord(
                    new ResourceRecordInfo(serviceName, ResourceRecordType.CNAME, QueryClass.IN, 1000, 0),
                    targetHost));

            var client = new LookupClient(ns);

            var mock = new Mock<IDnsQuery>();
            mock.Setup(p => p.QueryAsync(It.IsAny<string>(), It.IsAny<QueryType>(), It.IsAny<QueryClass>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<IDnsQueryResponse>(response.AsQueryResponse(ns, client.Settings)));

            var mockClient = mock.Object;

            var result = await mockClient.ResolveServiceAsync(baseName, serviceName);

            Assert.Single(result);
            var first = result.First();
            Assert.Equal(targetHost.ToString(), first.HostName);
            Assert.Equal(port, first.Port);
            Assert.Equal(prio, first.Priority);
            Assert.Equal(weight, first.Weight);
        }

        [Fact]
        public async Task ResolveService_WithFQNSrv()
        {
            var ns = new NameServer(IPAddress.Loopback, 8600);
            var serviceName = "myservice";
            var baseName = "service.consul";
            var fullQuery = $"{serviceName}.{baseName}";
            ushort prio = 99;
            ushort weight = 69;
            ushort port = 88;

            var targetHost = DnsString.Parse("http://localhost:88/");
            var response = new DnsResponseMessage(new DnsResponseHeader(1234, (int)DnsResponseCode.NoError, 0, 0, 0, 0), 0);
            response.AddAnswer(
                new SrvRecord(
                    new ResourceRecordInfo(fullQuery, ResourceRecordType.SRV, QueryClass.IN, 1000, 0),
                    prio,
                    weight,
                    port,
                    targetHost));

            var client = new LookupClient(ns);

            var mock = new Mock<IDnsQuery>();
            mock.Setup(p => p.QueryAsync(It.IsAny<string>(), It.IsAny<QueryType>(), It.IsAny<QueryClass>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<IDnsQueryResponse>(response.AsQueryResponse(ns, client.Settings)));

            var mockClient = mock.Object;

            var result = await mockClient.ResolveServiceAsync(baseName, serviceName);
            Assert.Single(result);
            var first = result.First();
            Assert.Equal(targetHost.ToString(), first.HostName);
            Assert.Equal(port, first.Port);
            Assert.Equal(prio, first.Priority);
            Assert.Equal(weight, first.Weight);
        }

        [Fact]
        public void Lookup_Query_QuestionCannotBeNull()
        {
            IDnsQuery client = new LookupClient(NameServer.GooglePublicDns);

            Assert.Throws<ArgumentNullException>("question", () => client.Query(null));
            Assert.ThrowsAsync<ArgumentNullException>("question", () => client.QueryAsync(null));
        }

        [Fact]
        public void Lookup_Query_SettingsCannotBeNull()
        {
            IDnsQuery client = new LookupClient(NameServer.GooglePublicDns);
            var question = new DnsQuestion("query", QueryType.A);
            var servers = new NameServer[] { NameServer.GooglePublicDns };

            Assert.Throws<ArgumentNullException>("queryOptions", () => client.Query(question, null));
            Assert.ThrowsAsync<ArgumentNullException>("queryOptions", () => client.QueryAsync(question, null));

            Assert.Throws<ArgumentNullException>("queryOptions", () => client.QueryServer(servers, question, null));
            Assert.ThrowsAsync<ArgumentNullException>("queryOptions", () => client.QueryServerAsync(servers, question, null));

            Assert.Throws<ArgumentNullException>("queryOptions", () => client.QueryReverse(IPAddress.Loopback, null));
            Assert.ThrowsAsync<ArgumentNullException>("queryOptions", () => client.QueryReverseAsync(IPAddress.Loopback, null));

            Assert.Throws<ArgumentNullException>("queryOptions", () => client.QueryServerReverse(servers, IPAddress.Loopback, null));
            Assert.ThrowsAsync<ArgumentNullException>("queryOptions", () => client.QueryServerReverseAsync(servers, IPAddress.Loopback, null));
        }

        [Fact]
        public async Task Lookup_GetHostAddresses_Local()
        {
            var client = new LookupClient();

            var result = await client.QueryAsync("localhost", QueryType.A);

            var answer = result.Answers.OfType<ARecord>().First();
            Assert.Equal("127.0.0.1", answer.Address.ToString());
            Assert.Equal(QueryClass.IN, result.Questions[0].QuestionClass);
            Assert.Equal(QueryType.A, result.Questions[0].QuestionType);
            Assert.True(result.Header.AnswerCount > 0);
        }

        [Fact]
        public void Lookup_GetHostAddresses_Local_Sync()
        {
            var client = new LookupClient();

            var result = client.Query("localhost", QueryType.A);

            var answer = result.Answers.OfType<ARecord>().First();
            Assert.Equal("127.0.0.1", answer.Address.ToString());
            Assert.Equal(QueryClass.IN, result.Questions[0].QuestionClass);
            Assert.Equal(QueryType.A, result.Questions[0].QuestionType);
            Assert.True(result.Header.AnswerCount > 0);
        }

        [Fact]
        public void Lookup_DisabledEdns_NoAdditionals()
        {
            var dns = new LookupClient(NameServer.GooglePublicDns);

            var result = dns.Query(new DnsQuestion("google.com", QueryType.A), new DnsQueryAndServerOptions()
            {
                RequestDnsSecRecords = false,
                ExtendedDnsBufferSize = 512
            });

            Assert.Empty(result.Additionals);
        }

        [Fact]
        public void Lookup_EnabledEdns_DoFlag()
        {
            var dns = new LookupClient(NameServer.GooglePublicDns);

            var result = dns.Query(new DnsQuestion("google.com", QueryType.A), new DnsQueryAndServerOptions()
            {
                RequestDnsSecRecords = true
            });

            Assert.True(result.Additionals.OfType<OptRecord>().First().IsDnsSecOk);
        }

#if ENABLE_REMOTE_DNS

        // see #10, wrong TCP result handling if the result contains like 1000 answers
        [Fact]
        public void Lookup_LargeResultWithTCP()
        {
            var dns = new LookupClient(NameServer.Cloudflare);

            var result = dns.Query("big.basic.caatestsuite.com", QueryType.CAA);

            Assert.True(result.Answers.CaaRecords().Count() >= 1000);
        }

        [Fact]
        public void Lookup_IPv4_Works()
        {
            var client = new LookupClient(
                new LookupClientOptions(NameServer.GooglePublicDns)
                {
                    // force both requests
                    UseCache = false
                });

            // both should use the same server/udp client
            var resultA = client.Query("google.com", QueryType.ANY);
            var resultB = client.Query("google.com", QueryType.ANY);

            Assert.Equal(NameServer.GooglePublicDns, resultA.NameServer);
            Assert.Equal(NameServer.GooglePublicDns, resultB.NameServer);
            Assert.True(resultA.Answers.Count > 0);
            Assert.True(resultB.Answers.Count > 0);
        }

        [Fact]
        public void Lookup_IPv4_TcpOnly_Works()
        {
            var client = new LookupClient(
                new LookupClientOptions(NameServer.GooglePublicDns)
                {
                    // force both requests
                    UseCache = false,
                    UseTcpOnly = true
                });

            // both should use the same server/udp client
            var resultA = client.Query("google.com", QueryType.ANY);
            var resultB = client.Query("google.com", QueryType.ANY);

            Assert.Equal(NameServer.GooglePublicDns, resultA.NameServer);
            Assert.Equal(NameServer.GooglePublicDns, resultB.NameServer);
            Assert.True(resultA.Answers.Count > 0);
            Assert.True(resultB.Answers.Count > 0);
        }

        [Fact]
        public void Lookup_IPv6_Works()
        {
            var client = new LookupClient(
                new LookupClientOptions(NameServer.GooglePublicDnsIPv6)
                {
                    // force both requests
                    UseCache = false
                });

            // both should use the same server/udp client
            var resultA = client.Query("google.com", QueryType.ANY);
            var resultB = client.Query("google.com", QueryType.ANY);

            Assert.Equal(NameServer.GooglePublicDnsIPv6, resultA.NameServer);
            Assert.Equal(NameServer.GooglePublicDnsIPv6, resultB.NameServer);
            Assert.True(resultA.Answers.Count > 0);
            Assert.True(resultB.Answers.Count > 0);
        }

        [Fact]
        public void Lookup_IPv6_TcpOnly_Works()
        {
            var client = new LookupClient(
                new LookupClientOptions(NameServer.GooglePublicDnsIPv6)
                {
                    // force both requests
                    UseCache = false,
                    UseTcpOnly = true
                });

            // both should use the same server/udp client
            var resultA = client.Query("google.com", QueryType.ANY);
            var resultB = client.Query("google.com", QueryType.ANY);

            Assert.Equal(NameServer.GooglePublicDnsIPv6, resultA.NameServer);
            Assert.Equal(NameServer.GooglePublicDnsIPv6, resultB.NameServer);
            Assert.True(resultA.Answers.Count > 0);
            Assert.True(resultB.Answers.Count > 0);
        }

        [Fact]
        public void Lookup_MultiServer_IPv4_and_IPv6()
        {
            var client = new LookupClient(
                new LookupClientOptions(NameServer.GooglePublicDns, NameServer.GooglePublicDnsIPv6)
                {
                    // force both requests
                    UseCache = false
                });

            // server rotation should use both defined dns servers
            var resultA = client.QueryServer(new[] { NameServer.GooglePublicDns }, "google.com", QueryType.ANY);
            var resultB = client.QueryServer(new[] { NameServer.GooglePublicDnsIPv6 }, "google.com", QueryType.ANY);

            Assert.Equal(NameServer.GooglePublicDns, resultA.NameServer);
            Assert.Equal(NameServer.GooglePublicDnsIPv6, resultB.NameServer);
            Assert.True(resultA.Answers.Count > 0);
            Assert.True(resultB.Answers.Count > 0);
        }

        [Fact]
        public void Lookup_MultiServer_IPv4_and_IPv6_TCP()
        {
            var client = new LookupClient(
                new LookupClientOptions(NameServer.GooglePublicDns, NameServer.GooglePublicDnsIPv6)
                {
                    // force both requests
                    UseCache = false,
                    UseTcpOnly = true
                });

            // server rotation should use both defined dns servers
            var resultA = client.QueryServer(new[] { NameServer.GooglePublicDns }, "google.com", QueryType.ANY);
            var resultB = client.QueryServer(new[] { NameServer.GooglePublicDnsIPv6 }, "google.com", QueryType.ANY);

            Assert.Equal(NameServer.GooglePublicDns, resultA.NameServer);
            Assert.Equal(NameServer.GooglePublicDnsIPv6, resultB.NameServer);
            Assert.True(resultA.Answers.Count > 0);
            Assert.True(resultB.Answers.Count > 0);
        }

        [Fact]
        public async Task Lookup_ThrowDnsErrors()
        {
            var client = new LookupClient(
                new LookupClientOptions(NameServer.GooglePublicDns)
                {
                    ThrowDnsErrors = true
                });

            var ex = await Assert.ThrowsAnyAsync<DnsResponseException>(() => client.QueryAsync("lalacom", (QueryType)12345));

            Assert.Equal(DnsResponseCode.NotExistentDomain, ex.Code);
        }

        [Fact]
        public void Lookup_ThrowDnsErrors_Sync()
        {
            var client = new LookupClient(
                new LookupClientOptions(NameServer.GooglePublicDns)
                {
                    ThrowDnsErrors = true
                });

            var ex = Assert.ThrowsAny<DnsResponseException>(() => client.Query("lalacom", (QueryType)12345));

            Assert.Equal(DnsResponseCode.NotExistentDomain, ex.Code);
        }

        public class QueryTimesOutTests
        {
            [Fact]
            public async Task Lookup_QueryTimesOut_Udp_Async()
            {
                var client = new LookupClient(
                    new LookupClientOptions(new NameServer(IPAddress.Loopback))
                    {
                        Timeout = s_timeout,
                        Retries = 0,
                        UseTcpFallback = false
                    });

                var ex = await Assert.ThrowsAnyAsync<DnsResponseException>(() => client.QueryAsync("lala.com", QueryType.A));

                Assert.Equal(DnsResponseCode.ConnectionTimeout, ex.Code);
                Assert.Contains("timed out", ex.Message);
            }

            [Fact]
            public void Lookup_QueryTimesOut_Udp_Sync()
            {
                var client = new LookupClient(
                    new LookupClientOptions(new NameServer(IPAddress.Loopback))
                    {
                        Timeout = s_timeout,
                        Retries = 0,
                        UseTcpFallback = false
                    });

                var ex = Assert.ThrowsAny<DnsResponseException>(() => client.Query("lala.com", QueryType.A));

                Assert.Equal(DnsResponseCode.ConnectionTimeout, ex.Code);
                Assert.Contains("timed out", ex.Message);
            }

            [Fact]
            public async Task Lookup_QueryTimesOut_Tcp_Async()
            {
                var client = new LookupClient(
                    new LookupClientOptions(new NameServer(IPAddress.Loopback))
                    {
                        Timeout = s_timeout,
                        Retries = 0,
                        UseTcpOnly = true
                    });

                var ex = await Assert.ThrowsAnyAsync<DnsResponseException>(() => client.QueryAsync("lala.com", QueryType.A));

                Assert.Equal(DnsResponseCode.ConnectionTimeout, ex.Code);
                Assert.Contains("timed out", ex.Message);
            }

            [Fact]
            public void Lookup_QueryTimesOut_Tcp_Sync()
            {
                var client = new LookupClient(
                    new LookupClientOptions(new NameServer(IPAddress.Loopback))
                    {
                        Timeout = s_timeout,
                        Retries = 0,
                        UseTcpOnly = true
                    });

                var ex = Assert.ThrowsAny<DnsResponseException>(() => client.Query("lala.com", QueryType.A));

                Assert.Equal(DnsResponseCode.ConnectionTimeout, ex.Code);
                Assert.Contains("timed out", ex.Message);
            }
        }

        public class DelayCancelTest
        {
            [Fact]
            public async Task Lookup_QueryDelayCanceled_Udp()
            {
                var client = new LookupClient(
                    new LookupClientOptions(s_doesNotExist)
                    {
                        Timeout = TimeSpan.FromMilliseconds(1000),
                        UseTcpFallback = false
                    });

                // should hit the cancellation timeout, not the 1sec timeout
                var tokenSource = new CancellationTokenSource(s_timeout);

                var token = tokenSource.Token;

                var ex = await Assert.ThrowsAnyAsync<DnsResponseException>(() => client.QueryAsync("lala.com", QueryType.A, cancellationToken: token));
                Assert.NotNull(ex.InnerException);
            }

            [Fact]
            public async Task Lookup_QueryDelayCanceled_Tcp()
            {
                var client = new LookupClient(
                    new LookupClientOptions(s_doesNotExist)
                    {
                        Timeout = TimeSpan.FromMilliseconds(1000),
                        UseTcpOnly = true
                    });

                // should hit the cancellation timeout, not the 1sec timeout
                var tokenSource = new CancellationTokenSource(s_timeout);

                var token = tokenSource.Token;

                var ex = await Assert.ThrowsAnyAsync<DnsResponseException>(() => client.QueryAsync("lala.com", QueryType.A, cancellationToken: token));
                Assert.NotNull(ex.InnerException);
            }

            [Fact]
            public async Task Lookup_QueryDelayCanceledWithUnlimitedTimeout_Udp()
            {
                var client = new LookupClient(
                    new LookupClientOptions(s_doesNotExist)
                    {
                        Timeout = Timeout.InfiniteTimeSpan,
                        UseTcpFallback = false
                    });

                // should hit the cancellation timeout, not the 1sec timeout
                var tokenSource = new CancellationTokenSource(s_timeout);

                var token = tokenSource.Token;

                var ex = await Assert.ThrowsAnyAsync<DnsResponseException>(() => client.QueryAsync("lala.com", QueryType.A, cancellationToken: token));
                Assert.NotNull(ex.InnerException);
            }

            [Fact]
            public async Task Lookup_QueryDelayCanceledWithUnlimitedTimeout_Tcp()
            {
                var client = new LookupClient(
                    new LookupClientOptions(s_doesNotExist)
                    {
                        Timeout = Timeout.InfiniteTimeSpan,
                        UseTcpOnly = true
                    });

                // should hit the cancellation timeout, not the 1sec timeout
                var tokenSource = new CancellationTokenSource(s_timeout);

                var token = tokenSource.Token;

                var ex = await Assert.ThrowsAnyAsync<DnsResponseException>(() => client.QueryAsync("lala.com", QueryType.A, cancellationToken: token));
                Assert.NotNull(ex.InnerException);
            }
        }

        [Fact]
        public async Task Lookup_QueryCanceled_Udp()
        {
            var client = new LookupClient(
                new LookupClientOptions(NameServer.GooglePublicDns)
                {
                    UseTcpFallback = false
                });

            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            Func<Task> act = () => client.QueryAsync("lala.com", QueryType.A, cancellationToken: token);
            tokenSource.Cancel();

            var ex = await Assert.ThrowsAnyAsync<DnsResponseException>(act);

            Assert.NotNull(ex.InnerException);
        }

        [Fact]
        public async Task Lookup_QueryCanceled_Tcp()
        {
            var client = new LookupClient(
                new LookupClientOptions(NameServer.GooglePublicDns)
                {
                    UseTcpOnly = true
                });

            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            Func<Task> act = () => client.QueryAsync("lala.com", QueryType.A, cancellationToken: token);
            tokenSource.Cancel();

            var ex = await Assert.ThrowsAnyAsync<DnsResponseException>(act);

            Assert.NotNull(ex.InnerException);
        }

        [Fact]
        public async Task GetHostName()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);
            string hostName = await client.GetHostNameAsync(IPAddress.Parse("8.8.8.8"));

            Assert.Equal("dns.google", hostName);
        }

        [Fact]
        public async Task Lookup_Reverse()
        {
            var client = new LookupClient();
            var result = await client.QueryReverseAsync(IPAddress.Parse("127.0.0.1"));

            Assert.Equal("localhost.", result.Answers.PtrRecords().First().PtrDomainName.Value);
        }

        [Fact]
        public void Lookup_ReverseSync()
        {
            var client = new LookupClient();
            var result = client.QueryReverse(IPAddress.Parse("127.0.0.1"));

            Assert.Equal("localhost.", result.Answers.PtrRecords().First().PtrDomainName.Value);
        }

#endif

        [Fact]
        public async Task Lookup_Query_AAAA()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);
            var result = await client.QueryAsync("google.com", QueryType.AAAA);

            Assert.NotEmpty(result.Answers.AaaaRecords());
            Assert.NotNull(result.Answers.AaaaRecords().First().Address);
        }

        [Fact]
        public void Lookup_Query_AAAA_Sync()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);
            var result = client.Query("google.com", QueryType.AAAA);

            Assert.NotEmpty(result.Answers.AaaaRecords());
            Assert.NotNull(result.Answers.AaaaRecords().First().Address);
        }

        [Fact]
        public async Task Lookup_Query_Any()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);
            var result = await client.QueryAsync("google.com", QueryType.ANY);

            Assert.NotEmpty(result.Answers);
            Assert.NotEmpty(result.Answers.ARecords());
        }

        [Fact]
        public void Lookup_Query_Any_Sync()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);
            var result = client.Query("google.com", QueryType.ANY);

            Assert.NotEmpty(result.Answers);
            Assert.NotEmpty(result.Answers.ARecords());
        }

        [Fact]
        public async Task Lookup_Query_Mx()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);
            var result = await client.QueryAsync("google.com", QueryType.MX);

            Assert.NotEmpty(result.Answers.MxRecords());
            Assert.NotNull(result.Answers.MxRecords().First().Exchange);
            Assert.True(result.Answers.MxRecords().First().Preference > 0);
        }

        [Fact]
        public void Lookup_Query_Mx_Sync()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);
            var result = client.Query("google.com", QueryType.MX);

            Assert.NotEmpty(result.Answers.MxRecords());
            Assert.NotNull(result.Answers.MxRecords().First().Exchange);
            Assert.True(result.Answers.MxRecords().First().Preference > 0);
        }

        [Fact]
        public async Task Lookup_Query_NS()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);
            var result = await client.QueryAsync("google.com", QueryType.NS);

            Assert.NotEmpty(result.Answers.NsRecords());
            Assert.NotNull(result.Answers.NsRecords().First().NSDName);
        }

        [Fact]
        public void Lookup_Query_NS_Sync()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);
            var result = client.Query("google.com", QueryType.NS);

            Assert.NotEmpty(result.Answers.NsRecords());
            Assert.NotNull(result.Answers.NsRecords().First().NSDName);
        }

        [Fact]
        public async Task Lookup_Query_TXT()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);
            var result = await client.QueryAsync("google.com", QueryType.TXT);

            Assert.NotEmpty(result.Answers.TxtRecords());
            Assert.NotEmpty(result.Answers.TxtRecords().First().EscapedText);
            Assert.NotEmpty(result.Answers.TxtRecords().First().Text);
        }

        [Fact]
        public void Lookup_Query_TXT_Sync()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);
            var result = client.Query("google.com", QueryType.TXT);

            Assert.NotEmpty(result.Answers.TxtRecords());
            Assert.NotEmpty(result.Answers.TxtRecords().First().EscapedText);
            Assert.NotEmpty(result.Answers.TxtRecords().First().Text);
        }

        [Fact]
        public async Task Lookup_Query_SOA()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);
            var result = await client.QueryAsync("google.com", QueryType.SOA);

            Assert.NotEmpty(result.Answers.SoaRecords());
            Assert.NotNull(result.Answers.SoaRecords().First().MName);
            Assert.NotNull(result.Answers.SoaRecords().First().RName);
        }

        [Fact]
        public void Lookup_Query_SOA_Sync()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);
            var result = client.Query("google.com", QueryType.SOA);

            Assert.NotEmpty(result.Answers.SoaRecords());
            Assert.NotNull(result.Answers.SoaRecords().First().MName);
            Assert.NotNull(result.Answers.SoaRecords().First().RName);
        }

        [Fact]
        public async Task Lookup_Query_Puny()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);
            var result = await client.QueryAsync("müsli.de", QueryType.A);

            Assert.NotEmpty(result.Answers);
            Assert.NotEmpty(result.Answers.ARecords());
        }

        [Fact]
        public void Lookup_Query_Puny_Sync()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);
            var result = client.Query("müsli.de", QueryType.A);

            Assert.NotEmpty(result.Answers);
            Assert.NotEmpty(result.Answers.ARecords());
        }

        [Fact]
        public void Lookup_Query_Puny2()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);
            var result = client.Query("müsli.com", QueryType.ANY);

            Assert.NotEmpty(result.Answers);
            Assert.NotEmpty(result.Answers.ARecords());
        }

        [Fact]
        public void Ip_Arpa_v4_Valid()
        {
            var ip = IPAddress.Parse("8.8.4.4");
            var client = new LookupClient(NameServer.GooglePublicDns);

            var result = DnsString.Parse(ip.GetArpaName());
            var queryResult = client.QueryReverse(ip);

            Assert.Equal("4.4.8.8.in-addr.arpa.", result);
            Assert.Contains("dns.google", queryResult.Answers.PtrRecords().First().PtrDomainName);
        }

#if ENABLE_REMOTE_DNS

        [Fact]
        public void Ip_Arpa_v6_Valid()
        {
            var ip = NameServer.GooglePublicDns2IPv6.Address;
            var client = new LookupClient(NameServer.GooglePublicDnsIPv6);

            var result = DnsString.Parse(ip.GetArpaName());
            var queryResult = client.QueryReverse(ip);

            Assert.Equal("8.8.8.8.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.6.8.4.0.6.8.4.1.0.0.2.ip6.arpa.", result);
            Assert.Contains("dns.google", queryResult.Answers.PtrRecords().First().PtrDomainName);
        }

        [Fact]
        public async Task Lookup_Query_NaPtr()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);
            var result = await client.QueryAsync("tel.t-online.de", QueryType.NAPTR);

            Assert.NotEmpty(result.Answers.NAPtrRecords());
            var naptrRecord = result.Answers.NAPtrRecords().First();
            Assert.NotNull(naptrRecord);
            Assert.True(naptrRecord.Order > 0);

            var hosts = client.ResolveService(naptrRecord.Replacement);
            Assert.NotEmpty(hosts);
            var host = hosts.First();
            Assert.NotNull(host.HostName);
        }

#endif

        [Fact]
        public async Task GetHostEntry_ExampleSub()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);
            var hostEntry = await client.GetHostEntryAsync("mail.google.com");
            Assert.EndsWith("google.com", hostEntry.Aliases.First(), StringComparison.OrdinalIgnoreCase);
            Assert.Equal("mail.google.com", hostEntry.HostName);
            Assert.True(hostEntry.AddressList.Length > 0);
        }

        [Fact]
        public void GetHostEntry_ByName_ManyIps_NoAlias()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);

            var result = client.GetHostEntry("google.com");

            Assert.True(result.AddressList.Length > 1);
            Assert.True(result.Aliases.Length == 0);
            Assert.Equal("google.com", result.HostName);
        }

        [Fact]
        public void GetHostEntry_ByName_ManyAliases()
        {
            var client = new LookupClient(new LookupClientOptions(NameServer.GooglePublicDns)
            {
                ThrowDnsErrors = true
            });

            var result = client.GetHostEntry("dnsclient.michaco.net");

            Assert.True(result.AddressList.Length >= 1);
            Assert.True(result.Aliases.Length > 1);
            Assert.Equal("dnsclient.michaco.net", result.HostName);
        }

        [Fact]
        public void GetHostEntry_ByName_EmptyString()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);

            Action act = () => client.GetHostEntry("");

            Assert.Throws<ArgumentNullException>("hostNameOrAddress", act);
        }

        [Fact]
        public void GetHostEntry_ByName_HostDoesNotExist()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);

            var result = client.GetHostEntry("lolhost");

            Assert.True(result.AddressList.Length == 0);
            Assert.True(result.Aliases.Length == 0);
            Assert.Equal("lolhost", result.HostName);
        }

        [Fact]
        public void GetHostEntry_ByName_HostDoesNotExist_WithThrow()
        {
            var client = new LookupClient(
                new LookupClientOptions(NameServer.GooglePublicDns)
                {
                    ThrowDnsErrors = true
                });

            var ex = Assert.ThrowsAny<DnsResponseException>(() => client.GetHostEntry("lolhost"));

            Assert.Equal(DnsResponseCode.NotExistentDomain, ex.Code);
        }

        [Fact]
        public void GetHostEntry_ByIp_NoHost()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);

            var result = client.GetHostEntry("1.0.0.0");

            Assert.Null(result);
        }

        [Fact]
        public void GetHostEntry_ByIp_NoHost_WithThrow()
        {
            var client = new LookupClient(
                new LookupClientOptions(NameServer.GooglePublicDns)
                {
                    ThrowDnsErrors = true
                });

            Action act = () => client.GetHostEntry("1.0.0.0");

            var ex = Assert.ThrowsAny<DnsResponseException>(() => client.GetHostEntry("1.0.0.0"));

            Assert.Equal(DnsResponseCode.NotExistentDomain, ex.Code);
        }

        [Fact]
        public void GetHostEntry_ByManyIps()
        {
            var client = new LookupClient(new LookupClientOptions(NameServer.GooglePublicDns)
            {
                ThrowDnsErrors = true
            });

            var nsServers = client.Query("google.com", QueryType.NS).Answers.NsRecords().ToArray();

            Assert.True(nsServers.Length > 0);

            foreach (var server in nsServers)
            {
                var ipAddress = client.GetHostEntry(server.NSDName).AddressList.First();
                var result = client.GetHostEntry(ipAddress);

                Assert.NotNull(result);
                Assert.True(result.AddressList.Length >= 1);
                Assert.Contains(ipAddress, result.AddressList);
                Assert.True(result.Aliases.Length == 0);

                // expecting always the name without . at the end!
                Assert.Equal(server.NSDName.Value.Substring(0, server.NSDName.Value.Length - 1), result.HostName);
            }
        }

        [Fact]
        public async Task GetHostEntryAsync_ByName_ManyIps_NoAlias()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);

            var result = await client.GetHostEntryAsync("google.com");

            Assert.True(result.AddressList.Length > 1);
            Assert.True(result.Aliases.Length == 0);
            Assert.Equal("google.com", result.HostName);
        }

        [Fact]
        public async Task GetHostEntryAsync_ByName_OneIp_ManyAliases()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);

            var result = await client.GetHostEntryAsync("dnsclient.michaco.net");

            Assert.True(result.AddressList.Length == 1);
            Assert.True(result.Aliases.Length > 1);
            Assert.Equal("dnsclient.michaco.net", result.HostName);
        }

#if ENABLE_REMOTE_DNS

        [Fact]
        public async Task GetHostEntryAsync_ByName_OneIp_NoAlias()
        {
            var client = new LookupClient();

            var result = await client.GetHostEntryAsync("localhost");

            Assert.True(result.AddressList.Length == 1);
            Assert.True(result.Aliases.Length == 0);
            Assert.Equal("localhost", result.HostName);
        }

        [Fact]
        public async Task GetHostEntryAsync_ByName_HostDoesNotExist_WithThrow()
        {
            var client = new LookupClient(
                new LookupClientOptions()
                {
                    ThrowDnsErrors = true
                });

            var ex = await Assert.ThrowsAnyAsync<DnsResponseException>(() => client.GetHostEntryAsync("lolhost"));

            Assert.Equal(DnsResponseCode.NotExistentDomain, ex.Code);
        }

#endif

        [Fact]
        public async Task GetHostEntryAsync_ByName_EmptyString()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);

            await Assert.ThrowsAsync<ArgumentNullException>("hostNameOrAddress", () => client.GetHostEntryAsync(""));
        }

        [Fact]
        public async Task GetHostEntryAsync_ByName_HostDoesNotExist()
        {
            var client = new LookupClient();

            var result = await client.GetHostEntryAsync("lolhost");

            Assert.True(result.AddressList.Length == 0);
            Assert.True(result.Aliases.Length == 0);
            Assert.Equal("lolhost", result.HostName);
        }

        [Fact]
        public async Task GetHostEntryAsync_ByIp_NoHost()
        {
            var client = new LookupClient();

            var result = await client.GetHostEntryAsync("1.0.0.0");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetHostEntryAsync_ByManyIps()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);
            var nsServers = client.Query("google.com", QueryType.NS).Answers.NsRecords().ToArray();

            Assert.True(nsServers.Length > 0, "Should have more than 0 NS servers");

            foreach (var server in nsServers)
            {
                var ipAddress = (await client.GetHostEntryAsync(server.NSDName)).AddressList.First();
                var result = await client.GetHostEntryAsync(ipAddress);

                Assert.NotNull(result);
                Assert.True(result.AddressList.Length >= 1, "Revers should have found at least one IP");
                Assert.True(result.AddressList.Contains(ipAddress), "Result should contain the NSDName address");
                Assert.True(result.Aliases.Length == 0, "There shouldn't be an alias");

                // expecting always the name without . at the end!
                Assert.Equal(server.NSDName.Value.Substring(0, server.NSDName.Value.Length - 1), result.HostName);
            }
        }

        [Fact]
        public void Lookup_SettingsFallback_UseClients()
        {
            var client = new LookupClient(NameServer.CloudflareIPv6);

            var settings = client.GetSettings(queryOptions: null);

            Assert.Equal(client.Settings, settings);
        }

        [Fact]
        // Tests that if a server gets passed in and the auto resolve flag is set to true
        // Lookup Client's ctor will auto resolve servers and append them to the specified server(s)
        public void Lookup_Options_UseClientsAndResolvedServers()
        {
            // Specify one and auto resolve
            var client = new LookupClient(new LookupClientOptions(NameServer.Cloudflare) { AutoResolveNameServers = true });

            Assert.True(client.NameServers.Count > 1);
            Assert.Contains(NameServer.Cloudflare, client.NameServers);
        }

        [Fact]
        // Tests that if a server gets passed in, the AutoResolve option gets set to false
        // And Lookup Client's ctor will not auto resolve any servers.
        public void Lookup_Options_AutoResolveDisabled_WhenServerIsSpecified1()
        {
            // Specify one and auto resolve
            var client = new LookupClient(new LookupClientOptions(NameServer.Cloudflare));

            Assert.Single(client.NameServers);
            Assert.Contains(NameServer.Cloudflare, client.NameServers);
        }

        [Fact]
        public void Lookup_Options_AutoResolveDisabled_WhenServerIsSpecified2()
        {
            // Specify one and auto resolve
            var client = new LookupClient(new LookupClientOptions(NameServer.Cloudflare.Address));

            Assert.Single(client.NameServers);
            Assert.Contains(NameServer.Cloudflare, client.NameServers);
        }

        [Fact]
        public void Lookup_Options_AutoResolveDisabled_WhenServerIsSpecified3()
        {
            // Specify one and auto resolve
            var client = new LookupClient(new LookupClientOptions(new IPEndPoint(NameServer.Cloudflare.Address, 33)));

            Assert.Single(client.NameServers);
            Assert.Contains(new IPEndPoint(NameServer.Cloudflare.Address, 33), client.NameServers);
        }

        [Fact]
        public void Lookup_SettingsFallback_UseClientsServers()
        {
            var client = new LookupClient(NameServer.CloudflareIPv6);

            // Test that the settings in the end has the name servers configured on the client above and
            // still the settings provided apart from the servers (everything else will not fallback to the client's settings...)
            var settings = client.GetSettings(queryOptions: new DnsQueryAndServerOptions()
            {
                ContinueOnDnsError = false,
                Recursion = false,
                RequestDnsSecRecords = true
            });

            Assert.Equal(client.NameServers, settings.NameServers);
            Assert.Single(settings.NameServers);
            Assert.NotEqual(client.Settings, settings);
            Assert.NotEqual(client.Settings.ContinueOnDnsError, settings.ContinueOnDnsError);
            Assert.NotEqual(client.Settings.Recursion, settings.Recursion);
            Assert.NotEqual(client.Settings.RequestDnsSecRecords, settings.RequestDnsSecRecords);
        }

        [Fact]
        public void Lookup_SettingsFallback_KeepProvidedServers1()
        {
            var client = new LookupClient(NameServer.CloudflareIPv6);

            var settings = client.GetSettings(queryOptions: new DnsQueryAndServerOptions(NameServer.GooglePublicDns));

            Assert.NotEqual(client.Settings, settings);
            Assert.NotEqual(client.NameServers, settings.NameServers);
        }

        [Fact]
        public void Lookup_SettingsFallback_KeepProvidedServers2()
        {
            var client = new LookupClient(NameServer.CloudflareIPv6);

            var settings = client.GetSettings(queryOptions: new DnsQueryAndServerOptions(IPAddress.Loopback));

            Assert.NotEqual(client.Settings, settings);
            Assert.NotEqual(client.NameServers, settings.NameServers);
        }

        [Fact]
        public void Lookup_SettingsFallback_KeepProvidedServers3()
        {
            var client = new LookupClient(NameServer.CloudflareIPv6);

            var settings = client.GetSettings(queryOptions: new DnsQueryAndServerOptions(new IPEndPoint(IPAddress.Loopback, 33)));

            Assert.NotEqual(client.Settings, settings);
            Assert.NotEqual(client.NameServers, settings.NameServers);
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(3, true)]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(3, false)]
        public async Task Lookup_XidMismatch(int mismatchResponses, bool sync)
        {
            var serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 54321);
            var options = new LookupClientOptions(new NameServer(serverEndpoint))
            {
                Retries = 20,
                UseCache = false
            };

            using var server = new UdpServerMistmatchXid(serverEndpoint, mismatchResponses);
            var client = new LookupClient(options);

            var dnsQuestion = new DnsQuestion("someservice", QueryType.TXT, QueryClass.IN);
            var response = sync ? client.Query(dnsQuestion) : await client.QueryAsync(dnsQuestion);

            Assert.Equal(2, response.Answers.TxtRecords().Count());
            Assert.Equal("example.com.", response.Answers.TxtRecords().First().DomainName.Value);
            Assert.Equal(mismatchResponses, server.MistmatchedResponsesCount);
            Assert.Equal(mismatchResponses + 1, server.RequestsCount);
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(3, true)]
        [InlineData(5, true)]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(3, false)]
        [InlineData(5, false)]
        public async Task Lookup_DuplicateUDPResponses(int duplicatesCount, bool sync)
        {
            var serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 54321);
            var options = new LookupClientOptions(new NameServer(serverEndpoint))
            {
                Retries = 20,
                UseCache = false,
                Timeout = TimeSpan.FromSeconds(5)
            };

            using var server = new UdpServerDuplicateResponses(serverEndpoint, duplicatesCount);
            var client = new LookupClient(options);

            var dnsQuestion = new DnsQuestion("someservice", QueryType.TXT, QueryClass.IN);
            var response1 = sync ? client.Query(dnsQuestion) : await client.QueryAsync(dnsQuestion);
            var response2 = sync ? client.Query(dnsQuestion) : await client.QueryAsync(dnsQuestion);

            Assert.Equal(2, response1.Answers.TxtRecords().Count());
            Assert.Equal("example.com.", response1.Answers.TxtRecords().First().DomainName.Value);

            Assert.Equal(2, response2.Answers.TxtRecords().Count());
            Assert.Equal("example.com.", response2.Answers.TxtRecords().First().DomainName.Value);

            Assert.True(server.RequestsCount >= 2, "At least 2 requests are expected");

            // Validate that duplicate response was not picked up
            Assert.NotEqual(response1.Header.Id, response2.Header.Id);
        }
    }
}
