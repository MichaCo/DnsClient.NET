using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DnsClient.Protocol;
using Xunit;

namespace DnsClient.Tests
{
    public class LookupTest
    {
        private static readonly IPAddress DoesNotExist = IPAddress.Parse("192.0.21.43");

        [Fact]
        public void Lookup_Defaults()
        {
            var client = new LookupClient();

            Assert.True(client.UseCache);
            Assert.False(client.EnableAuditTrail);
            Assert.Null(client.MimimumCacheTimeout);
            Assert.True(client.Recursion);
            Assert.False(client.ThrowDnsErrors);
            Assert.Equal(client.Retries, 5);
            Assert.Equal(client.Timeout, TimeSpan.FromSeconds(5));
            Assert.True(client.UseTcpFallback);
            Assert.False(client.UseTcpOnly);
        }

        [Fact]
        public void Lookup_Query_InvalidTimeout()
        {
            var client = new LookupClient();

            Action act = () => client.Timeout = TimeSpan.FromMilliseconds(0);

            Assert.ThrowsAny<ArgumentOutOfRangeException>(act);
        }

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
            var client = new LookupClient() { Timeout = TimeSpan.FromMilliseconds(500) };

            client.EnableAuditTrail = true;
            var result = await client.QueryAsync("127.0.0.1", QueryType.A);

            Assert.Equal(QueryClass.IN, result.Questions.First().QuestionClass);
            Assert.Equal(QueryType.A, result.Questions.First().QuestionType);
            Assert.True(result.Header.AnswerCount == 0);
        }

        [Fact]
        public void Lookup_GetHostAddresses_LocalReverse_NoResult_Sync()
        {
            // expecting no result as reverse lookup must be explicit
            var client = new LookupClient() { Timeout = TimeSpan.FromMilliseconds(500) };

            client.EnableAuditTrail = true;
            var result = client.Query("127.0.0.1", QueryType.A);

            Assert.Equal(QueryClass.IN, result.Questions.First().QuestionClass);
            Assert.Equal(QueryType.A, result.Questions.First().QuestionType);
            Assert.True(result.Header.AnswerCount == 0);
        }

#if ENABLE_REMOTE_DNS

