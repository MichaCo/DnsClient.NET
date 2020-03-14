using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

#if !NETCOREAPP1_1

namespace DnsClient.Tests
{
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
    }
}

#else
namespace System.Diagnostics.CodeAnalysis
{
    public class ExcludeFromCodeCoverageAttribute : Attribute
    {
    }
}

#endif