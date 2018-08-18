using System;
using System.Buffers;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using DnsClient;

namespace Benchmarks
{
    public static partial class DnsClientBenchmarks
    {
        public class IntToBytes
        {
            private const int Ops = 100000;
            private readonly byte[] forMemoryBuffer;

            public IntToBytes()
            {
                // could use array pool, but that has overhead, unfair to compair as we would
                // not get a new array everytime to write an integer...
                forMemoryBuffer = new byte[4];
            }

            [Benchmark(Baseline = true)]
            public void BitConverterGetBytes()
            {
                for (int i = 0; i < Ops; i++)
                {
                    var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(i));
                }
            }

            [Benchmark()]
            public void MemoryMarshalTryWrite()
            {
                for (int i = 0; i < Ops; i++)
                {
                    var netInt = IPAddress.HostToNetworkOrder(i);
                    MemoryMarshal.TryWrite(forMemoryBuffer, ref netInt);
                }
            }

            [Benchmark()]
            public void MemoryMarshalAsBytes()
            {
                var intArray = new int[1];
                for (int i = 0; i < Ops; i++)
                {
                    intArray[0] = IPAddress.HostToNetworkOrder(i);
                    var result = MemoryMarshal.AsBytes(new ReadOnlySpan<int>(intArray));
                }
            }
        }

        public class SetBytesComparision
        {
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