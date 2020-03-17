using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

#if !NETCOREAPP1_1

namespace DnsClient.Tests
{
#pragma warning disable CS0618 // Type or member is obsolete

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ApiCompatibilityTest
    {
        [Fact]
        public void API_1_1_0_Compat()
        {
            var test = new ApiDesign.OldReference.TestLookupClient();
            var error = Record.Exception(() => test.TestQuery());

            Assert.Null(error);
        }

        [Fact]
        public async Task API_1_1_0_CompatAsync()
        {
            var test = new ApiDesign.OldReference.TestLookupClient();
            var error = await Record.ExceptionAsync(() => test.TestQueryAsync());

            Assert.Null(error);
        }

        [Fact]
        public void API_1_1_0_PropertiesNonDefaults()
        {
            var test = new ApiDesign.OldReference.TestLookupClient();

            test.SetNonDefaults();

            Assert.NotEmpty(test.Client.NameServers);
            Assert.False(test.Client.UseCache);
            Assert.True(test.Client.EnableAuditTrail);
            Assert.Equal(TimeSpan.FromSeconds(11), test.Client.MinimumCacheTimeout);
            Assert.False(test.Client.Recursion);
            Assert.True(test.Client.ThrowDnsErrors);
            Assert.Equal(10, test.Client.Retries);
            Assert.Equal(TimeSpan.FromMinutes(1), test.Client.Timeout);
            Assert.False(test.Client.UseTcpFallback);
            Assert.True(test.Client.UseTcpOnly);
            Assert.False(test.Client.ContinueOnDnsError);
            Assert.False(test.Client.UseRandomNameServer);

            Assert.False(test.Client.Settings.UseCache);
            Assert.True(test.Client.Settings.EnableAuditTrail);
            Assert.Equal(TimeSpan.FromSeconds(11), test.Client.Settings.MinimumCacheTimeout);
            Assert.False(test.Client.Settings.Recursion);
            Assert.True(test.Client.Settings.ThrowDnsErrors);
            Assert.Equal(10, test.Client.Settings.Retries);
            Assert.Equal(TimeSpan.FromMinutes(1), test.Client.Settings.Timeout);
            Assert.False(test.Client.Settings.UseTcpFallback);
            Assert.True(test.Client.Settings.UseTcpOnly);
            Assert.False(test.Client.Settings.ContinueOnDnsError);
            Assert.False(test.Client.Settings.UseRandomNameServer);

        }
    }

#pragma warning restore CS0618 // Type or member is obsolete
}

#else
namespace System.Diagnostics.CodeAnalysis
{
    public class ExcludeFromCodeCoverageAttribute : Attribute
    {
    }
}

#endif