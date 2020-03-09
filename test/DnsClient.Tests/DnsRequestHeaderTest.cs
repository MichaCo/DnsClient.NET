using System;
using Xunit;

namespace DnsClient.Tests
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class DnsRequestHeaderTest
    {
        [Fact]
        public void DnsRequestHeader_ValidateCtor1()
        {
            var header = new DnsRequestHeader(DnsOpCode.Notify);

            Assert.True(header.UseRecursion);
            Assert.Equal(DnsOpCode.Notify, header.OpCode);
        }

        [Fact]
        public void DnsRequestHeader_ValidateCtor2()
        {
            var header = new DnsRequestHeader(true, DnsOpCode.Notify);

            Assert.True(header.UseRecursion);
            Assert.Equal(DnsOpCode.Notify, header.OpCode);
        }

        [Fact]
        public void DnsRequestHeader_ChangeRecursion()
        {
            var header = new DnsRequestHeader(true, DnsOpCode.Notify);

            Assert.Equal(8448, header.RawFlags);

            header.UseRecursion = false;
            Assert.False(header.UseRecursion);
            Assert.Equal(8192, header.RawFlags);

            header.UseRecursion = true;
            Assert.True(header.UseRecursion);
            Assert.Equal(8448, header.RawFlags);

            header.UseRecursion = false;
            Assert.False(header.UseRecursion);
            Assert.Equal(8192, header.RawFlags);

            header.UseRecursion = true;
            Assert.True(header.UseRecursion);
            Assert.Equal(8448, header.RawFlags);

            Assert.Equal(DnsOpCode.Notify, header.OpCode);
        }
    }
}