using System;
using System.Net;
using Xunit;

namespace DnsClient.Tests
{
    public class ClientOptionsTest
    {
        [Fact]
        public void ClientOptionsTest_EmptyCtor()
        {
            var options = new DnsClientOptions();

            Assert.True(options.DnsServers.Count > 0);
        }

        [Fact]
        public void ClientOptionsTest_InvalidList()
        {
            Action act = () => new DnsClientOptions(null);

            var err = Assert.Throws<ArgumentException>(act);
            Assert.Contains("dnsServerEndpoints", err.ParamName);
        }

        [Fact]
        public void ClientOptionsTest_EmptyList()
        {
            Action act = () => new DnsClientOptions(new IPEndPoint[] { });

            var err = Assert.Throws<ArgumentException>(act);
            Assert.Contains("dnsServerEndpoints", err.ParamName);
        }

        [Fact]
        public void ClientOptionsTest_TestDefaults()
        {
            var options = new DnsClientOptions();

            Assert.True(options.Recursion);
            Assert.True(options.Retries == 3);
            Assert.True(options.Timeout == 1000);
            Assert.True(options.TransportType == TransportType.Udp);
            Assert.True(options.UseCache);
        }
    }
}