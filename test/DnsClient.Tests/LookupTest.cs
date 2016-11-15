using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DnsClient.Tests
{
    public class LookupTest
    {
        private static ILoggerFactory LoggerFactory = new LoggerFactory()
            .AddConsole(LogLevel.Debug)
            .AddDebug(LogLevel.Debug);

        private ILogger Logger { get; } = LoggerFactory.CreateLogger<LookupTest>();

        private async Task<IPHostEntry> GetDnsEntryAsync()
        {
            // retries the normal host name (without domain)
            var hostname = Dns.GetHostName();
            var hostIp = await Dns.GetHostAddressesAsync(hostname);

            // find the actual IP of the adapter used for inter networking
            var ip = hostIp.Where(p => p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).First();

            // get the entry which contains the full domain qualified host name
            var entry = await Dns.GetHostEntryAsync(ip);

            Logger.LogDebug("Dns ReverseEntry {0} {1}.", entry.AddressList.First(), entry.HostName);

            return entry;
        }

        [Theory]
        [InlineData(TransportType.Tcp)]
        [InlineData(TransportType.Udp)]
        public async Task Lookup_GetHostAddresses_Local(TransportType transport)
        {
            var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { TransportType = transport });
            var result = await client.GetHostAddressesAsync("localhost");

            Assert.Equal("127.0.0.1", result.First().ToString());
        }

        [Theory]
        [InlineData(TransportType.Tcp)]
        [InlineData(TransportType.Udp)]
        public async Task Lookup_GetHostAddresses_ActualHost(TransportType transport)
        {
            var entry = await GetDnsEntryAsync();

            var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { TransportType = transport });
            var result = await client.GetHostAddressesAsync(entry.HostName);

            Assert.True(entry.AddressList.Contains(result.First()));
        }

        [Theory]
        [InlineData(TransportType.Tcp)]
        [InlineData(TransportType.Udp)]
        public async Task Lookup_GetHostEntryAsync_ByIp(TransportType transport)
        {
            var entry = await GetDnsEntryAsync();
            var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { TransportType = transport });
            var result = await client.GetHostEntryAsync(entry.AddressList.First());

            Assert.True(entry.AddressList.Contains(result.AddressList.First()));
            Assert.Equal(entry.HostName, result.HostName);
        }

        [Theory]
        [InlineData(TransportType.Tcp)]
        [InlineData(TransportType.Udp)]
        public async Task Lookup_GetHostEntryAsync_ByName(TransportType transport)
        {
            var entry = await GetDnsEntryAsync();
            var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { TransportType = transport });
            var result = await client.GetHostEntryAsync(entry.HostName);

            Assert.True(entry.AddressList.Contains(result.AddressList.First()));
            Assert.Equal(entry.HostName, result.HostName);
        }

        [Theory]
        [InlineData(TransportType.Tcp)]
        [InlineData(TransportType.Udp)]
        public async Task Lookup_Query_A(TransportType transport)
        {
            var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { TransportType = transport });
            var result = await client.QueryAsync("google.com", QType.A);

            Assert.True(result.Answers.Count > 0);
            Assert.True(result.RecordsA.Count > 0);
        }

        [Theory]
        [InlineData(TransportType.Tcp)]
        [InlineData(TransportType.Udp)]
        public async Task Lookup_Query_AAAA(TransportType transport)
        {
            var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { TransportType = transport });
            var result = await client.QueryAsync("google.com", QType.AAAA);

            Assert.True(result.Answers.Count > 0);
            Assert.True(result.RecordsAAAA.Count > 0);
        }

        [Theory]
        [InlineData(TransportType.Tcp)]
        [InlineData(TransportType.Udp)]
        public async Task Lookup_Query_Any(TransportType transport)
        {
            var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { TransportType = transport });
            var result = await client.QueryAsync("google.com", QType.ANY);

            Assert.True(result.Answers.Count > 5);
            Assert.True(result.RecordsA.Count > 0);
            Assert.True(result.RecordsAAAA.Count > 0);
            Assert.True(result.RecordsMX.Count > 0);
        }

        [Theory]
        [InlineData(TransportType.Tcp)]
        [InlineData(TransportType.Udp)]
        public async Task Lookup_Query_Mx(TransportType transport)
        {
            var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { TransportType = transport });
            var result = await client.QueryAsync("google.com", QType.MX);

            Assert.True(result.Answers.Count > 0);
            Assert.True(result.RecordsMX.Count > 0);
        }

        [Theory]
        [InlineData(TransportType.Tcp)]
        [InlineData(TransportType.Udp)]
        public async Task Lookup_Query_NS(TransportType transport)
        {
            var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { TransportType = transport });
            var result = await client.QueryAsync("google.com", QType.NS);

            Assert.True(result.Answers.Count > 0);
            Assert.True(result.RecordsNS.Count > 0);
        }

        [Theory]
        [InlineData(TransportType.Tcp)]
        [InlineData(TransportType.Udp)]
        public async Task Lookup_Query_TXT(TransportType transport)
        {
            var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { TransportType = transport });
            var result = await client.QueryAsync("google.com", QType.TXT);

            Assert.True(result.Answers.Count > 0);
            Assert.True(result.RecordsTXT.Count > 0);
        }

        [Theory]
        [InlineData(TransportType.Tcp)]
        [InlineData(TransportType.Udp)]
        public async Task Lookup_Query_SOA(TransportType transport)
        {
            var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { TransportType = transport });
            var result = await client.QueryAsync("google.com", QType.SOA);

            Assert.True(result.Answers.Count > 0);
            Assert.True(result.RecordsSOA.Count > 0);
        }

        [Theory]
        [InlineData(TransportType.Tcp)]
        [InlineData(TransportType.Udp)]
        public async Task Lookup_Query_ForceTimeout(TransportType transport)
        {
            // basically testing we don't throw an error but return information
            var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { Timeout = 0, TransportType = transport });
            var result = await client.QueryAsync("google.com", QType.ANY);
            Assert.True(!string.IsNullOrWhiteSpace(result.Error));
            Assert.True(result.Answers.Count == 0);
        }
    }
}