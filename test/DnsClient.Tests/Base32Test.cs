using System;
using System.Linq;
using DnsClient.Internal;
using Xunit;

namespace DnsClient.Tests
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Base32Test
    {
        [Fact]
        public void Base32Hex_InvalidStringInput()
        {
            Assert.Throws<ArgumentException>(() => Base32Hex.FromBase32HexString("XYZ"));
            Assert.Throws<ArgumentException>(() => Base32Hex.FromBase32HexString("YZ"));
            Assert.Throws<ArgumentException>(() => Base32Hex.FromBase32HexString("Z="));
            Assert.Throws<ArgumentException>(() => Base32Hex.FromBase32HexString("11234^="));
        }

        [Fact]
        public void Base32Hex_ValidConversion()
        {
            var expected = "CK0Q1GIN43N1ARRC9OSM6QPQR81H5M9A";
            var expectedBytes = new byte[] { 101, 1, 160, 194, 87, 32, 238, 21, 111, 108, 78, 57, 99, 107, 58, 218, 3, 18, 217, 42 };
            var bytes = Base32Hex.FromBase32HexString(expected);
            var result = Base32Hex.ToBase32HexString(bytes);

            Assert.Equal(expected, result);
            Assert.Equal(expectedBytes, bytes);
        }

        [Fact]
        public void Base32Hex_ValidConversion2()
        {
            var expected = "VVVVVVVV";
            var expectedBytes = new byte[] { 255, 255, 255, 255, 255 };

            var bytes = Base32Hex.FromBase32HexString(expected);
            var result = Base32Hex.ToBase32HexString(bytes);

            Assert.Equal(expected, result);
            Assert.Equal(expectedBytes, bytes);
        }
    }
}
