using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public static partial class DnsClientBenchmarks
    {
        public class ForeachVsSelect
        {
            private static List<ModelA> Source = new List<ModelA>(Enumerable.Repeat(new ModelA(), 10000).ToArray());

            public ForeachVsSelect()
            {
            }

            [Benchmark(Baseline = true)]
            public void ForeachTransform()
            {
                var result = new List<ModelB>(Source.Count);
                foreach (var model in Source)
                {
                    result.Add(new ModelB() { Id = model.Id, SomeInt = model.SomeInt });
                }
            }

            [Benchmark]
            public void SelectTransform()
            {
                var result = Source.Select(p => new ModelB() { Id = p.Id, SomeInt = p.SomeInt }).ToList();
            }
        }

        public class ModelA
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();

            public int SomeInt { get; set; } = int.MaxValue;
        }

        public class ModelB
        {
            public string Id { get; set; }

            public int SomeInt { get; set; }
        }
    }
}