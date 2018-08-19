using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using DnsClient;

namespace Benchmarks
{
    public static partial class DnsClientBenchmarks
    {
        //[MarkdownExporter, AsciiDocExporter, HtmlExporter, CsvExporter, RPlotExporter]
        //[MinColumn, MaxColumn]
        //[ClrJob, CoreJob, MediumRunJob]
        public class StaticServerQuery
        {
            private readonly LookupClient _client;

            public StaticServerQuery()
            {
                // relies on static dns server
                _client = new LookupClient(new LookupClientOptions(new NameServer(IPAddress.Parse("127.0.0.1"), 5053))
                {
                    EnableAuditTrail = false,
                    UseCache = false,
                    UseTcpFallback = false,
                    Timeout = Timeout.InfiniteTimeSpan
                });
            }

            [Benchmark(Baseline = true)]
            public void RequestSync()
            {
                var result = _client.Query("doesnotmatter.com", QueryType.A);
                if (result.HasError || result.Answers.Count == 0)
                {
                    throw new Exception("Expected 1 result.");
                }
            }

            [Benchmark()]
            public async Task RequestAsync()
            {
                var result = await _client.QueryAsync("doesnotmatter.com", QueryType.A);
                if (result.HasError || result.Answers.Count == 0)
                {
                    throw new Exception("Expected 1 result.");
                }
            }
        }
    }
}