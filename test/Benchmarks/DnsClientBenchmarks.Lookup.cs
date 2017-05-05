using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using DnsClient;

namespace Benchmarks
{
    public static partial class DnsClientBenchmarks
    {
        public class LoopupAsyncVsSync
        {
            private LookupClient _lookup;
            private LookupClient _lookupNoTtl;

            public LoopupAsyncVsSync()
            {
                _lookup = new LookupClient();
                _lookup.UseCache = false;

                _lookupNoTtl = new LookupClient()
                {
                    UseCache = false,
                    Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite)
                };
            }

            [Benchmark(Baseline = true)]
            public async Task Async()
            {
                var result = await _lookup.QueryAsync("localhost", QueryType.A);
            }

            [Benchmark]
            public async Task AsyncNoTtl()
            {
                var result = await _lookup.QueryAsync("localhost", QueryType.A);
            }

            [Benchmark]
            public void Sync()
            {
                var result = _lookup.Query("localhost", QueryType.A);
            }
        }

        public class LoopupCachedAsyncVsSync
        {
            private LookupClient _lookup;

            public LoopupCachedAsyncVsSync()
            {
                _lookup = new LookupClient();
                _lookup.UseCache = true;
            }

            [Benchmark(Baseline = true)]
            public async Task Async()
            {
                var result = await _lookup.QueryAsync("localhost", QueryType.A);
            }

            [Benchmark]
            public void Sync()
            {
                var result = _lookup.Query("localhost", QueryType.A);
            }
        }
    }
}