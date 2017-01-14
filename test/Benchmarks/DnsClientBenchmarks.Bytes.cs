using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using DnsClient;

namespace Benchmarks
{
    public static partial class DnsClientBenchmarks
    {
        public class SetBytesComparision
        {
            public const string domainName = "www.hello.world.fcking.funny.com";
            public static byte[] sourceSmall = new byte[] { 192, 168, 178, 23 };
            public static byte[] sourceBig = Enumerable.Repeat((byte)150, 1000).ToArray();

            public SetBytesComparision()
            {
            }

            [Benchmark]
            public ArraySegment<byte> ForLoop()
            {
                using (var writer = new DnsDatagramWriter())
                {
                    foreach (var b in sourceSmall)
                    {
                        writer.WriteByte(b);
                    }

                    return writer.Data;
                }
            }

            [Benchmark(Baseline = true)]
            public ArraySegment<byte> BlockCopy()
            {
                using (var writer = new DnsDatagramWriter())
                {
                    writer.WriteBytes(sourceSmall, sourceSmall.Length);
                    return writer.Data;
                }
            }

            [Benchmark]
            public ArraySegment<byte> ForLoopLarge()
            {
                using (var writer = new DnsDatagramWriter())
                {
                    foreach (var b in sourceBig)
                    {
                        writer.WriteByte(b);
                    }

                    return writer.Data;
                }
            }

            [Benchmark]
            public ArraySegment<byte> BlockCopyLarge()
            {
                using (var writer = new DnsDatagramWriter())
                {
                    writer.WriteBytes(sourceBig, sourceBig.Length);
                    return writer.Data;
                }
            }
        }

        public class AllocateVsPooledWriteHostName
        {
            public const string domainName = "www.hello.world.fcking.funny.com";

            public AllocateVsPooledWriteHostName()
            {
            }

            [Benchmark(Baseline = true)]
            public ArraySegment<byte> Allocate()
            {
                // allocate array manually (not pooled)
                using (var writer = new DnsDatagramWriter(new ArraySegment<byte>(new byte[DnsDatagramWriter.BufferSize])))
                {
                    writer.WriteHostName(domainName);
                    return writer.Data;
                }
            }

            [Benchmark]
            public ArraySegment<byte> Pooled()
            {
                using (var writer = new DnsDatagramWriter())
                {
                    writer.WriteHostName(domainName);
                    return writer.Data;
                }
            }
        }
    }
}