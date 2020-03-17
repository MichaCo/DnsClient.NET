using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DnsClient;

namespace ApiDesign.OldReference
{
    public class TestLookupClient
    {
        private readonly LookupClient _client;

        public TestLookupClient()
        {
            _client = new LookupClient(IPAddress.Loopback, IPAddress.Loopback, IPAddress.Loopback);
            _client = new LookupClient(IPAddress.Loopback, 5465);
            _client = new LookupClient(new IPEndPoint(IPAddress.Loopback, 4444), new IPEndPoint(IPAddress.Loopback, 4443), new IPEndPoint(IPAddress.Loopback, 4442));
            _client = new LookupClient();
        }

        public void SetNonDefaults()
        {
            _client.ContinueOnDnsError = false;
            _client.EnableAuditTrail = true;
            _client.MinimumCacheTimeout = TimeSpan.FromSeconds(11);
            _client.Recursion = false;
            _client.Retries = 10;
            _client.ThrowDnsErrors = true;
            _client.Timeout = TimeSpan.FromMinutes(1);
            _client.UseCache = false;
            _client.UseRandomNameServer = false;
            _client.UseTcpFallback = false;
            _client.UseTcpOnly = true;
        }

        public ILookupClient Client => _client;

        public void TestQuery()
        {
            _client.Query("domain", QueryType.A);
            _client.Query("domain", QueryType.A, QueryClass.IN);
            _client.QueryReverse(IPAddress.Loopback);

            _client.QueryServer(new[] { NameServer.GooglePublicDns.Address }, "domain", QueryType.A);
            _client.QueryServer(new[] { NameServer.GooglePublicDns.Address }, "domain", QueryType.A, QueryClass.IN);
            _client.QueryServerReverse(new[] { NameServer.GooglePublicDns.Address }, IPAddress.Loopback);

            _client.QueryServer(new[] { NameServer.GooglePublicDns }, "domain", QueryType.A);
            _client.QueryServer(new[] { NameServer.GooglePublicDns }, "domain", QueryType.A, QueryClass.IN);
            _client.QueryServerReverse(new[] { NameServer.GooglePublicDns }, IPAddress.Loopback);

            _client.GetHostEntry(IPAddress.Loopback);
            _client.GetHostName(IPAddress.Loopback);
            _client.ResolveService("domain", "srv", tag: null);
        }

        public async Task TestQueryAsync()
        {
            await _client.QueryAsync("domain", QueryType.A);
            await _client.QueryAsync("domain", QueryType.A, QueryClass.IN);
            await _client.QueryAsync("domain", QueryType.A, cancellationToken: default(CancellationToken));
            await _client.QueryAsync("domain", QueryType.A, QueryClass.IN, default(CancellationToken));

            await _client.QueryReverseAsync(IPAddress.Loopback);
            await _client.QueryReverseAsync(IPAddress.Loopback, default(CancellationToken));

            await _client.QueryServerAsync(new[] { NameServer.GooglePublicDns.Address }, "domain", QueryType.A);
            await _client.QueryServerAsync(new[] { NameServer.GooglePublicDns.Address }, "domain", QueryType.A, cancellationToken: default(CancellationToken));
            await _client.QueryServerAsync(new[] { NameServer.GooglePublicDns.Address }, "domain", QueryType.A, QueryClass.IN);
            await _client.QueryServerAsync(new[] { NameServer.GooglePublicDns.Address }, "domain", QueryType.A, QueryClass.IN, default(CancellationToken));
            await _client.QueryServerReverseAsync(new[] { NameServer.GooglePublicDns.Address }, IPAddress.Loopback);
            await _client.QueryServerReverseAsync(new[] { NameServer.GooglePublicDns.Address }, IPAddress.Loopback, default(CancellationToken));

            await _client.QueryServerAsync(new[] { NameServer.GooglePublicDns }, "domain", QueryType.A);
            await _client.QueryServerAsync(new[] { NameServer.GooglePublicDns }, "domain", QueryType.A, cancellationToken: default(CancellationToken));
            await _client.QueryServerAsync(new[] { NameServer.GooglePublicDns }, "domain", QueryType.A, QueryClass.IN);
            await _client.QueryServerAsync(new[] { NameServer.GooglePublicDns }, "domain", QueryType.A, QueryClass.IN, default(CancellationToken));
            await _client.QueryServerReverseAsync(new[] { NameServer.GooglePublicDns }, IPAddress.Loopback);
            await _client.QueryServerReverseAsync(new[] { NameServer.GooglePublicDns }, IPAddress.Loopback, default(CancellationToken));

            await _client.GetHostEntryAsync(IPAddress.Loopback);
            await _client.GetHostEntryAsync("localhost");
            await _client.GetHostNameAsync(IPAddress.Loopback);
            await _client.ResolveServiceAsync("domain", "srv", tag: null);
        }
    }
}