using System;
using System.Net;
using Xunit;

namespace DnsClient.Tests
{
    public class LookupOptionsTest
    {
        [Fact]
        public void LookupOptions_EmptyCtor()
        {
            var options = new DnsLookupOptions();

            Assert.True(options.DnsServers.Count > 0);
        }

        [Fact]
        public void LookupOptions_InvalidList()
        {
            Action act = () => new DnsLookupOptions(null);

            var err = Assert.Throws<ArgumentException>(act);
            Assert.Contains("dnsServerEndpoints", err.ParamName);
        }

        [Fact]
        public void LookupOptions_EmptyList()
        {
            Action act = () => new DnsLookupOptions(new IPEndPoint[] { });

            var err = Assert.Throws<ArgumentException>(act);
            Assert.Contains("dnsServerEndpoints", err.ParamName);
        }

        [Fact]
        public void LookupOptions_TestDefaults()
        {
            var options = new DnsLookupOptions();

            Assert.True(options.Recursion);
            Assert.True(options.Retries == 3);
            Assert.True(options.Timeout == 1000);
            Assert.True(options.TransportType == TransportType.Udp);
            Assert.True(options.UseCache);
        }
    }
}