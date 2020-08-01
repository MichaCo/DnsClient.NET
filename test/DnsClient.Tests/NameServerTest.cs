using Xunit;

namespace DnsClient.Tests
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]

    public class NameServerTest
    {

#if !NET461

        [Fact]
        public void NativeDnsServerResolution()
        {
            var ex = Record.Exception(() => NameServer.ResolveNameServersNative());
            Assert.Null(ex);
        }

#endif
    }
}