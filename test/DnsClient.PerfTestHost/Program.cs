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
        private static void Main(string[] args)
        {
            var port = 5053;
            var server = new StaticDnsServer(
                printStats: false,
                port: port,
                workers: 1);

            server.Start();

            var client = new LookupClient(IPAddress.Parse("127.0.0.1"), port)
            {
                UseCache = false,
                EnableAuditTrail = false,
                UseTcpFallback = false,
                ThrowDnsErrors = false,
                MinimumCacheTimeout = null,
                Retries = 0,
                Timeout = Timeout.InfiniteTimeSpan
            };

            double runTime = 5;
            RunSync(client, runTime);
            RunAsync(client, runTime).Wait();

            server.Stop();
        }

        private static void RunSync(LookupClient client, double runTime)
        {
            var swatch = Stopwatch.StartNew();
            var swatchInner = Stopwatch.StartNew();

            long execCount = 0;
            long tookOverall = 0;

            Action act = () =>
            {
                while (swatch.ElapsedMilliseconds < runTime * 1000)
                {
                    swatchInner.Restart();
                    var result = client.Query("doesntmatter.com", QueryType.A);
                    if (result.HasError || result.Answers.Count < 1)
                    {
                        throw new Exception("Expected something");
                    }

                    var took = swatchInner.ElapsedTicks;
                    Interlocked.Add(ref tookOverall, took);
                    Interlocked.Increment(ref execCount);
                }
            };

            var tasks = 8;

            Console.WriteLine($"Running sync with {tasks} parallel tasks.");
            Parallel.Invoke(new ParallelOptions()
            {
                MaxDegreeOfParallelism = tasks
            },
            Enumerable.Repeat(act, tasks).ToArray());

            double tookInMs = (double)tookOverall / (Stopwatch.Frequency / 1000);
            double msPerExec = tookInMs / execCount;
            double execPerSec = execCount / runTime;

            Console.WriteLine($"Sync: {execCount} hits, {execPerSec:N0} query/sec with {msPerExec:N3} ms/query.");
        }

        private static async Task RunAsync(LookupClient client, double runTime)
        {
            var swatch = Stopwatch.StartNew();
            var swatchInner = Stopwatch.StartNew();

            long execCount = 0;
            long tookOverall = 0;

            Func<Task> act = async () =>
            {
                while (swatch.ElapsedMilliseconds < runTime * 1000)
                {
                    swatchInner.Restart();
                    var result = await client.QueryAsync("doesntmatter.com", QueryType.A);
                    if (result.HasError || result.Answers.Count < 1)
                    {
                        throw new Exception("Expected something");
                    }

                    var took = swatchInner.ElapsedTicks;
                    Interlocked.Add(ref tookOverall, took);
                    Interlocked.Increment(ref execCount);
                }
            };
            

            var tasksCount = 16;
            Console.WriteLine($"Running async with {tasksCount} parallel tasks.");
            var tasks = new List<Task>();
            for (var i = 0; i < tasksCount; i++)
            {
                tasks.Add(act());
            }

            await Task.WhenAll(tasks.ToArray());

            double tookInMs = (double)tookOverall / (Stopwatch.Frequency / 1000);
            double msPerExec = tookInMs / execCount;
            double execPerSec = execCount / runTime;

            Console.WriteLine($"Async: {execCount} hits, {execPerSec:N0} query/sec with {msPerExec:N3} ms/query.");
        }
    }
}