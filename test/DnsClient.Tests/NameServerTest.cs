using System;
using System.Net;
using System.Threading.Tasks;
using DnsClient.Windows;
using Xunit;

namespace DnsClient.Tests
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class NameServerTest
    {
        [Fact]
        public void NativeDnsServerResolution()
        {
            var ex = Record.Exception(() => NameServer.ResolveNameServersNative());
            Assert.Null(ex);
        }

        [Fact]
        public void ValidateAnyAddressIPv4()
        {
            Assert.ThrowsAny<InvalidOperationException>(() => NameServer.ValidateNameServers(new[] { new NameServer(IPAddress.Any) }));
        }

        [Fact]
        public void ValidateAnyAddressIPv6()
        {
            Assert.ThrowsAny<InvalidOperationException>(() => NameServer.ValidateNameServers(new[] { new NameServer(IPAddress.IPv6Any) }));
        }

        [Fact]
        public void ValidateAnyAddress_LookupClientInit()
        {
            Assert.ThrowsAny<InvalidOperationException>(() => new LookupClient(IPAddress.Any));
            Assert.ThrowsAny<InvalidOperationException>(() => new LookupClient(IPAddress.Any, 33));
            Assert.ThrowsAny<InvalidOperationException>(() => new LookupClient(IPAddress.IPv6Any));
            Assert.ThrowsAny<InvalidOperationException>(() => new LookupClient(IPAddress.IPv6Any, 555));
        }

        [Fact]
        public void ValidateAnyAddress_LookupClientQuery()
        {
            var client = new LookupClient(NameServer.Cloudflare);

            Assert.ThrowsAny<InvalidOperationException>(() => client.QueryServer(new[] { IPAddress.Any }, "query", QueryType.A));
            Assert.ThrowsAny<InvalidOperationException>(() => client.QueryServerReverse(new[] { IPAddress.Any }, IPAddress.Loopback));
        }

        [Fact]
        public async Task ValidateAnyAddress_LookupClientQueryAsync()
        {
            var client = new LookupClient(NameServer.Cloudflare);

            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => client.QueryServerAsync(new[] { IPAddress.Any }, "query", QueryType.A));
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => client.QueryServerReverseAsync(new[] { IPAddress.Any }, IPAddress.Loopback));
        }

        [Fact]
        public void ValidateNameResolutionPolicyDoesntThrowNormally()
        {
            var ex = Record.Exception(() => NameResolutionPolicy.Resolve());

            Assert.Null(ex);
        }

        [Fact]
        public void EqualityA()
        {
            var a = new NameServer(NameServer.GooglePublicDns);
            var b = new NameServer(NameServer.GooglePublicDns);

            Assert.Equal(a, b);
            Assert.Equal(a, a);
        }

        [Fact]
        public void EqualityB()
        {
            var a = new NameServer(IPAddress.Loopback);
            var b = new NameServer(IPAddress.Loopback);

            Assert.Equal(a, b);
        }

        [Fact]
        public void EqualityF()
        {
            var a = new NameServer(IPAddress.Loopback);
            var b = new NameServer(IPAddress.IPv6Any);

            Assert.NotEqual(a, b);
        }

        [Fact]
        public void EqualityC()
        {
            var a = new NameServer(IPAddress.Loopback);
            var b = new NameServer(IPAddress.Loopback, 111);

            Assert.NotEqual(a, b);
            Assert.NotEqual(b, a);
        }

        [Fact]
        public void EqualityD()
        {
            var a = new NameServer(IPAddress.Loopback, "domain");
            var b = new NameServer(IPAddress.Loopback, "domain");

            Assert.Equal(a, b);
        }

        [Fact]
        public void EqualityE()
        {
            var a = new NameServer(IPAddress.Loopback);
            var b = new NameServer(IPAddress.Loopback, "domain");

            Assert.Equal(a, b);
            Assert.Equal(b, a);
        }

        [Fact]
        public void EqualityG()
        {
            var a = new NameServer(IPAddress.IPv6Any);
            var b = new NameServer(IPAddress.Loopback, "domain");

            Assert.NotEqual(a, b);
            Assert.NotEqual(b, a);
        }

        [Fact]
        public void EqualityH()
        {
            var a = new NameServer(IPAddress.IPv6Any, "domain");
            var b = new NameServer(IPAddress.Loopback, "domain");

            Assert.NotEqual(a, b);
            Assert.NotEqual(b, a);
        }

        [Fact]
        public void EqualityI()
        {
            var a = new NameServer(IPAddress.IPv6Any, "domain");
            var b = new NameServer(IPAddress.Loopback, "domain2");

            Assert.NotEqual(a, b);
            Assert.NotEqual(b, a);
        }

        [Fact]
        public void EqualityJ()
        {
            var a = new NameServer(IPAddress.Loopback, "domain");
            var b = new NameServer(IPAddress.Loopback, "domain2");

            Assert.Equal(a, b);
            Assert.Equal(b, a);
        }
    }
}
