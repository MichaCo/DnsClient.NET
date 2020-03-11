using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DnsClient.Protocol;
using DnsClient.Protocol.Options;
using Xunit;

namespace DnsClient.Tests
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class LookupTest
    {
        private static readonly IPAddress DoesNotExist = IPAddress.Parse("192.168.21.43");

        static LookupTest()
        {
            Tracing.Source.Switch.Level = System.Diagnostics.SourceLevels.Information;
        }

        [Fact]
        public void Lookup_Query_QuestionCannotBeNull()
        {
            IDnsQuery client = new LookupClient();

            Assert.Throws<ArgumentNullException>("question", () => client.Query(null));
            Assert.ThrowsAsync<ArgumentNullException>("question", () => client.QueryAsync(null));
        }

        [Fact]
        public void Lookup_Query_SettingsCannotBeNull()
        {
            IDnsQuery client = new LookupClient();

            Assert.Throws<ArgumentNullException>("queryOptions", () => client.Query("query", QueryType.A, null));
            Assert.ThrowsAsync<ArgumentNullException>("queryOptions", () => client.QueryAsync("query", QueryType.A, null));

            Assert.Throws<ArgumentNullException>("queryOptions", () => client.QueryReverse(IPAddress.Loopback, null));
            Assert.ThrowsAsync<ArgumentNullException>("queryOptions", () => client.QueryReverseAsync(IPAddress.Loopback, null));
        }

#if !NET461

        [Fact]
        public void NativeDnsServerResolution()
        {
            var ex = Record.Exception(() => NameServer.ResolveNameServersNative());
            Assert.Null(ex);
        }

#endif

        [Fact]
        public async Task Lookup_GetHostAddresses_Local()
        {
            var client = new LookupClient();

            var result = await client.QueryAsync("localhost", QueryType.A);

            var answer = result.Answers.OfType<ARecord>().First();
            Assert.Equal("127.0.0.1", answer.Address.ToString());
            Assert.Equal(QueryClass.IN, result.Questions.First().QuestionClass);
            Assert.Equal(QueryType.A, result.Questions.First().QuestionType);
            Assert.True(result.Header.AnswerCount > 0);
        }

        [Fact]
        public void Lookup_GetHostAddresses_Local_Sync()
        {
            var client = new LookupClient();

            var result = client.Query("localhost", QueryType.A);

            var answer = result.Answers.OfType<ARecord>().First();
            Assert.Equal("127.0.0.1", answer.Address.ToString());
            Assert.Equal(QueryClass.IN, result.Questions.First().QuestionClass);
            Assert.Equal(QueryType.A, result.Questions.First().QuestionType);
            Assert.True(result.Header.AnswerCount > 0);
        }

        [Fact]
        public async Task Lookup_GetHostAddresses_LocalReverse_NoResult()
        {
            // expecting no result as reverse lookup must be explicit
            var client = new LookupClient(
                new LookupClientOptions()
                {
                    Timeout = TimeSpan.FromMilliseconds(500),
                    EnableAuditTrail = true
                });

            var result = await client.QueryAsync("127.0.0.1", QueryType.A);

            Assert.Equal(QueryClass.IN, result.Questions.First().QuestionClass);
            Assert.Equal(QueryType.A, result.Questions.First().QuestionType);
            Assert.True(result.Header.AnswerCount == 0);
        }

        [Fact]
        public void Lookup_GetHostAddresses_LocalReverse_NoResult_Sync()
        {
            // expecting no result as reverse lookup must be explicit
            var client = new LookupClient(
                new LookupClientOptions()
                {
                    Timeout = TimeSpan.FromMilliseconds(500),
                    EnableAuditTrail = true
                });

            var result = client.Query("127.0.0.1", QueryType.A);

            Assert.Equal(QueryClass.IN, result.Questions.First().QuestionClass);
            Assert.Equal(QueryType.A, result.Questions.First().QuestionType);
            Assert.True(result.Header.AnswerCount == 0);
        }

        [Fact]
        public void Lookup_DisabledEdns_NoAdditionals()
        {
            var dns = new LookupClient(NameServer.GooglePublicDns);

            var result = dns.Query("google.com", QueryType.A, queryOptions: new DnsQueryAndServerOptions()
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

            var result = dns.Query("google.com", QueryType.A, queryOptions: new DnsQueryAndServerOptions()
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
            var dns = new LookupClient(NameServer.GooglePublicDns);

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

#endif

        [Fact]
        public void Lookup_ThrowDnsErrors()
        {
            var client = new LookupClient(
                new LookupClientOptions()
                {
                    ThrowDnsErrors = true
                });

            Action act = () => client.QueryAsync("lalacom", (QueryType)12345).GetAwaiter().GetResult();

            var ex = Record.Exception(act) as DnsResponseException;

            Assert.Equal(DnsResponseCode.NotExistentDomain, ex.Code);
        }

        [Fact]
        public void Lookup_ThrowDnsErrors_Sync()
        {
            var client = new LookupClient(
                new LookupClientOptions()
                {
                    ThrowDnsErrors = true
                });

            Action act = () => client.Query("lalacom", (QueryType)12345);

            var ex = Record.Exception(act) as DnsResponseException;

            Assert.Equal(DnsResponseCode.NotExistentDomain, ex.Code);
        }

        public class QueryTimesOutTests
        {
            [Fact]
            public async Task Lookup_QueryTimesOut_Udp_Async()
            {
                var client = new LookupClient(
                    new LookupClientOptions(DoesNotExist)
                    {
                        Timeout = TimeSpan.FromMilliseconds(200),
                        Retries = 0,
                        UseTcpFallback = false
                    });

                var ex = await Assert.ThrowsAnyAsync<DnsResponseException>(() => client.QueryAsync("lala.com", QueryType.A));

                Assert.NotNull(ex);
                Assert.Equal(DnsResponseCode.ConnectionTimeout, ex.Code);
                Assert.Contains("timed out", ex.Message);
            }

            [Fact]
            public void Lookup_QueryTimesOut_Udp_Sync()
            {
                var client = new LookupClient(
                    new LookupClientOptions(DoesNotExist)
                    {
                        Timeout = TimeSpan.FromMilliseconds(200),
                        Retries = 0,
                        UseTcpFallback = false
                    });

                var exe = Record.Exception(() => client.Query("lala.com", QueryType.A));

                var ex = exe as DnsResponseException;
                Assert.NotNull(ex);
                Assert.Equal(DnsResponseCode.ConnectionTimeout, ex.Code);
                Assert.Contains("timed out", ex.Message);
            }

            [Fact]
            public async Task Lookup_QueryTimesOut_Tcp_Async()
            {
                var client = new LookupClient(
                    new LookupClientOptions(DoesNotExist)
                    {
                        Timeout = TimeSpan.FromMilliseconds(200),
                        Retries = 0,
                        UseTcpOnly = true
                    });

                var exe = await Record.ExceptionAsync(() => client.QueryAsync("lala.com", QueryType.A));

                var ex = exe as DnsResponseException;
                Assert.NotNull(ex);
                Assert.Equal(DnsResponseCode.ConnectionTimeout, ex.Code);
                Assert.Contains("timed out", ex.Message);
            }

            [Fact]
            public void Lookup_QueryTimesOut_Tcp_Sync()
            {
                var client = new LookupClient(
                    new LookupClientOptions(DoesNotExist)
                    {
                        Timeout = TimeSpan.FromMilliseconds(200),
                        Retries = 0,
                        UseTcpOnly = true
                    });

                var exe = Record.Exception(() => client.Query("lala.com", QueryType.A));

                var ex = exe as DnsResponseException;
                Assert.NotNull(ex);
                Assert.Equal(DnsResponseCode.ConnectionTimeout, ex.Code);
                Assert.Contains("timed out", ex.Message);
            }
        }

        [Fact]
        public void Lookup_QueryCanceled_Udp()
        {
            var client = new LookupClient(
                new LookupClientOptions()
                {
                    UseTcpFallback = false
                });

            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            Action act = () => client.QueryAsync("lala.com", QueryType.A, cancellationToken: token).GetAwaiter().GetResult();
            tokenSource.Cancel();

            var ex = Record.Exception(act) as OperationCanceledException;

            Assert.NotNull(ex);
            Assert.Equal(ex.CancellationToken, token);
        }

        [Fact]
        public void Lookup_QueryCanceled_Tcp()
        {
            var client = new LookupClient(
                new LookupClientOptions()
                {
                    UseTcpOnly = true
                });

            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            Action act = () => client.QueryAsync("lala.com", QueryType.A, cancellationToken: token).GetAwaiter().GetResult();
            tokenSource.Cancel();

            var ex = Record.Exception(act) as OperationCanceledException;

            Assert.NotNull(ex);
            Assert.Equal(ex.CancellationToken, token);
        }

        public class DelayCancelTest
        {
            [Fact]
            public async Task Lookup_QueryDelayCanceled_Udp()
            {
                var client = new LookupClient(
                    new LookupClientOptions(DoesNotExist)
                    {
                        Timeout = TimeSpan.FromMilliseconds(1000),
                        UseTcpFallback = false
                    });

                // should hit the cancelation timeout, not the 1sec timeout
                var tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

                var token = tokenSource.Token;

                var ex = await Record.ExceptionAsync(() => client.QueryAsync("lala.com", QueryType.A, cancellationToken: token));
                Assert.True(ex is OperationCanceledException);
                Assert.Equal(token, ((OperationCanceledException)ex).CancellationToken);
            }

            [Fact]
            public async Task Lookup_QueryDelayCanceled_Tcp()
            {
                var client = new LookupClient(
                    new LookupClientOptions(DoesNotExist)
                    {
                        Timeout = TimeSpan.FromMilliseconds(1000),
                        UseTcpOnly = true
                    });

                // should hit the cancelation timeout, not the 1sec timeout
                var tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

                var token = tokenSource.Token;

                var ex = await Record.ExceptionAsync(() => client.QueryAsync("lala.com", QueryType.A, cancellationToken: token));
                Assert.True(ex is OperationCanceledException);
                Assert.Equal(token, ((OperationCanceledException)ex).CancellationToken);
            }

            [Fact]
            public async Task Lookup_QueryDelayCanceledWithUnlimitedTimeout_Udp()
            {
                var client = new LookupClient(
                    new LookupClientOptions(DoesNotExist)
                    {
                        Timeout = Timeout.InfiniteTimeSpan,
                        UseTcpFallback = false
                    });

                // should hit the cancelation timeout, not the 1sec timeout
                var tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

                var token = tokenSource.Token;

                var ex = await Record.ExceptionAsync(() => client.QueryAsync("lala.com", QueryType.A, cancellationToken: token));
                Assert.True(ex is OperationCanceledException);
                Assert.Equal(token, ((OperationCanceledException)ex).CancellationToken);
            }

            [Fact]
            public async Task Lookup_QueryDelayCanceledWithUnlimitedTimeout_Tcp()
            {
                var client = new LookupClient(
                    new LookupClientOptions(DoesNotExist)
                    {
                        Timeout = Timeout.InfiniteTimeSpan,
                        UseTcpOnly = true
                    });

                // should hit the cancelation timeout, not the 1sec timeout
                var tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

                var token = tokenSource.Token;

                var ex = await Record.ExceptionAsync(() => client.QueryAsync("lala.com", QueryType.A, cancellationToken: token));
                Assert.True(ex is OperationCanceledException);
                Assert.Equal(token, ((OperationCanceledException)ex).CancellationToken);
            }
        }

        private async Task<IPAddress> GetDnsEntryAsync()
        {
            // retries the normal host name (without domain)
            var hostname = Dns.GetHostName();
            var hostIp = await Dns.GetHostAddressesAsync(hostname);

            // find the actual IP of the adapter used for inter networking
            var ip = hostIp.Where(p => p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).First();
            return ip;
        }

        [Fact]
        public async Task GetHostName()
        {
            var client = new LookupClient();
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

        [Fact]
        public async Task Lookup_Query_AAAA()
        {
            var client = new LookupClient();
            var result = await client.QueryAsync("google.com", QueryType.AAAA);

            Assert.NotEmpty(result.Answers.AaaaRecords());
            Assert.NotNull(result.Answers.AaaaRecords().First().Address);
        }

        [Fact]
        public void Lookup_Query_AAAA_Sync()
        {
            var client = new LookupClient();
            var result = client.Query("google.com", QueryType.AAAA);

            Assert.NotEmpty(result.Answers.AaaaRecords());
            Assert.NotNull(result.Answers.AaaaRecords().First().Address);
        }

        [Fact]
        public async Task Lookup_Query_Any()
        {
            var client = new LookupClient();
            var result = await client.QueryAsync("google.com", QueryType.ANY);

            Assert.NotEmpty(result.Answers);
            Assert.NotEmpty(result.Answers.ARecords());
            Assert.NotEmpty(result.Answers.NsRecords());
        }

        [Fact]
        public void Lookup_Query_Any_Sync()
        {
            var client = new LookupClient();
            var result = client.Query("google.com", QueryType.ANY);

            Assert.NotEmpty(result.Answers);
            Assert.NotEmpty(result.Answers.ARecords());
            Assert.NotEmpty(result.Answers.NsRecords());
        }

        [Fact]
        public async Task Lookup_Query_Mx()
        {
            var client = new LookupClient();
            var result = await client.QueryAsync("google.com", QueryType.MX);

            Assert.NotEmpty(result.Answers.MxRecords());
            Assert.NotNull(result.Answers.MxRecords().First().Exchange);
            Assert.True(result.Answers.MxRecords().First().Preference > 0);
        }

        [Fact]
        public void Lookup_Query_Mx_Sync()
        {
            var client = new LookupClient();
            var result = client.Query("google.com", QueryType.MX);

            Assert.NotEmpty(result.Answers.MxRecords());
            Assert.NotNull(result.Answers.MxRecords().First().Exchange);
            Assert.True(result.Answers.MxRecords().First().Preference > 0);
        }

        [Fact]
        public async Task Lookup_Query_NS()
        {
            var client = new LookupClient();
            var result = await client.QueryAsync("google.com", QueryType.NS);

            Assert.NotEmpty(result.Answers.NsRecords());
            Assert.NotNull(result.Answers.NsRecords().First().NSDName);
        }

        [Fact]
        public void Lookup_Query_NS_Sync()
        {
            var client = new LookupClient();
            var result = client.Query("google.com", QueryType.NS);

            Assert.NotEmpty(result.Answers.NsRecords());
            Assert.NotNull(result.Answers.NsRecords().First().NSDName);
        }

        [Fact]
        public async Task Lookup_Query_TXT()
        {
            var client = new LookupClient();
            var result = await client.QueryAsync("google.com", QueryType.TXT);

            Assert.NotEmpty(result.Answers.TxtRecords());
            Assert.NotEmpty(result.Answers.TxtRecords().First().EscapedText);
            Assert.NotEmpty(result.Answers.TxtRecords().First().Text);
        }

        [Fact]
        public void Lookup_Query_TXT_Sync()
        {
            var client = new LookupClient();
            var result = client.Query("google.com", QueryType.TXT);

            Assert.NotEmpty(result.Answers.TxtRecords());
            Assert.NotEmpty(result.Answers.TxtRecords().First().EscapedText);
            Assert.NotEmpty(result.Answers.TxtRecords().First().Text);
        }

        [Fact]
        public async Task Lookup_Query_SOA()
        {
            var client = new LookupClient();
            var result = await client.QueryAsync("google.com", QueryType.SOA);

            Assert.NotEmpty(result.Answers.SoaRecords());
            Assert.NotNull(result.Answers.SoaRecords().First().MName);
            Assert.NotNull(result.Answers.SoaRecords().First().RName);
        }

        [Fact]
        public void Lookup_Query_SOA_Sync()
        {
            var client = new LookupClient();
            var result = client.Query("google.com", QueryType.SOA);

            Assert.NotEmpty(result.Answers.SoaRecords());
            Assert.NotNull(result.Answers.SoaRecords().First().MName);
            Assert.NotNull(result.Answers.SoaRecords().First().RName);
        }

        [Fact]
        public async Task Lookup_Query_Puny()
        {
            var client = new LookupClient(IPAddress.Parse("8.8.8.8"));
            var result = await client.QueryAsync("müsli.de", QueryType.A);

            Assert.NotEmpty(result.Answers);
            Assert.NotEmpty(result.Answers.ARecords());
        }

        [Fact]
        public void Lookup_Query_Puny_Sync()
        {
            var client = new LookupClient(IPAddress.Parse("8.8.8.8"));
            var result = client.Query("müsli.de", QueryType.A);

            Assert.NotEmpty(result.Answers);
            Assert.NotEmpty(result.Answers.ARecords());
        }

        [Fact]
        public void Lookup_Query_Puny2()
        {
            var client = new LookupClient();
            var result = client.Query("müsli.com", QueryType.ANY);

            Assert.NotEmpty(result.Answers);
            Assert.NotEmpty(result.Answers.ARecords());
        }

        [Fact]
        public void Ip_Arpa_v4_Valid()
        {
            var ip = IPAddress.Parse("127.0.0.1");
            var client = new LookupClient();

            var result = DnsString.Parse(ip.GetArpaName());
            var queryResult = client.QueryReverse(ip);

            Assert.Equal("1.0.0.127.in-addr.arpa.", result);
            Assert.Equal("localhost.", queryResult.Answers.PtrRecords().First().PtrDomainName);
        }

        [Fact]
        public void Ip_Arpa_v6_Valid()
        {
            var ip = NameServer.GooglePublicDns2IPv6.Address;
            var client = new LookupClient();

            var result = DnsString.Parse(ip.GetArpaName());
            var queryResult = client.QueryReverse(ip);

            Assert.Equal("8.8.8.8.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.6.8.4.0.6.8.4.1.0.0.2.ip6.arpa.", result);
            Assert.Contains("dns.google", queryResult.Answers.PtrRecords().First().PtrDomainName);
        }

        [Fact]
        public async Task GetHostEntry_ExampleSub()
        {
            var client = new LookupClient();
            var hostEntry = await client.GetHostEntryAsync("mail.google.com");
            Assert.EndsWith("google.com", hostEntry.Aliases.First(), StringComparison.OrdinalIgnoreCase);
            Assert.Equal("mail.google.com", hostEntry.HostName);
            Assert.True(hostEntry.AddressList.Length > 0);
        }

        [Fact]
        public void GetHostEntry_ByName_ManyIps_NoAlias()
        {
            var client = new LookupClient();

            var result = client.GetHostEntry("google.com");

            Assert.True(result.AddressList.Length > 1);
            Assert.True(result.Aliases.Length == 0);
            Assert.Equal("google.com", result.HostName);
        }

        [Fact]
        public void GetHostEntry_ByName_OneIp_ManyAliases()
        {
            var client = new LookupClient(new LookupClientOptions()
            {
                ThrowDnsErrors = true
            });

            var result = client.GetHostEntry("dnsclient.michaco.net");

            Assert.True(result.AddressList.Length >= 1);
            Assert.True(result.Aliases.Length > 1);
            Assert.Equal("dnsclient.michaco.net", result.HostName);
        }

        [Fact]
        public void GetHostEntry_ByName_OneIp_NoAlias()
        {
            var client = new LookupClient();

            var result = client.GetHostEntry("localhost");

            Assert.True(result.AddressList.Length == 1);
            Assert.True(result.Aliases.Length == 0);
            Assert.Equal("localhost", result.HostName);
        }

        [Fact]
        public void GetHostEntry_ByName_EmptyString()
        {
            var client = new LookupClient();

            Action act = () => client.GetHostEntry("");

            var ex = Record.Exception(act);
            Assert.NotNull(ex);
            Assert.Contains("hostNameOrAddress", ex.Message);
        }

        [Fact]
        public void GetHostEntry_ByName_HostDoesNotExist()
        {
            var client = new LookupClient();

            var result = client.GetHostEntry("lolhost");

            Assert.True(result.AddressList.Length == 0);
            Assert.True(result.Aliases.Length == 0);
            Assert.Equal("lolhost", result.HostName);
        }

        [Fact]
        public void GetHostEntry_ByName_HostDoesNotExist_WithThrow()
        {
            var client = new LookupClient(
                new LookupClientOptions()
                {
                    ThrowDnsErrors = true
                });

            Action act = () => client.GetHostEntry("lolhost");

            var ex = Record.Exception(act) as DnsResponseException;
            Assert.NotNull(ex);
            Assert.Equal(DnsResponseCode.NotExistentDomain, ex.Code);
        }

        [Fact]
        public void GetHostEntry_ByIp_NoHost()
        {
            var client = new LookupClient();

            var result = client.GetHostEntry("1.0.0.0");

            Assert.Null(result);
        }

        [Fact]
        public void GetHostEntry_ByIp_NoHost_WithThrow()
        {
            var client = new LookupClient(
                new LookupClientOptions()
                {
                    ThrowDnsErrors = true
                });

            Action act = () => client.GetHostEntry("1.0.0.0");

            var ex = Record.Exception(act) as DnsResponseException;

            Assert.NotNull(ex);
            Assert.Equal(DnsResponseCode.NotExistentDomain, ex.Code);
        }

        [Fact]
        public void GetHostEntry_ByIp()
        {
            var source = Dns.GetHostEntry("localhost");
            Assert.NotNull(source);

            var client = new LookupClient();
            var result = client.GetHostEntry(source.AddressList.First());

            Assert.NotNull(result);
            Assert.True(result.AddressList.Length >= 1);
        }

        [Fact]
        public void GetHostEntry_ByManyIps()
        {
            var client = new LookupClient(new LookupClientOptions()
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
            var client = new LookupClient();

            var result = await client.GetHostEntryAsync("google.com");

            Assert.True(result.AddressList.Length > 1);
            Assert.True(result.Aliases.Length == 0);
            Assert.Equal("google.com", result.HostName);
        }

        [Fact]
        public async Task GetHostEntryAsync_ByName_OneIp_ManyAliases()
        {
            var client = new LookupClient();

            var result = await client.GetHostEntryAsync("dnsclient.michaco.net");

            Assert.True(result.AddressList.Length == 1);
            Assert.True(result.Aliases.Length > 1);
            Assert.Equal("dnsclient.michaco.net", result.HostName);
        }

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
        public async Task GetHostEntryAsync_ByName_EmptyString()
        {
            var client = new LookupClient();

            Func<Task> act = async () => await client.GetHostEntryAsync("");

            var ex = await Record.ExceptionAsync(act);
            Assert.NotNull(ex);
            Assert.Contains("hostNameOrAddress", ex.Message);
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
        public async Task GetHostEntryAsync_ByName_HostDoesNotExist_WithThrow()
        {
            var client = new LookupClient(
                new LookupClientOptions()
                {
                    ThrowDnsErrors = true
                });

            Func<Task> act = async () => await client.GetHostEntryAsync("lolhost");

            var ex = await Record.ExceptionAsync(act) as DnsResponseException;
            Assert.NotNull(ex);
            Assert.Equal(DnsResponseCode.NotExistentDomain, ex.Code);
        }

        [Fact]
        public async Task GetHostEntryAsync_ByIp_NoHost()
        {
            var client = new LookupClient();

            var result = await client.GetHostEntryAsync("1.0.0.0");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetHostEntryAsync_ByIp_NoHost_WithThrow()
        {
            var client = new LookupClient(
                new LookupClientOptions()
                {
                    ThrowDnsErrors = true
                });

            Func<Task> act = async () => await client.GetHostEntryAsync("1.0.0.0");

            var ex = await Record.ExceptionAsync(act) as DnsResponseException;

            Assert.NotNull(ex);
            Assert.Equal(DnsResponseCode.NotExistentDomain, ex.Code);
        }

        [Fact]
        public async Task GetHostEntryAsync_ByIp()
        {
            var source = await Dns.GetHostEntryAsync("localhost");
            Assert.NotNull(source);

            var client = new LookupClient();
            var result = await client.GetHostEntryAsync(source.AddressList.First());

            Assert.NotNull(result);
            Assert.True(result.AddressList.Length >= 1);
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

                Assert.NotNull(ipAddress);
                var result = await client.GetHostEntryAsync(ipAddress);

                Assert.NotNull(result);
                Assert.True(result.AddressList.Length >= 1, "Revers should have found at least one ip");
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
        public void Lookup_SettingsFallback_UseClientsServers()
        {
            var client = new LookupClient(NameServer.CloudflareIPv6);

            // Test that the settings in the end has the name servers configured on the client above and
            // still the settings provided apart from the servers (everything else will not fallback to the client's settings...)
            var settings = client.GetSettings(queryOptions: new DnsQueryAndServerOptions(resolveNameServers: false)
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
        public void Lookup_SettingsFallback_KeepResolvedServers()
        {
            var client = new LookupClient(NameServer.CloudflareIPv6);

            // Should keep all the provided settings including the resolved servers.
            var settings = client.GetSettings(queryOptions: new DnsQueryAndServerOptions(resolveNameServers: true));

            Assert.NotEqual(client.Settings, settings);
            Assert.NotEqual(client.NameServers, settings.NameServers);
        }

        [Fact]
        public void Lookup_SettingsFallback_KeepResolvedServers_EvenIfEmpty()
        {
            var client = new LookupClient(NameServer.CloudflareIPv6);

            // Trick the logic to think it was using auto resolve and should prefer these servers but then there are none
            var options = new DnsQueryAndServerOptions(resolveNameServers: true);
            options.NameServers.Clear();
            var settings = client.GetSettings(queryOptions: options);

            Assert.NotEqual(client.Settings, settings);
            Assert.NotEqual(client.NameServers, settings.NameServers);
            Assert.Empty(settings.NameServers);
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
    }
}