// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

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
            var result = NameServer.ResolveNameServersNative();
            Assert.NotEmpty(result);
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
    }
}
