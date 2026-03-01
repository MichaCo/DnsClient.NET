// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using DnsClient;
using DnsClient.Internal;

namespace Benchmarks
{
    public static partial class DnsClientBenchmarks
    {
        public class CopyBytes
        {
            private const int OpsSmall = 10000;
            private const int OpsLarge = 10;
            public static byte[] sourceSmall = new byte[] { 192, 168, 178, 23 };
            public static byte[] sourceBig = Enumerable.Repeat((byte)150, 4000).ToArray();

            public CopyBytes()
            {
            }

            [Benchmark(Baseline = true, OperationsPerInvoke = OpsSmall)]
            public void ForLoopSmall()
            {
                var target = new byte[sourceSmall.Length];
                for (var o = 0; o < OpsSmall; o++)
                {
                    for (var i = 0; i < sourceSmall.Length; i++)
                    {
                        target[i] = sourceSmall[i];
                    }
                }
            }

            // TOO slow
            ////[Benchmark(OperationsPerInvoke = OpsLarge)]
            ////public void ForLoopLarge()
            ////{
            ////    var target = new byte[sourceBig.Length];

            ////    for (var o = 0; o < OpsLarge; o++)
            ////    {
            ////        for (var i = 0; i < sourceBig.Length; i++)
            ////        {
            ////            target[i] = sourceBig[i];
            ////        }
            ////    }
            ////}

            [Benchmark(OperationsPerInvoke = OpsSmall)]
            public void BlockCopySmall()
            {
                var target = new byte[sourceSmall.Length];
                for (var o = 0; o < OpsSmall; o++)
                {
                    Buffer.BlockCopy(sourceSmall, 0, target, 0, sourceSmall.Length);
                }
            }

            [Benchmark(OperationsPerInvoke = OpsLarge)]
            public void BlockCopyLarge()
            {
                var target = new byte[sourceBig.Length];
                for (var o = 0; o < OpsLarge; o++)
                {
                    Buffer.BlockCopy(sourceBig, 0, target, 0, sourceBig.Length);
                }
            }

            [Benchmark(OperationsPerInvoke = OpsSmall)]
            public void UnsafeCopySmall()
            {
                var target = new byte[sourceSmall.Length];
                for (var o = 0; o < OpsSmall; o++)
                {
                    Copy(sourceSmall, 0, target, 0, sourceSmall.Length);
                }
            }

            [Benchmark(OperationsPerInvoke = OpsLarge)]
            public void UnsafeCopyLarge()
            {
                var target = new byte[sourceBig.Length];
                for (var o = 0; o < OpsLarge; o++)
                {
                    Copy(sourceBig, 0, target, 0, sourceBig.Length);
                }
            }

            private static unsafe void Copy(byte[] source, int sourceOffset, byte[] target, int targetOffset, int count)
            {
                fixed (byte* pSource = source, pTarget = target)
                {
                    for (int i = 0; i < count; i++)
                    {
                        pTarget[targetOffset + i] = pSource[sourceOffset + i];
                    }
                }
            }
        }

        public class DnsDatagramWriter_IntToBytes
        {
            private const int Ops = 100000;
            private readonly byte[] _forMemoryBuffer;

            public DnsDatagramWriter_IntToBytes()
            {
                // could use array pool, but that has overhead, unfair to compair as we would
                // not get a new array everytime to write an integer...
                _forMemoryBuffer = new byte[4];
            }

            [Benchmark(Baseline = true, OperationsPerInvoke = Ops)]
            public void BitConverterGetBytes()
            {
                for (int i = 0; i < Ops; i++)
                {
                    _ = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(i));
                }
            }

            [Benchmark(OperationsPerInvoke = Ops)]
            public void MemoryMarshalTryWrite()
            {
                for (int i = 0; i < Ops; i++)
                {
                    var netInt = IPAddress.HostToNetworkOrder(i);
                    MemoryMarshal.TryWrite(_forMemoryBuffer, in netInt);
                }
            }

