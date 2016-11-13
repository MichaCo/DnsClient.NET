using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DnsClient.Tests
{
    public class ClientTest
    {
        static ILoggerFactory LoggerFactory = new LoggerFactory()
            .AddConsole(LogLevel.Debug)
            .AddDebug(LogLevel.Debug);

        private ILogger Logger { get; } = LoggerFactory.CreateLogger<ClientTest>();

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

        [Fact]
        public async Task Client_GetHostAddresses_Local()
        {
            var client = new Client(LoggerFactory, new DnsClientOptions());
            var result = await client.GetHostAddressesAsync("localhost");

            Assert.Equal("127.0.0.1", result.First().ToString());
        }

        [Fact]
        public async Task Client_GetHostAddresses_ActualHost()
        {
            var entry = await GetDnsEntryAsync();

            var client = new Client(LoggerFactory, new DnsClientOptions());
            var result = await client.GetHostAddressesAsync(entry.HostName);

            Assert.True(entry.AddressList.Contains(result.First()));
        }

        [Fact]
        public async Task Client_GetHostEntryAsync_ByIp()
        {
            var entry = await GetDnsEntryAsync();
            var client = new Client(LoggerFactory, new DnsClientOptions());
            var result = await client.GetHostEntryAsync(entry.AddressList.First());

            Assert.True(entry.AddressList.Contains(result.AddressList.First()));
            Assert.Equal(entry.HostName, result.HostName);
        }

        [Fact]
        public async Task Client_GetHostEntryAsync_ByName()
        {
            var entry = await GetDnsEntryAsync();
            var client = new Client(LoggerFactory, new DnsClientOptions());
            var result = await client.GetHostEntryAsync(entry.HostName);

            Assert.True(entry.AddressList.Contains(result.AddressList.First()));
            Assert.Equal(entry.HostName, result.HostName);
        }

        [Fact]
        public async Task Client_Query_A()
        {
            var client = new Client(LoggerFactory, new DnsClientOptions());
            var result = await client.QueryAsync("google.com", QType.A);

            Assert.True(result.Answers.Count > 0);
            Assert.True(result.RecordsA.Count > 0);
        }

        [Fact]
        public async Task Client_Query_AAAA()
        {
            var client = new Client(LoggerFactory, new DnsClientOptions());
            var result = await client.QueryAsync("google.com", QType.AAAA);

            Assert.True(result.Answers.Count > 0);
            Assert.True(result.RecordsAAAA.Count > 0);
        }

        [Fact]
        public async Task Client_Query_Any()
        {
            var client = new Client(LoggerFactory, new DnsClientOptions());
            var result = await client.QueryAsync("google.com", QType.ANY);

            Assert.True(result.Answers.Count > 5);
            Assert.True(result.RecordsA.Count > 0);
            Assert.True(result.RecordsAAAA.Count > 0);
            Assert.True(result.RecordsMX.Count > 0);
        }

        [Fact]
        public async Task Client_Query_Mx()
        {
            var client = new Client(LoggerFactory, new DnsClientOptions());
            var result = await client.QueryAsync("google.com", QType.MX);

            Assert.True(result.Answers.Count > 0);
            Assert.True(result.RecordsMX.Count > 0);
        }

        [Fact]
        public async Task Client_Query_NS()
        {
            var client = new Client(LoggerFactory, new DnsClientOptions());
            var result = await client.QueryAsync("google.com", QType.NS);

            Assert.True(result.Answers.Count > 0);
            Assert.True(result.RecordsNS.Count > 0);
        }

        [Fact]
        public async Task Client_Query_TXT()
        {
            var client = new Client(LoggerFactory, new DnsClientOptions());
            var result = await client.QueryAsync("google.com", QType.TXT);

            Assert.True(result.Answers.Count > 0);
            Assert.True(result.RecordsTXT.Count > 0);
        }

        [Fact]
        public async Task Client_Query_SOA()
        {
            var client = new Client(LoggerFactory, new DnsClientOptions());
            var result = await client.QueryAsync("google.com", QType.SOA);

            Assert.True(result.Answers.Count > 0);
            Assert.True(result.RecordsSOA.Count > 0);
        }

        [Fact]
        public async Task Client_Query_ForceTimeout()
        {
            // basically testing we don't throw an error but return information
            var client = new Client(LoggerFactory, new DnsClientOptions() { Timeout = 0});
            var result = await client.QueryAsync("google.com", QType.ANY);
            Assert.True(!string.IsNullOrWhiteSpace(result.Error));
            Assert.True(result.Answers.Count == 0);
        }
    }
}
