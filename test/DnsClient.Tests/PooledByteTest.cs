using System;
using System.Linq;
using DnsClient.Internal;
using Xunit;

namespace DnsClient.Tests
{
    public class PooledByteTest
    {
        [Fact]
        public void PooledByte_SimpleRent()
        {
            using (var b = new PooledBytes(10))
            {
                for (byte i = 0; i < 10; i++)
                {
                    b.Buffer[i] = i;
                }

                Assert.Equal(10, b.BufferSegment.Count);
                Assert.True(b.Buffer.Length > 10);
                Assert.Equal(9, b.Buffer[9]);
            }
        }

        [Fact]
        public void PooledByte_Extend()
        {
            using (var b = new PooledBytes(10))
            {
                for (byte i = 0; i < 10; i++)
                {
                    b.Buffer[i] = i;
                }

                Assert.Equal(10, b.BufferSegment.Count);

                b.Extend(100);

                b.Buffer[109] = 42;

                Assert.Equal(110, b.BufferSegment.Count);
                Assert.True(b.Buffer.Length > 110);
                Assert.Equal(1, b.Buffer[1]);
                Assert.Equal(9, b.Buffer[9]);
                Assert.Equal(0, b.Buffer[10]);
                Assert.Equal(42, b.Buffer[109]);
            }
        }
    }
}