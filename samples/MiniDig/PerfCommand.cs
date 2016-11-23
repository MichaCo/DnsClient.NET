using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DnsClient;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace DigApp
{
    internal struct PerfResult
    {
        public List<double> Times { get; set; }

        public int SuccessResponses { get; set; }

        public int ErrorResponses { get; set; }

        public long TimeTakenMs { get; set; }

        public override string ToString()
        {
            Times.Sort();
            var median = Times.ElementAt(Times.Count / 2);
            return $":: {TimeTakenMs,-15:N0} {SuccessResponses,-10} {ErrorResponses,-10} {Times.Min(),-10:N4}{Times.Max(),-10:N2}{Times.Average(),-10:N4}{median,-10:N4}";
        }
    }

    internal class PerfClient
    {
        private readonly LookupClient _lookup;
        private readonly string _query;
        private readonly int _runs;

        public PerfClient(LookupSettings settings, int runs, string query)
        {
            _lookup = new LookupClient(settings.Endpoints)
            {
                Recursion = settings.Recursion,
                Retries = settings.Retries,
                Timeout = settings.Timeout,
                UseCache = settings.UseCache,
                MimimumCacheTimeout = settings.MinTTL
            };

            _query = query;
            _runs = runs;
        }

        public async Task<PerfResult> Run()
        {
            var result = new PerfResult();
            result.Times = new List<double>();

            var swatchReq = Stopwatch.StartNew();
            for (var index = 0; index < _runs; index++)
            {
                swatchReq.Restart();
                var queryResult = await _lookup.QueryAsync(_query, QueryType.ANY);
                var responseElapsed = swatchReq.ElapsedTicks / 10000d;
                if (queryResult.HasError)
                {
                    result.ErrorResponses++;
                }
                else
                {
                    result.SuccessResponses++;
                }

                result.Times.Add(responseElapsed);

                // delay each request a little
                await Task.Delay(0);
            }

            result.TimeTakenMs = (long)result.Times.Sum();
            return result;
        }
    }

    internal class PerfClientNative
    {
        private readonly LookupClient _lookup;
        private readonly string _query;
        private readonly int _runs;

        public PerfClientNative(LookupSettings settings, int runs, string query)
        {
            _query = query;
            _runs = runs;
        }

        public PerfResult Run()
        {
            var result = new PerfResult();
            result.Times = new List<double>();
            var swatchReq = Stopwatch.StartNew();
            for (var index = 0; index < _runs; index++)
            {
                swatchReq.Restart();
                try
                {
                    var queryResult = Interop.Dns.GetMxRecords(_query);
                    result.SuccessResponses++;
                }
                catch (Exception)
                {
                    result.ErrorResponses++;
                }
                var responseElapsed = swatchReq.ElapsedTicks / 10000d;

                result.Times.Add(responseElapsed);
            }

            result.TimeTakenMs = (long)result.Times.Sum();

            return result;
        }
    }

    internal class PerfCommand : DnsCommand
    {
        public CommandOption ClientsArg { get; private set; }

        public CommandArgument QueryArg { get; private set; }

        public CommandOption RunsArg { get; private set; }

        public PerfCommand(CommandLineApplication app, string[] originalArgs) : base(app, originalArgs)
        {
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
            var useQuery = string.IsNullOrWhiteSpace(QueryArg.Value) ? "google.com" : QueryArg.Value;
            var lookup = GetDnsLookup();

            var loggerFactory = new LoggerFactory().AddConsole(GetLoglevelValue());
            var logger = loggerFactory.CreateLogger("Dig_Perf");

            var settings = GetLookupSettings();
            var runner = new PerfRunner(settings, useClients, useRuns, useQuery);
            await runner.Run();

            return 0;
        }
    }

    internal class PerfRunner
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

            // warmup
            //var tasks = new List<Task<PerfResult>>();

            //Console.Write(";; Warming up ");
            //for (var i = 0; i < 10; i++)
            //{
            //    Console.Write(".");
            //    var client = new PerfClient(_settings, 2, _query);
            //    tasks.Add(client.Run());
            //}
            //Console.Write("\r");
            //await Task.WhenAll(tasks);

            //await ManagedTest();
            await NativeTest();
        }

        private async Task ManagedTest()
        {
            // managed test
            var sw = Stopwatch.StartNew();
            var tasks = new List<Task<PerfResult>>();

            for (var i = 0; i < _clients; i++)
            {
                var client = new PerfClient(_settings, _runs, _query);
                tasks.Add(client.Run());
            }

            await Task.WhenAll(tasks);

            var elapsed = sw.ElapsedMilliseconds;
            var results = tasks.Select(p => p.Result);

            Console.WriteLine($";; Managed Results per client:\t\t");
            Console.WriteLine($";; {"Overall(ms)",-15} {"OK",-10} {"Errors",-10} {"MIN(ms)",-10}{"MAX(ms)",-10}{"AVG(ms)",-10}{"Median",-10}");
            foreach (var result in results)
            {
                Console.WriteLine(result);
            }

            var avgRuntime = (1000.0d / elapsed) * (_clients * _runs);
            Console.WriteLine($";; Run finished after {elapsed}ms for {_clients} clients and {_runs} queries => {avgRuntime:N0}queries per second.");
        }

        private async Task NativeTest()
        {
            // native test
            var sw = Stopwatch.StartNew();
            var tasks = new List<Action>();
            var results = new System.Collections.Concurrent.ConcurrentBag<PerfResult>();
            for (var i = 0; i < _clients; i++)
            {
                var client = new PerfClientNative(_settings, _runs, _query);
                tasks.Add(() =>
                {
                    results.Add(client.Run());
                });
            }

            Parallel.Invoke(new ParallelOptions() { MaxDegreeOfParallelism = 8 }, tasks.ToArray());

            var elapsed = sw.ElapsedMilliseconds;

            Console.WriteLine($";; Native Results per client:\t\t");
            Console.WriteLine($";; {"Overall(ms)",-15} {"OK",-10} {"Errors",-10} {"MIN(ms)",-10}{"MAX(ms)",-10}{"AVG(ms)",-10}{"Median",-10}");
            foreach (var result in results)
            {
                Console.WriteLine(result);
            }

            var avgRuntime = (1000.0d / elapsed) * (_clients * _runs);
            Console.WriteLine($";; Run finished after {elapsed}ms for {_clients} clients and {_runs} queries => {avgRuntime:N0}queries per second.");
        }
    }
}