        [Fact]
        public void Lookup_IPv4_Works()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);

            // force both requests
            client.UseCache = false;

            // both should use the same server/udp client
            var resultA = client.Query("google.com", QueryType.ANY);
            var resultB = client.Query("google.com", QueryType.ANY);

            Assert.Equal(NameServer.GooglePublicDns, resultA.NameServer.Endpoint);
            Assert.Equal(NameServer.GooglePublicDns, resultB.NameServer.Endpoint);
            Assert.True(resultA.Answers.Count > 0);
            Assert.True(resultB.Answers.Count > 0);
        }

        [Fact]
        public void Lookup_IPv4_TcpOnly_Works()
        {
            var client = new LookupClient(NameServer.GooglePublicDns);

            // force both requests
            client.UseCache = false;
            client.UseTcpOnly = true;

            // both should use the same server/udp client
            var resultA = client.Query("google.com", QueryType.ANY);
            var resultB = client.Query("google.com", QueryType.ANY);

            Assert.Equal(NameServer.GooglePublicDns, resultA.NameServer.Endpoint);
            Assert.Equal(NameServer.GooglePublicDns, resultB.NameServer.Endpoint);
            Assert.True(resultA.Answers.Count > 0);
            Assert.True(resultB.Answers.Count > 0);
        }

        [Fact]
        public void Lookup_IPv6_Works()
        {
            var client = new LookupClient(NameServer.GooglePublicDnsIPv6);

            // force both requests
            client.UseCache = false;

            // both should use the same server/udp client
            var resultA = client.Query("google.com", QueryType.ANY);
            var resultB = client.Query("google.com", QueryType.ANY);

            Assert.Equal(NameServer.GooglePublicDnsIPv6, resultA.NameServer.Endpoint);
            Assert.Equal(NameServer.GooglePublicDnsIPv6, resultB.NameServer.Endpoint);
            Assert.True(resultA.Answers.Count > 0);
            Assert.True(resultB.Answers.Count > 0);
        }

        [Fact]
        public void Lookup_IPv6_TcpOnly_Works()
        {
            var client = new LookupClient(NameServer.GooglePublicDnsIPv6);

            // force both requests
            client.UseCache = false;
            client.UseTcpOnly = true;

            // both should use the same server/udp client
            var resultA = client.Query("google.com", QueryType.ANY);
            var resultB = client.Query("google.com", QueryType.ANY);

            Assert.Equal(NameServer.GooglePublicDnsIPv6, resultA.NameServer.Endpoint);
            Assert.Equal(NameServer.GooglePublicDnsIPv6, resultB.NameServer.Endpoint);
            Assert.True(resultA.Answers.Count > 0);
            Assert.True(resultB.Answers.Count > 0);
        }

        [Fact]
        public void Lookup_MultiServer_IPv4_and_IPv6()
        {
            var client = new LookupClient(NameServer.GooglePublicDns, NameServer.GooglePublicDnsIPv6);

            // force both requests
            client.UseCache = false;

            // server rotation should use both defined dns servers
            var resultA = client.Query("google.com", QueryType.ANY);
            var resultB = client.Query("google.com", QueryType.ANY);

            Assert.Equal(NameServer.GooglePublicDnsIPv6, resultA.NameServer.Endpoint);
            Assert.Equal(NameServer.GooglePublicDns, resultB.NameServer.Endpoint);
            Assert.True(resultA.Answers.Count > 0);
            Assert.True(resultB.Answers.Count > 0);
        }

        [Fact]
        public void Lookup_MultiServer_IPv4_and_IPv6_TCP()
        {
            var client = new LookupClient(NameServer.GooglePublicDns, NameServer.GooglePublicDnsIPv6);

            // force both requests
            client.UseCache = false;
            client.UseTcpOnly = true;

            // server rotation should use both defined dns servers
            var resultA = client.Query("google.com", QueryType.ANY);
            var resultB = client.Query("google.com", QueryType.ANY);

            Assert.Equal(NameServer.GooglePublicDnsIPv6, resultA.NameServer.Endpoint);
            Assert.Equal(NameServer.GooglePublicDns, resultB.NameServer.Endpoint);
            Assert.True(resultA.Answers.Count > 0);
            Assert.True(resultB.Answers.Count > 0);
        }

