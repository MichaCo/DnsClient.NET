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
            var error = await Record.ExceptionAsync(() => test.TestQueryAsync_1_1());

            Assert.Null(error);
        }

        [Fact]
        public void API_CompatProtocol_1_1()
        {
            var test = new OldReference.TestLookupClient();

            test.TestProtocol_1_1();
        }

#if NET6_0
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
