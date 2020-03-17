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

            var tasksCount = 12;
            //Console.WriteLine("warmup");
            //RunSync(client, 5, tasksCount);
            //RunAsync(client, 5, tasksCount).Wait();
            Console.WriteLine("running...");
            Console.ReadKey();
            double runTime = 2;
            for (var i = 1; i <= 3; i++)
            {
                for (var run = 0; run < 5; run++)
                {
                    RunSync(client, runTime, tasksCount * i);

                    RunAsync(client, runTime, tasksCount * i).GetAwaiter().GetResult();
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

            double tookInMs = (double)tookOverall / (Stopwatch.Frequency / 1000);
            double msPerExec = tookInMs / execCount;
            double execPerSec = execCount / runTime;

            Console.WriteLine($"{tasksCount,-5} {"sync",5} {execCount,10} {execPerSec,10:N0} {msPerExec,10:N5}");
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

            double tookInMs = (double)tookOverall / (Stopwatch.Frequency / 1000);
            double msPerExec = tookInMs / execCount;
            double execPerSec = execCount / runTime;

            Console.WriteLine($"{tasksCount,-5} {"async",5} {execCount,10} {execPerSec,10:N0} {msPerExec,10:N5}");
        }
    }
}