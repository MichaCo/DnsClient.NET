using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace NetCoreApp
{
    public static partial class DnsClientBenchmarks
    {
        public class ListConcat
        {
            public static ArraySegment<byte>[] sourceData;
            public static List<ArraySegment<byte>> source;

            public ListConcat()
            {
                byte[] bytes = Enumerable.Repeat((byte)192, 200).ToArray();
                sourceData = Enumerable.Repeat(new ArraySegment<byte>(bytes), 10).ToArray();
                source = new List<ArraySegment<byte>>(sourceData);
            }

            [Benchmark(Baseline = true)]
            public List<ArraySegment<byte>> List_Concat()
            {
                var start = new List<ArraySegment<byte>>(source);
                var concat = start.Concat(source);

                var result = concat.ToList();
                if (result.Count != source.Count * 2)
                {
                    throw new Exception();
                }
                return result;
            }

            [Benchmark]
            public List<ArraySegment<byte>> List_AddForEach()
            {
                var result = new List<ArraySegment<byte>>(source);
                foreach (var str in source)
                {
                    result.Add(str);
                }

                if (result.Count != source.Count * 2)
                {
                    throw new Exception();
                }
                return result;
            }

            [Benchmark]
            public List<ArraySegment<byte>> List_AddRange()
            {
                var result = new List<ArraySegment<byte>>(source);

                result.AddRange(source);

                if (result.Count != source.Count * 2)
                {
                    throw new Exception();
                }
                return result.ToList();
            }

            [Benchmark]
            public List<ArraySegment<byte>> List_ArrayCopy()
            {
                ArraySegment<byte>[] a = new ArraySegment<byte>[source.Count * 2];
                Array.Copy(sourceData, a, source.Count);
                Array.Copy(sourceData, a, source.Count);

                var result = new List<ArraySegment<byte>>(a);
                if (result.Count != source.Count * 2)
                {
                    throw new Exception();
                }
                return result;
            }
        }
    }
}