using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DnsClient;
using Microsoft.Extensions.CommandLineUtils;

namespace DigApp
{
    public class PerfCommand : DnsCommand
    {
        private static string[] _domainNames;
        private static object _nameLock = new object();
        private static Random _randmom = new Random();

        public CommandOption ClientsArg { get; private set; }

        public CommandArgument QueryArg { get; private set; }

        public CommandOption RunsArg { get; private set; }

        static PerfCommand()
        {
            _domainNames = File.ReadAllLines("names.txt");
        }

        public PerfCommand(CommandLineApplication app, string[] originalArgs) : base(app, originalArgs)
        {
        }

        public static string NextDomainName()
        {
            lock (_nameLock)
            {
                return _domainNames[_randmom.Next(0, _domainNames.Length)];
            }
        }

        protected override void Configure()
        {
            QueryArg = App.Argument("query", "the domain query to run.", false);
            ClientsArg = App.Option("-c | --clients", "Number of clients to run", CommandOptionType.SingleValue);
            RunsArg = App.Option("-r | --runs", "Number of runs", CommandOptionType.SingleValue);
            base.Configure();
        }

        protected override async Task<int> Execute()
        {
            var useClients = ClientsArg.HasValue() ? int.Parse(ClientsArg.Value()) : 10;
            var useRuns = RunsArg.HasValue() ? int.Parse(RunsArg.Value()) : 100;
            var useQuery = string.IsNullOrWhiteSpace(QueryArg.Value) ? string.Empty : QueryArg.Value;
            var lookup = GetDnsLookup();
            
            var settings = GetLookupSettings();
            var runner = new PerfRunner(settings, useClients, useRuns, useQuery);
            await runner.Run();

            return 0;
        }

        private struct PerformanceResult
        {
            public int ErrorResponses { get; set; }

            public int SuccessResponses { get; set; }

            public List<double> Times { get; set; }

            public long TimeTakenMs { get; set; }

            public override string ToString()
            {
                if (Times.Count > 0)
                {
                    Times.Sort();
                    var median = Times.Sum() / (Times.Count / 2);
                    return $":: {TimeTakenMs,-15:N0} {SuccessResponses,-10} {ErrorResponses,-10} {Times.Min(),-10:N4}{Times.Max(),-10:N2}{Times.Average(),-10:N4}{median,-10:N4}";
                }
                return $";; no response.";
            }
        }

        private class ManagedTestClient : PerformanceTestClient
        {
            private readonly LookupClient _lookup;
            
            public ManagedTestClient(LookupSettings settings, int runs, string query)
                : base(settings, runs, query)
            {
                _lookup = new LookupClient(settings.Endpoints)
                {
                    Recursion = settings.Recursion,
                    Retries = settings.Retries,
                    Timeout = settings.Timeout,
                    UseCache = settings.UseCache,
                    MimimumCacheTimeout = settings.MinTTL
                };
            }

            protected override async Task<int> ExcecuteIterationAsync()
            {
                var queryResult = await _lookup.QueryAsync(Query, QueryType.A);
                return queryResult.MessageSize;
            }
        }

        private class NativeTestClientDnsQuery : PerformanceTestClient
        {
            public NativeTestClientDnsQuery(LookupSettings settings, int runs, string query)
                : base(settings, runs, query)
            {
            }

            protected override Task<int> ExcecuteIterationAsync()
            {
                var queryResult = Interop.Dns.GetARecords(Query, Settings.UseCache);
                return Task.FromResult(queryResult.Count);
            }
        }

        private class NativeTestClientDnsQueryEx : PerformanceTestClient
        {
            public NativeTestClientDnsQueryEx(LookupSettings settings, int runs, string query)
                : base(settings, runs, query)
            {
            }

            protected override Task<int> ExcecuteIterationAsync()
            {
                var queryResult = Interop.DNSQueryer.QueryDNSForRecordTypeSpecificNameServers(
                            Query, Settings.Endpoints, Interop.DNSQueryer.DnsRecordTypes.DNS_TYPE_A);

                return Task.FromResult(queryResult.Select(p => p.Keys.Count).Count());
            }
        }

        private abstract class PerformanceTestClient
        {
            private readonly bool _useRandom;

            protected string Query { get; set; }

            protected int Runs { get; }

            protected LookupSettings Settings { get; }

            public PerformanceTestClient(LookupSettings settings, int runs, string query)
            {
                Settings = settings;
                Query = query;
                Runs = runs;
                if (Query == string.Empty)
                {
                    _useRandom = true;
                }
            }

            public async Task<PerformanceResult> Run()
            {
                var result = new PerformanceResult();
                var responseBytes = 0;
                result.Times = new List<double>();

                var swatchReq = Stopwatch.StartNew();
                for (var index = 0; index < Runs; index++)
                {
                    swatchReq.Restart();
                    if (_useRandom)
                    {
                        Query = PerfCommand.NextDomainName();
                    }

                    try
                    {
                        var resultCount = await ExcecuteIterationAsync();
                        var responseElapsed = swatchReq.ElapsedTicks / 10000d;
                        result.Times.Add(responseElapsed);

                        if (resultCount <= 0)
                        {
                            result.ErrorResponses++;
                        }
                        else
                        {
                            Interlocked.Add(ref responseBytes, resultCount);
                        }
                    }
                    catch
                    {
                        result.ErrorResponses++;
                    }
                }

                result.TimeTakenMs = (long)result.Times.Sum();
                result.SuccessResponses = responseBytes;

                return result;
            }