            [Benchmark(OperationsPerInvoke = Ops)]
            public void MemoryMarshalWrite()
            {
                for (int i = 0; i < Ops; i++)
                {
                    var netInt = IPAddress.HostToNetworkOrder(i);
                    MemoryMarshal.Write(_forMemoryBuffer, in netInt);
                }
            }

            [Benchmark(OperationsPerInvoke = Ops)]
            public void MemoryMarshalAsBytes()
            {
                var intArray = new int[1];
                for (int i = 0; i < Ops; i++)
                {
                    intArray[0] = IPAddress.HostToNetworkOrder(i);
                    _ = MemoryMarshal.AsBytes(new ReadOnlySpan<int>(intArray));
                }
            }
        }

        public class DnsDatagramWriter_SetBytesComparision
        {
            public static byte[] sourceSmall = new byte[] { 192, 168, 178, 23 };
            public static byte[] sourceBig = Enumerable.Repeat((byte)150, 1000).ToArray();

            public DnsDatagramWriter_SetBytesComparision()
            {
            }

            [Benchmark()]
            public ArraySegment<byte> ForLoopSmall()
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
            public ArraySegment<byte> BlockCopySmall()
            {
                using (var writer = new DnsDatagramWriter())
                {
                    writer.WriteBytes(sourceSmall, sourceSmall.Length);
                    return writer.Data;
                }
            }

            [Benchmark()]
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

            [Benchmark()]
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
            public const string DomainName = "www.hello.world.fcking.funny.com";

            public AllocateVsPooledWriteHostName()
            {
            }

            [Benchmark(Baseline = true)]
            public ArraySegment<byte> Allocate()
            {
                // allocate array manually (not pooled)
                using (var writer = new DnsDatagramWriter(new ArraySegment<byte>(new byte[DnsDatagramWriter.BufferSize])))
                {
                    writer.WriteHostName(DomainName);
                    return writer.Data;
                }
            }

            [Benchmark]
            public ArraySegment<byte> Pooled()
            {
                using (var writer = new DnsDatagramWriter())
                {
                    writer.WriteHostName(DomainName);
                    return writer.Data;
                }
            }
        }

        public class DnsDatagramWriterWriteInt
        {
            private const int Ops = 1000;

            [Benchmark(Baseline = true, OperationsPerInvoke = Ops)]
            public ArraySegment<byte> BufferedWriteInt()
            {
                using (var bytes = new PooledBytes(4 * Ops))
                using (var writer = new DnsDatagramWriter(bytes.BufferSegment))
                {
                    for (var i = 0; i < 1000; i++)
                    {
                        writer.WriteInt32NetworkOrder(i);
                    }

                    return writer.Data;
                }
            }

            [Benchmark(OperationsPerInvoke = Ops)]
            public ArraySegment<byte> MemoryWriteInt()
            {
                using (var writer = new TestingDnsDatagramWriter(Ops * 4))
                {
                    for (var i = 0; i < Ops; i++)
                    {
                        writer.WriteInt32NetworkOrder(i);
                    }

                    return new ArraySegment<byte>(writer.Memory.ToArray());
                }
            }
        }

        public class DnsDatagramWriterWriteString
        {
            private const int Ops = 1000;
            private const string ShortTestValue = "some.strange.domain.doodle.com.";
            private static readonly int s_shortTestValueByteLength = Encoding.UTF8.GetByteCount(ShortTestValue);

            [Benchmark(Baseline = true, OperationsPerInvoke = Ops)]
            public ArraySegment<byte> BufferedWrite()
            {
                using (var bytes = new PooledBytes(s_shortTestValueByteLength * Ops + Ops))
                using (var writer = new DnsDatagramWriter(bytes.BufferSegment))
                {
                    for (var i = 0; i < Ops; i++)
                    {
                        writer.WriteHostName(ShortTestValue);
                    }

                    return writer.Data;
                }
            }

            [Benchmark(OperationsPerInvoke = Ops)]
            public void MemoryWrite()
            {
                using (var writer = new TestingDnsDatagramWriter(s_shortTestValueByteLength * Ops))
                {
                    for (var i = 0; i < Ops; i++)
                    {
                        writer.WriteHostName(ShortTestValue);
                    }
                }
            }
        }

