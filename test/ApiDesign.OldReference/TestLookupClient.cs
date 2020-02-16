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

        public void TestQuery()
        {
            _client.Query("query", QueryType.A);
            _client.Query("query", QueryType.A, QueryClass.IN);
        }

        public async Task TestQueryAsync()
        {
            await _client.QueryAsync("query", QueryType.A);
            await _client.QueryAsync("query", QueryType.A, QueryClass.IN);
            await _client.QueryAsync("query", QueryType.A, cancellationToken: default(CancellationToken));
            await _client.QueryAsync("query", QueryType.A, QueryClass.IN, default(CancellationToken));
        }
    }
}