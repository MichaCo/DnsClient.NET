using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DnsClient;
using Microsoft.Extensions.CommandLineUtils;

namespace DigApp
{
    public class PerfCommand2 : DnsCommand
    {
        private int _clients;
        private string _query;
        private int _runtime;
        private long _reportExcecutions = 0;
        private long _allExcecutions = 0;
        private long _allAvgExec = 0;
        private bool _running;
        private LookupSettings _settings;
        private LookupClient _lookup;
        private int _errors;
        private int _success;
        private Spiner _spinner;

        public CommandOption ClientsArg { get; private set; }

        public CommandArgument QueryArg { get; private set; }

        public CommandOption RuntimeArg { get; private set; }

        public PerfCommand2(CommandLineApplication app, string[] originalArgs) : base(app, originalArgs)
        {
        }

        protected override void Configure()
        {
            QueryArg = App.Argument("query", "the domain query to run.", false);
            ClientsArg = App.Option("-c | --clients", "Number of clients to run", CommandOptionType.SingleValue);
            RuntimeArg = App.Option("-r | --runtime", "Time in seconds to run", CommandOptionType.SingleValue);
            base.Configure();
        }

        protected override async Task<int> Execute()
        {
            _clients = ClientsArg.HasValue() ? int.Parse(ClientsArg.Value()) : 10;
            _runtime = RuntimeArg.HasValue() ? int.Parse(RuntimeArg.Value()) <= 1 ? 5 : int.Parse(RuntimeArg.Value()) : 5;
            _query = string.IsNullOrWhiteSpace(QueryArg.Value) ? string.Empty : QueryArg.Value;
            _lookup = GetDnsLookup();
            _running = true;
            _settings = GetLookupSettings();

            Console.WriteLine($"; <<>> Starting perf run with {_clients} clients and {_runtime} queries per client <<>>");
            Console.WriteLine($"; ({_settings.Endpoints.Length} Servers, caching:{_settings.UseCache}, minttl:{_settings.MinTTL.TotalMilliseconds})");
            _spinner = new Spiner();
            _spinner.Start();

            var sw = Stopwatch.StartNew();

            var tasks = new List<Task>();
            for (var clientIndex = 0; clientIndex < _clients; clientIndex++)
            {
                tasks.Add(ExcecuteRun());
            }

            tasks.Add(CollectPrint());

            await Task.WhenAny(tasks.ToArray());

            double elapsedSeconds = sw.ElapsedMilliseconds / 1000d;

            // results
            _spinner.Stop();

            Console.WriteLine(string.Join("-", Enumerable.Repeat("-", 50)));
            Console.WriteLine($";; results:\t\t");
            Console.WriteLine(string.Join("-", Enumerable.Repeat("-", 50)));

            var avgRuntime = _allExcecutions / _runtime;
            Console.WriteLine($";; run for {elapsedSeconds}sec {_clients} clients.");

            var successPercent = _errors == 0 ? 100 : _success == 0 ? 0 : (100 - _success / (_errors * (double)_success));
            Console.WriteLine($";; {_errors:N0} errors {_success:N0} ok {successPercent:N0}% success.");

            var execPerSec = _allExcecutions / _runtime;
            var avgExec = _allAvgExec / _runtime;
            Console.WriteLine($";; {execPerSec:N2} queries per second.");

            Console.WriteLine($";;Log: arraysAllocated: {StaticLog.ByteArrayAllocations} arraysReleased: {StaticLog.ByteArrayReleases} queries: {StaticLog.SyncResolveQueryCount} queryTries: {StaticLog.SyncResolveQueryTries}");
            return 0;
        }

        private async Task CollectPrint()
        {
            var waitCount = 0;
            while (waitCount < _runtime)
            {
                waitCount++;
                await Task.Delay(1000);

                //long avgExec = _reportExcecutions / 1000;
                //Interlocked.Add(ref _allAvgExec, avgExec);
                _spinner.Message = $"Requests per sec: {_reportExcecutions:N2}.";
                Interlocked.Exchange(ref _reportExcecutions, 0);
            }
            _running = false;
        }

        private async Task ExcecuteRun()
        {
            //var swatch = Stopwatch.StartNew();
            while (_running)
            {
                try
                {
                    var queryResult = await _lookup.QueryAsync(_query, QueryType.A);
                    Interlocked.Increment(ref _allExcecutions);
                    Interlocked.Increment(ref _reportExcecutions);
                    //Interlocked.Add(ref _allExecTime, swatch.ElapsedMilliseconds);
                    //swatch.Restart();
                    if (queryResult.HasError)
                    {
                        Interlocked.Increment(ref _errors);
                    }
                    else
                    {
                        Interlocked.Increment(ref _success);
                    }
                }
                catch
                {
                    Interlocked.Increment(ref _errors);
                }
            }
        }

        private class Spiner
        {
            private ConsoleColor _oldColor;
            private CancellationTokenSource _source;
            private CancellationToken _token;

            public string Message { get; set; }

            public void Start()
            {
                Console.CursorVisible = false;
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
                Console.Write("\t\t\t\t\t\t");
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.CursorVisible = true;
                Console.ForegroundColor = _oldColor;
            }

            private async Task Spin()
            {
                var chars = new Queue<string>(new[] { "|", "/", "-", "\\" });

                while (true)
                {
                    _token.ThrowIfCancellationRequested();
                    var chr = chars.Dequeue();
                    chars.Enqueue(chr);
                    Console.CursorVisible = false;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("{" + chr + "} " + Message);
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.ForegroundColor = _oldColor;
                    Console.CursorVisible = true;
                    await Task.Delay(100, _token);
                }
            }
        }
    }
}