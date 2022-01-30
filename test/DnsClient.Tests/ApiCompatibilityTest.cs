using System;
using System.Threading.Tasks;
using Xunit;

namespace DnsClient.Tests
{
#pragma warning disable CS0618 // Type or member is obsolete

#if ENABLE_REMOTE_DNS

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ApiCompatibilityTest
    {
        [Fact]
        public void API_Compat()
        {
            var test = new OldReference.TestLookupClient();
            var error = Record.Exception(() => test.TestQuery_1_1());

            Assert.Null(error);
        }

        [Fact]
        public async Task API_CompatAsync()
        {
            var test = new OldReference.TestLookupClient();
            var error = await Record.ExceptionAsync(() => test.TestQueryAsync_1_1()).ConfigureAwait(false);

            Assert.Null(error);
        }

        [Fact]
        public void API_CompatPropertiesNonDefaults()
        {
            var test = new OldReference.TestLookupClient();

            var client = test.SetNonDefaults();

            Assert.NotEmpty(client.NameServers);
            Assert.False(client.UseCache);
            Assert.True(client.EnableAuditTrail);
            Assert.Equal(TimeSpan.FromSeconds(11), client.MinimumCacheTimeout);
            Assert.False(client.Recursion);
            Assert.True(client.ThrowDnsErrors);
            Assert.Equal(10, client.Retries);
            Assert.Equal(TimeSpan.FromMinutes(1), client.Timeout);
            Assert.False(client.UseTcpFallback);
            Assert.True(client.UseTcpOnly);
            Assert.False(client.ContinueOnDnsError);
            Assert.False(client.UseRandomNameServer);

            Assert.False(client.Settings.UseCache);
            Assert.True(client.Settings.EnableAuditTrail);
            Assert.Equal(TimeSpan.FromSeconds(11), client.Settings.MinimumCacheTimeout);
            Assert.False(client.Settings.Recursion);
            Assert.True(client.Settings.ThrowDnsErrors);
            Assert.Equal(10, client.Settings.Retries);
            Assert.Equal(TimeSpan.FromMinutes(1), client.Settings.Timeout);
            Assert.False(client.Settings.UseTcpFallback);
            Assert.True(client.Settings.UseTcpOnly);
            Assert.False(client.Settings.ContinueOnDnsError);
            Assert.False(client.Settings.UseRandomNameServer);
        }

        [Fact]
        public void API_CompatProtocol_1_1()
        {
            var test = new OldReference.TestLookupClient();

            test.TestProtocol_1_1();
        }

#if NETCOREAPP3_1
        [Fact]
        public void API_CompatProtocol_1_2()
        {
            var test = new OldReference.TestLookupClient();

            test.TestProtocol_1_2();
        }
#endif

        [Fact]
        public void API_CompatOther()
        {
            var test = new OldReference.TestLookupClient();

            test.TestOtherTypes();
        }
    }
#endif
#pragma warning restore CS0618 // Type or member is obsolete
}
