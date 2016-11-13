using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace DnsClient.Tests
{
    public class ClientTest
    {
        [Fact]
        public async Task Client_GetHostAddresses_Local()
        {
            var client = new Client(new DnsClientOptions());
            var result = await client.GetHostAddressesAsync("localhost");

            Assert.Equal("127.0.0.1", result.First().ToString());
        }

        [Fact]
        public async Task Client_GetHostAddresses_ActualHost()
        {
            var hostname = Dns.GetHostName();
            var hostIp = await Dns.GetHostAddressesAsync(hostname);
            hostIp = hostIp.Where(p => p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToArray();

            var client = new Client(new DnsClientOptions());
            var result = await client.GetHostAddressesAsync(hostname);

            Assert.Equal(hostIp, result);
        }

        [Fact]
        public async Task Client_GetHostEntryAsync_ByIp()
        {
            var hostname = Dns.GetHostName();
            var hostIp = await Dns.GetHostAddressesAsync(hostname);
            var ip = hostIp.Where(p => p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).First();

            var actual = await Dns.GetHostEntryAsync(ip);
            var client = new Client(new DnsClientOptions());
            var result = await client.GetHostEntryAsync(ip);

            Assert.Equal(actual.AddressList, result.AddressList);
            Assert.Equal(actual.HostName, result.HostName);
        }

        [Fact]
        public async Task Client_GetHostEntryAsync_ByName()
        {
            var hostname = Dns.GetHostName();
            var actual = await Dns.GetHostEntryAsync(hostname);
            var client = new Client(new DnsClientOptions());
            var result = await client.GetHostEntryAsync(hostname);

            Assert.True(actual.AddressList.Contains(result.AddressList.First()));
            Assert.Equal(actual.HostName, result.HostName);
        }

        [Fact]
        public async Task Client_Query_A()
        {
            var client = new Client(new DnsClientOptions());
            var result = await client.QueryAsync("google.com", QType.A);

            Assert.True(result.Answers.Count > 0);
        }

        [Fact]
        public async Task Client_Query_AAAA()
        {
            var client = new Client(new DnsClientOptions());
            var result = await client.QueryAsync("google.com", QType.AAAA);

            Assert.True(result.Answers.Count > 0);
        }

        [Fact]
        public async Task Client_Query_Any()
        {
            var client = new Client(new DnsClientOptions());
            var result = await client.QueryAsync("google.com", QType.ANY);

            Assert.True(result.Answers.Count > 5);
        }

        [Fact]
        public async Task Client_Query_Mx()
        {
            var client = new Client(new DnsClientOptions());
            var result = await client.QueryAsync("google.com", QType.MX);

            Assert.True(result.Answers.Count > 1);
        }

        [Fact]
        public async Task Client_Query_NS()
        {
            var client = new Client(new DnsClientOptions());
            var result = await client.QueryAsync("google.com", QType.NS);

            Assert.True(result.Answers.Count > 1);
        }
    }
}