#endif

        [Fact]
        public void Lookup_ThrowDnsErrors()
        {
            var client = new LookupClient();
            client.ThrowDnsErrors = true;

            Action act = () => client.QueryAsync("lalacom", (QueryType)12345).GetAwaiter().GetResult();

            var ex = Record.Exception(act) as DnsResponseException;

            Assert.Equal(ex.Code, DnsResponseCode.NotExistentDomain);
        }

        [Fact]
        public void Lookup_ThrowDnsErrors_Sync()
        {
            var client = new LookupClient();
            client.ThrowDnsErrors = true;

            Action act = () => client.Query("lalacom", (QueryType)12345);

            var ex = Record.Exception(act) as DnsResponseException;

            Assert.Equal(ex.Code, DnsResponseCode.NotExistentDomain);
        }

        public class QueryTimesOutTests
        {
            [Fact]
            public async Task Lookup_QueryTimesOut_Udp_Async()
            {
                var client = new LookupClient(DoesNotExist);
                client.Timeout = TimeSpan.FromMilliseconds(200);
                client.Retries = 0;
                client.UseTcpFallback = false;

                var exe = await Record.ExceptionAsync(() => client.QueryAsync("lala.com", QueryType.A));

                var ex = exe as DnsResponseException;
                Assert.NotNull(ex);
                Assert.Equal(DnsResponseCode.ConnectionTimeout, ex.Code);
                Assert.Contains("No connection", ex.Message);
            }

            [Fact]
            public void Lookup_QueryTimesOut_Udp_Sync()
            {
                var client = new LookupClient(DoesNotExist);
                client.Timeout = TimeSpan.FromMilliseconds(200);
                client.Retries = 0;
                client.UseTcpFallback = false;

                var exe = Record.Exception(() => client.Query("lala.com", QueryType.A));

                var ex = exe as DnsResponseException;
                Assert.NotNull(ex);
                Assert.Equal(DnsResponseCode.ConnectionTimeout, ex.Code);
                Assert.Contains("No connection", ex.Message);
            }

            [Fact]
            public async Task Lookup_QueryTimesOut_Tcp_Async()
            {
                var client = new LookupClient(DoesNotExist);
                client.Timeout = TimeSpan.FromMilliseconds(200);
                client.Retries = 0;
                client.UseTcpOnly = true;

                var exe = await Record.ExceptionAsync(() => client.QueryAsync("lala.com", QueryType.A));

                var ex = exe as DnsResponseException;
                Assert.NotNull(ex);
                Assert.Equal(DnsResponseCode.ConnectionTimeout, ex.Code);
                Assert.Contains("No connection", ex.Message);
            }

            [Fact]
            public void Lookup_QueryTimesOut_Tcp_Sync()
            {
                var client = new LookupClient(DoesNotExist);
                client.Timeout = TimeSpan.FromMilliseconds(200);
                client.Retries = 0;
                client.UseTcpOnly = true;

                var exe = Record.Exception(() => client.Query("lala.com", QueryType.A));

                var ex = exe as DnsResponseException;
                Assert.NotNull(ex);
                Assert.Equal(DnsResponseCode.ConnectionTimeout, ex.Code);
                Assert.Contains("No connection", ex.Message);
            }
        }

        [Fact]
        public void Lookup_QueryCanceled_Udp()
        {
            var client = new LookupClient();
            client.UseTcpFallback = false;

            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            Action act = () => client.QueryAsync("lala.com", QueryType.A, token).GetAwaiter().GetResult();
            tokenSource.Cancel();

            var ex = Record.Exception(act) as OperationCanceledException;

            Assert.NotNull(ex);
            Assert.Equal(ex.CancellationToken, token);
        }

        [Fact]
        public void Lookup_QueryCanceled_Tcp()
        {
            var client = new LookupClient();
            client.UseTcpOnly = true;

            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            Action act = () => client.QueryAsync("lala.com", QueryType.A, token).GetAwaiter().GetResult();
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
                var client = new LookupClient(DoesNotExist);
                client.Timeout = TimeSpan.FromMilliseconds(1000);
                client.UseTcpFallback = false;

                // should hit the cancelation timeout, not the 1sec timeout
                var tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

                var token = tokenSource.Token;

                var ex = await Record.ExceptionAsync(() => client.QueryAsync("lala.com", QueryType.A, token));
                Assert.True(ex is OperationCanceledException);
                Assert.Equal(token, ((OperationCanceledException)ex).CancellationToken);
            }

            [Fact]
            public async Task Lookup_QueryDelayCanceled_Tcp()
            {
                var client = new LookupClient(DoesNotExist);
                client.Timeout = TimeSpan.FromMilliseconds(1000);
                client.UseTcpOnly = true;

                // should hit the cancelation timeout, not the 1sec timeout
                var tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

                var token = tokenSource.Token;

                var ex = await Record.ExceptionAsync(() => client.QueryAsync("lala.com", QueryType.A, token));
                Assert.True(ex is OperationCanceledException);
                Assert.Equal(token, ((OperationCanceledException)ex).CancellationToken);
            }

            [Fact]
            public async Task Lookup_QueryDelayCanceledWithUnlimitedTimeout_Udp()
            {
                var client = new LookupClient(DoesNotExist);
                client.Timeout = Timeout.InfiniteTimeSpan;
                client.UseTcpFallback = false;

                // should hit the cancelation timeout, not the 1sec timeout
                var tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

                var token = tokenSource.Token;

                var ex = await Record.ExceptionAsync(() => client.QueryAsync("lala.com", QueryType.A, token));
                Assert.True(ex is OperationCanceledException);
                Assert.Equal(token, ((OperationCanceledException)ex).CancellationToken);
            }

            [Fact]
            public async Task Lookup_QueryDelayCanceledWithUnlimitedTimeout_Tcp()
            {
                var client = new LookupClient(DoesNotExist);
                client.Timeout = Timeout.InfiniteTimeSpan;
                client.UseTcpOnly = true;

                // should hit the cancelation timeout, not the 1sec timeout
                var tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

                var token = tokenSource.Token;

                var ex = await Record.ExceptionAsync(() => client.QueryAsync("lala.com", QueryType.A, token));
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
        public async Task Lookup_Reverse()
        {
            var client = new LookupClient();
            var result = await client.QueryReverseAsync(IPAddress.Parse("127.0.0.1"));

            Assert.Equal(result.Answers.PtrRecords().First().PtrDomainName.Value, "localhost.");
        }

        [Fact]
        public void Lookup_ReverseSync()
        {
            var client = new LookupClient();
            var result = client.QueryReverse(IPAddress.Parse("127.0.0.1"));

            Assert.Equal(result.Answers.PtrRecords().First().PtrDomainName.Value, "localhost.");
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
        public void Lookup_Query_InvalidPuny()
        {
            var client = new LookupClient(IPAddress.Parse("8.8.8.8"));

            Func<IDnsQueryResponse> act = () => client.QueryAsync("müsliiscool!.de", QueryType.A).Result;

            Assert.ThrowsAny<ArgumentException>(act);
        }

        [Fact]
        public void Ip_Arpa_v4_Valid()
        {
            var ip = IPAddress.Parse("127.0.0.1");
            var client = new LookupClient();

            var result = DnsString.ParseQueryString(ip.GetArpaName());
            var queryResult = client.QueryReverse(ip);

            Assert.Equal("1.0.0.127.in-addr.arpa.", result);
            Assert.Equal("localhost.", queryResult.Answers.PtrRecords().First().PtrDomainName);
        }

        [Fact]
        public void Ip_Arpa_v6_Valid()
        {
            var ip = NameServer.GooglePublicDns2IPv6.Address;
            var client = new LookupClient();

            var result = DnsString.ParseQueryString(ip.GetArpaName());
            var queryResult = client.QueryReverse(ip);

            Assert.Equal("8.8.8.8.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.6.8.4.0.6.8.4.1.0.0.2.ip6.arpa.", result);
            Assert.Contains("google-public-dns", queryResult.Answers.PtrRecords().First().PtrDomainName);
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
            var client = new LookupClient();

            var result = client.GetHostEntry("dnsclient.michaco.net");

            Assert.True(result.AddressList.Length == 1);
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
            var client = new LookupClient();
            client.ThrowDnsErrors = true;

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
            var client = new LookupClient();
            client.ThrowDnsErrors = true;

            Action act = () => client.GetHostEntry("1.0.0.0");

            var ex = Record.Exception(act) as DnsResponseException;

            Assert.NotNull(ex);
            Assert.Equal(DnsResponseCode.NotExistentDomain, ex.Code);
        }

#if ENABLE_REMOTE_DNS

        [Fact]
        public void GetHostEntry_ByIp()
        {
            var source = Dns.GetHostEntryAsync("localhost").Result;
            var client = new LookupClient();
            var result = client.GetHostEntry(source.AddressList.First());

            Assert.True(result.AddressList.Length == 1);
            Assert.True(result.Aliases.Length == 0);
        }

#endif

        [Fact]
        public void GetHostEntry_ByManyIps()
        {
            var client = new LookupClient();
            var nsServers = client.Query("google.com", QueryType.NS).Answers.NsRecords().ToArray();

            Assert.True(nsServers.Length > 0);

            foreach (var server in nsServers)
            {
                var ipAddress = client.GetHostEntry(server.NSDName).AddressList.First();
                var result = client.GetHostEntry(ipAddress);

                Assert.True(result.AddressList.Length >= 1);
                Assert.True(result.AddressList.Contains(ipAddress));
                Assert.True(result.Aliases.Length == 0);

                // expecting always the name without . at the end!
                Assert.Equal(server.NSDName.Value.Substring(0, server.NSDName.Value.Length - 1), result.HostName);
            }
        }
    }
}