            // return <= 0 for fail or >=1 for number of results
            protected abstract Task<int> ExcecuteIterationAsync();
        }

        private class PerfRunner
        {
            private readonly int _clients;
            private readonly string _query;
            private readonly int _runs;
            private readonly LookupSettings _settings;

            public PerfRunner(LookupSettings settings, int clients, int runs, string query)
            {
                _query = query;
                _settings = settings;
                _clients = clients;
                _runs = runs;
            }

            public async Task Run()
            {
                Console.WriteLine($"; <<>> Starting perf run with {_clients} clients and {_runs} queries per client <<>>");
                Console.WriteLine($"; ({_settings.Endpoints.Length} Servers, caching:{_settings.UseCache}, minttl:{_settings.MinTTL.TotalMilliseconds})");
                
                await RunManaged();
                // await RunNativeDnsQuery();
                await RunNativeDnsQueryEx();
            }

            private async Task RunBench(string name, Func<Task<PerformanceResult[]>> act)
            {
                var spinner = new Spiner();
                spinner.Start();

                // native test
                var sw = Stopwatch.StartNew();
                PerformanceResult[] results = await act();
                var elapsed = sw.ElapsedMilliseconds;

                // results
                spinner.Stop();

                Console.WriteLine(string.Join("-", Enumerable.Repeat("-", 50)));
                Console.WriteLine($";; {name} results:\t\t");
                Console.WriteLine(string.Join("-", Enumerable.Repeat("-", 50)));
                Console.WriteLine($";; {"Overall(ms)",-15} {"OK",-10} {"Errors",-10} {"MIN(ms)",-10}{"MAX(ms)",-10}{"AVG(ms)",-10}{"Median",-10}");
                foreach (var result in results)
                {
                    Console.WriteLine(result);
                }

                var avgRuntime = (1000.0d / elapsed) * (_clients * _runs);
                Console.WriteLine($";; {name} run took {elapsed}ms for {_clients} clients * {_runs} queries: {avgRuntime:N0} queries per second.");
            }

            private async Task RunManaged()
            {
                await RunBench("Managed", async () =>
                {
                    var tasks = new List<Task<PerformanceResult>>();

                    for (var i = 0; i < _clients; i++)
                    {
                        var client = new ManagedTestClient(_settings, _runs, _query);
                        tasks.Add(client.Run());
                    }

                    return await Task.WhenAll(tasks);
                });
            }

            private async Task RunNativeDnsQuery()
            {
                await RunBench("DnsQuery", () =>
                {
                    var tasks = new List<Action>();
                    var results = new System.Collections.Concurrent.ConcurrentBag<PerformanceResult>();
                    for (var i = 0; i < _clients; i++)
                    {
                        var client = new NativeTestClientDnsQuery(_settings, _runs, _query);
                        tasks.Add(() =>
                        {
                            results.Add(client.Run().Result);
                        });
                    }

                    Parallel.Invoke(new ParallelOptions() { MaxDegreeOfParallelism = 32 }, tasks.ToArray());
                    return Task.FromResult(results.ToArray());
                });
            }

            private async Task RunNativeDnsQueryEx()
            {
                await RunBench("DnsQueryEx", () =>
                {
                    var tasks = new List<Action>();
                    var results = new System.Collections.Concurrent.ConcurrentBag<PerformanceResult>();
                    for (var i = 0; i < _clients; i++)
                    {
                        var client = new NativeTestClientDnsQueryEx(_settings, _runs, _query);
                        tasks.Add(() =>
                        {
                            results.Add(client.Run().Result);
                        });
                    }

                    Parallel.Invoke(new ParallelOptions() { MaxDegreeOfParallelism = 32 }, tasks.ToArray());
                    return Task.FromResult(results.ToArray());
                });
            }
        }

        private class Spiner
        {
            private ConsoleColor _oldColor;
            private CancellationTokenSource _source;
            private CancellationToken _token;
            public void Start()
            {
                Console.CursorVisible = false;
                Console.Write("Running...");
                _oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;

                _source = new CancellationTokenSource();
                _token = _source.Token;
                Task.Run(Spin, _token);
            }

            public void Stop()
            {
                _source.Cancel();

                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write("\t\t\t\t   ");
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.CursorVisible = true;
                Console.ForegroundColor = _oldColor;
            }

            private async Task Spin()
            {
                var chars = new System.Collections.Generic.Queue<string>(new[] { "|", "/", "-", "\\" });

                while (true)
                {
                    var chr = chars.Dequeue();
                    chars.Enqueue(chr);
                    Console.Write("{" + chr + "}");
                    Console.SetCursorPosition(Console.CursorLeft - 3, Console.CursorTop);
                    await Task.Delay(100, _token);
                }
            }
        }
    }
}