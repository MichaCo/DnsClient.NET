// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public static partial class DnsClientBenchmarks
    {
        public class ListConcat
        {
            private static ArraySegment<byte>[] s_sourceData;
            private static List<ArraySegment<byte>> s_source;

            public ListConcat()
            {
                byte[] bytes = Enumerable.Repeat((byte)192, 200).ToArray();
                s_sourceData = Enumerable.Repeat(new ArraySegment<byte>(bytes), 10).ToArray();
                s_source = new List<ArraySegment<byte>>(s_sourceData);
            }

            [Benchmark]
            public List<ArraySegment<byte>> List_Concat()
            {
                var start = new List<ArraySegment<byte>>(s_source);
                var concat = start.Concat(s_source);

                var result = concat.ToList();
                if (result.Count != s_source.Count * 2)
                {
                    throw new Exception();
                }
                return result;
            }

            [Benchmark]
            public List<ArraySegment<byte>> List_AddForEach()
            {
                var start = new List<ArraySegment<byte>>(s_source);
                foreach (var str in s_source)
                {
                    start.Add(str);
                }

                if (start.Count != s_source.Count * 2)
                {
                    throw new Exception();
                }
                return start;
            }

            [Benchmark(Baseline = true)]
            public List<ArraySegment<byte>> List_AddRange()
            {
                var start = new List<ArraySegment<byte>>(s_source);
                start.AddRange(s_source);

                if (start.Count != s_source.Count * 2)
                {
                    throw new Exception();
                }
                return start;
            }

            [Benchmark]
            public List<ArraySegment<byte>> List_ArrayCopy()
            {
                ArraySegment<byte>[] a = new ArraySegment<byte>[s_source.Count * 2];
                Array.Copy(s_sourceData, a, s_source.Count);
                Array.Copy(s_sourceData, a, s_source.Count);

                var result = new List<ArraySegment<byte>>(a);
                if (result.Count != s_source.Count * 2)
                {
                    throw new Exception();
                }
                return result;
            }
        }
    }
}