        internal class TestingDnsDatagramWriter : DnsDatagramWriter
        {
            private const byte DotByte = 46;
            private static readonly int s_intSize = Marshal.SizeOf<int>();
            private static readonly int s_shortSize = Marshal.SizeOf<short>();
            private static readonly int s_ushortSize = Marshal.SizeOf<ushort>();
            private static readonly int s_uintSize = Marshal.SizeOf<uint>();

            private readonly IMemoryOwner<byte> _ownedMemory;
            private readonly Memory<byte> _memory;

            public ReadOnlyMemory<byte> Memory => _ownedMemory.Memory.Slice(0, Index);

            public TestingDnsDatagramWriter(int size = BufferSize)
            {
                _ownedMemory = MemoryPool<byte>.Shared.Rent(size);
                _memory = _ownedMemory.Memory;
            }

            public override void WriteHostName(string queryName)
            {
                //var byteLength = Encoding.UTF8.GetByteCount(queryName);
                //if (byteLength == 0)
                //{
                //    WriteByte(0);
                //    return;
                //}
                //using (var buffer = new PooledBytes(byteLength))
                {
                    //Encoding.UTF8.GetBytes(queryName, 0, queryName.Length, buffer.Buffer, 0);
                    //var span = buffer.Buffer.AsSpan(0, byteLength);
                    var bytes = Encoding.UTF8.GetBytes(queryName);

                    if (bytes.Length == 0)
                    {
                        WriteByte(0);
                        return;
                    }

                    var span = bytes.AsSpan(0, bytes.Length);

                    int lastOctet = 0;
                    var index = 0;
                    foreach (var b in span)
                    {
                        if (b == DotByte)
                        {
                            WriteByte((byte)(index - lastOctet)); // length
                            WriteBytes(span.Slice(lastOctet, index - lastOctet));
                            lastOctet = index + 1;
                        }

                        index++;
                    }

                    WriteByte(0);
                }
            }

            public override void WriteByte(byte b)
            {
                _memory.Span[Index++] = b;
            }

            public override void WriteBytes(byte[] data, int length) => WriteBytes(data, 0, length);

            public void WriteBytes(Span<byte> data)
            {
                data.CopyTo(_memory.Slice(Index, data.Length).Span);
                Index += data.Length;
            }

            public override void WriteInt16NetworkOrder(short value)
            {
                var slice = _memory.Slice(Index, s_shortSize);

                if (BitConverter.IsLittleEndian)
                {
                    BinaryPrimitives.WriteInt32BigEndian(slice.Span, value);
                }
                else
                {
                    BinaryPrimitives.WriteInt32LittleEndian(slice.Span, value);
                }

                Index += s_shortSize;
            }

            public override void WriteInt32NetworkOrder(int value)
            {
                var slice = _ownedMemory.Memory.Slice(Index, s_intSize);

                if (BitConverter.IsLittleEndian)
                {
                    BinaryPrimitives.WriteInt32BigEndian(slice.Span, value);
                }
                else
                {
                    BinaryPrimitives.WriteInt32LittleEndian(slice.Span, value);
                }

                Index += s_intSize;
            }

            public override void WriteUInt16NetworkOrder(ushort value)
            {
                var slice = _ownedMemory.Memory.Slice(Index, s_ushortSize);

                if (BitConverter.IsLittleEndian)
                {
                    BinaryPrimitives.WriteUInt16BigEndian(slice.Span, value);
                }
                else
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(slice.Span, value);
                }

                Index += s_ushortSize;
            }

            public override void WriteUInt32NetworkOrder(uint value)
            {
                var slice = _ownedMemory.Memory.Slice(Index, s_uintSize);

                if (BitConverter.IsLittleEndian)
                {
                    BinaryPrimitives.WriteUInt32BigEndian(slice.Span, value);
                }
                else
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(slice.Span, value);
                }

                Index += s_uintSize;
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                if (disposing)
                {
                    _ownedMemory?.Dispose();
                }
            }
        }
    }
}
