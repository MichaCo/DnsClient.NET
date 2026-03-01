// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public static partial class DnsClientBenchmarks
    {
        public class StringSplit
        {
            private const string Source = ".www.lala.lolol.blub.de.com.org.eu.gov.";

            public StringSplit()
            {
            }

            //[Benchmark(Baseline = true)]
            public object Split()
            {
                var x = SplitString(Source).ToArray();
                return x;
            }

            // [Benchmark]
            public object ManualSplit()
            {
                var x = ManualSplitBytes(Source).ToArray();
                return x;
            }

            [Benchmark]
            public object ManualSplitMemoryT()
            {
                var sourceMem = Source.AsMemory();
                var readTotal = 0;
                var len = sourceMem.Length * 4;

                Span<byte> mem = stackalloc byte[len];

                //using (var mem = MemoryPool<byte>.Shared.Rent(len))
                //{
                var seq = new ReadOnlySequence<char>(sourceMem);

                SequencePosition? pos = null;
                while ((pos = seq.PositionOf(CharDot)) != null)
                {
                    var part = seq.Slice(seq.Start, pos.Value);
                    seq = seq.Slice(seq.GetPosition(1, pos.Value));

                    // Label memory.
                    var slice = mem.Slice(readTotal);

                    // Write label.
                    var read = Encoding.UTF8.GetBytes(part.First.Span, slice.Slice(1));

                    // Write label length prefix.
                    slice[0] = (byte)read;
                    readTotal += read + 1;
                }

                return readTotal;
                //}
            }

            [Benchmark]
            public object ManualSplitMemoryT2()
            {
                var sourceMem = Source.AsMemory();
                Span<byte> inputMem = stackalloc byte[sourceMem.Length * 4];
                Span<byte> outputMem = stackalloc byte[sourceMem.Length * 4];
                var read = Encoding.UTF8.GetBytes(sourceMem.Span, inputMem);

                int currentPos = 0;
                int offset = 0;
                while ((offset = inputMem.Slice(currentPos).IndexOf(Dot)) > -1)
                {
                    var label = inputMem.Slice(currentPos, offset);
                    outputMem[currentPos++] = (byte)label.Length;
                    label.CopyTo(outputMem.Slice(currentPos));
                    currentPos += offset;
                }

                return read;
                //}
            }

            public static IEnumerable<byte[]> SplitString(string input)
            {
                if (input is null)
                {
                    throw new ArgumentNullException(nameof(input));
                }

                foreach (var label in input.Split(new char[] { '.' }, StringSplitOptions.None))
                {
                    yield return Encoding.UTF8.GetBytes(label);
                }
            }

            private const char CharDot = '.';
            private const byte Dot = (byte)CharDot;

            public static IEnumerable<ArraySegment<byte>> ManualSplitBytes(string input)
            {
                if (input is null)
                {
                    throw new ArgumentNullException(nameof(input));
                }

                var bytes = Encoding.UTF8.GetBytes(input);

                int lastStop = 0;
                for (int index = 0; index < input.Length; index++)
                {
                    if (bytes[index] == Dot)
                    {
                        yield return new ArraySegment<byte>(bytes, lastStop, index - lastStop);
                        lastStop = index + 1;
                    }
                }

                if (lastStop < bytes.Length)
                {
                    yield return new ArraySegment<byte>(bytes, lastStop, bytes.Length - lastStop);
                }
            }
        }
    }
}
