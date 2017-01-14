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
            public static ArraySegment<byte>[] _sourceData;
            public static List<ArraySegment<byte>> _source;

            public ListConcat()
            {
                byte[] bytes = Enumerable.Repeat((byte)192, 200).ToArray();
                _sourceData = Enumerable.Repeat(new ArraySegment<byte>(bytes), 10).ToArray();
                _source = new List<ArraySegment<byte>>(_sourceData);
            }

            [Benchmark]
            public List<ArraySegment<byte>> List_Concat()
            {
                var start = new List<ArraySegment<byte>>(_source);
                var concat = start.Concat(_source);

                var result = concat.ToList();
                if (result.Count != _source.Count * 2)
                {
                    throw new Exception();
                }
                return result;
            }

            [Benchmark]
            public List<ArraySegment<byte>> List_AddForEach()
            {
                var start = new List<ArraySegment<byte>>(_source);
                foreach (var str in _source)
                {
                    start.Add(str);
                }

                if (start.Count != _source.Count * 2)
                {
                    throw new Exception();
                }
                return start;
            }

            [Benchmark(Baseline = true)]
            public List<ArraySegment<byte>> List_AddRange()
            {
                var start = new List<ArraySegment<byte>>(_source);
                start.AddRange(_source);

                if (start.Count != _source.Count * 2)
                {
                    throw new Exception();
                }
                return start;
            }

            [Benchmark]
            public List<ArraySegment<byte>> List_ArrayCopy()
            {
                ArraySegment<byte>[] a = new ArraySegment<byte>[_source.Count * 2];
                Array.Copy(_sourceData, a, _source.Count);
                Array.Copy(_sourceData, a, _source.Count);

                var result = new List<ArraySegment<byte>>(a);
                if (result.Count != _source.Count * 2)
                {
                    throw new Exception();
                }
                return result;
            }
        }
    }
}