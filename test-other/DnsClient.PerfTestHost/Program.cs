using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DnsClient.PerfTestHost
{
    internal class Program
    {
        private static async Task Main()
        {
            var port = 5053;
            var server = new StaticDnsServer(
                printStats: false,
                port: port,
                workers: 2);

            server.Start();

            var options = new LookupClientOptions(new NameServer(IPAddress.Parse("127.0.0.1"), port))
            {
                UseCache = false,
                EnableAuditTrail = false,
                UseTcpFallback = false,
                ThrowDnsErrors = false,
                MinimumCacheTimeout = null,
                Retries = 0,
                Timeout = Timeout.InfiniteTimeSpan
            };

            var client = new LookupClient(options);

            var tasksCount = 16;
            double runTime = 1;
            for (var i = 0; i < 4; i++)
            {
                for (var run = 0; run < 5; run++)
                {
                    RunSync(client, runTime, tasksCount + 8 * i);

                    await RunAsync(client, runTime, tasksCount + 8 * i);
                }
            }

            server.Stop();
        }

        private static void RunSync(LookupClient client, double runTime, int tasksCount = 8)
        {
            var swatch = Stopwatch.StartNew();

            long execCount = 0;
            long tookOverall = 0;

            Action act = () =>
            {
                var swatchInner = Stopwatch.StartNew();
                while (swatch.ElapsedMilliseconds < runTime * 1000)
                {
                    var result = client.Query("doesntmatter.com", QueryType.A);
                    if (result.HasError || result.Answers.Count < 1)
                    {
                        throw new Exception("Expected something");
                    }

                    Interlocked.Increment(ref execCount);
                }

                var took = swatchInner.ElapsedTicks;
                Interlocked.Add(ref tookOverall, took);
            };

            Parallel.Invoke(new ParallelOptions()
            {
                MaxDegreeOfParallelism = tasksCount
            },
            Enumerable.Repeat(act, tasksCount).ToArray());

            double execPerMs = execCount / swatch.ElapsedMilliseconds;
            double exedTimeInMs = 1 / execPerMs;
            double execPerSec = execCount / swatch.Elapsed.TotalSeconds;

            Console.WriteLine($"{tasksCount,-5} {"sync",5} {execCount,10} execs {execPerSec,10:N0}/sec {execPerMs,10:N0}/ms {exedTimeInMs,10:N4} ms/exec.");
        }

        private static async Task RunAsync(LookupClient client, double runTime, int tasksCount = 8)
        {
            var swatch = Stopwatch.StartNew();

            long execCount = 0;
            long tookOverall = 0;

            async Task Worker()
            {
                var swatchInner = Stopwatch.StartNew();
                while (swatch.ElapsedMilliseconds < runTime * 1000)
                {
                    var result = await client.QueryAsync("doesntmatter.com", QueryType.A);
                    if (result.HasError || result.Answers.Count < 1)
                    {
                        throw new Exception("Expected something");
                    }

                    Interlocked.Increment(ref execCount);
                }

                var took = swatchInner.ElapsedTicks;
                Interlocked.Add(ref tookOverall, took);
            };

            var tasks = new List<Task>();
            for (var i = 0; i < tasksCount; i++)
            {
                tasks.Add(Worker());
            }

            await Task.WhenAll(tasks.ToArray());

            double execPerMs = execCount / swatch.ElapsedMilliseconds;
            double exedTimeInMs = 1 / execPerMs;
            double execPerSec = execCount / swatch.Elapsed.TotalSeconds;

            Console.WriteLine($"{tasksCount,-5} {"async",5} {execCount,10} execs {execPerSec,10:N0}/sec {execPerMs,10:N0}/ms {exedTimeInMs,10:N4} ms/exec.");
        }
    }
}
