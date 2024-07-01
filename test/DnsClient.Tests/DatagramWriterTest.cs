// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Linq;
using Xunit;

namespace DnsClient.Tests
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class DatagramWriterTest
    {
        [Fact]
        public void WriteStringWithPrefix_TooLong()
        {
            using var writer = new DnsDatagramWriter();
            var value = new string(Enumerable.Repeat('X', 256).ToArray());
            Assert.Throws<ArgumentException>(() => writer.WriteStringWithLengthPrefix(value));
        }

        [Fact]
        public void ReverseBytes_UInt16()
        {
            ushort value = ushort.MaxValue / 2 + 10;

            using var writer = new DnsDatagramWriter();
            writer.WriteUInt16NetworkOrder(value);

            var sourceBytes = BitConverter.GetBytes(value);

            var result = writer.Data.Take(2).ToArray();

            Assert.Equal(sourceBytes[0], result[1]);
            Assert.Equal(sourceBytes[1], result[0]);
        }
    }
}
