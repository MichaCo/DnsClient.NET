// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System.Net;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using DnsClient;

namespace Benchmarks
{
    public static partial class DnsClientBenchmarks
    {
        public class LoopupAsyncVsSync
        {
            private readonly LookupClient _lookup;

            public LoopupAsyncVsSync()
            {
                _lookup = new LookupClient(
                    new LookupClientOptions(IPAddress.Loopback)
                    {
                        UseCache = false
                    });
            }

            [Benchmark(Baseline = true)]
            public async Task Async()
            {
                _ = await _lookup.QueryAsync("localhost", QueryType.A).ConfigureAwait(false);
            }

            [Benchmark]
            public async Task AsyncNoTtl()
            {
                _ = await _lookup.QueryAsync("localhost", QueryType.A).ConfigureAwait(false);
            }

            [Benchmark]
            public void Sync()
            {
                _ = _lookup.Query("localhost", QueryType.A);
            }
        }

        public class LoopupCachedAsyncVsSync
        {
            private readonly LookupClient _lookup;

            public LoopupCachedAsyncVsSync()
            {
                _lookup = new LookupClient(new LookupClientOptions(IPAddress.Loopback)
                {
                    UseCache = true
                });
            }

            [Benchmark(Baseline = true)]
            public async Task Async()
            {
                _ = await _lookup.QueryAsync("localhost", QueryType.A).ConfigureAwait(false);
            }

            [Benchmark]
            public void Sync()
            {
                _ = _lookup.Query("localhost", QueryType.A);
            }
        }
    }
}
