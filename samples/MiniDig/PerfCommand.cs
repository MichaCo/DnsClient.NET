using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DnsClient;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace DigApp
{
    internal class PerfClient
    {
        private readonly int _id;
        private readonly ILogger<PerfClient> _logger;
        private readonly DnsLookup _lookup;
        private readonly string _query;
        private readonly int _runs;

        public PerfClient(int clientId, ILoggerFactory loggerFactory, DnsLookupOptions options, int runs, string query)
        {
            options.UseCache = true;

            _query = query;
            _id = clientId;
            _lookup = new DnsLookup(loggerFactory, options);
            _runs = runs;
            _logger = loggerFactory.CreateLogger<PerfClient>();
        }

        public async Task Run()
        {
            for (var index = 0; index < _runs; index++)
            {
                var result = await _lookup.QueryAsync(_query, QType.ANY);
                _logger.LogInformation("[{0}] Query to {1} {2}/{3} => {4} answers.", _id, _query, index + 1, _runs, result.Answers.Count);
            }
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
            var options = GetDnsLookupOptions();

            var loggerFactory = new LoggerFactory().AddConsole(GetLoglevelValue());
            var logger = loggerFactory.CreateLogger("Dig_Perf");

            var runner = new PerfRunner(loggerFactory, options, useClients, useRuns, useQuery);
            await runner.Run();

            return 0;
        }
    }

    internal class PerfRunner
    {
        private readonly int _clients;
        private readonly ILoggerFactory _loggerFactory;
        private readonly DnsLookupOptions _options;
        private readonly string _query;
        private readonly int _runs;

        public PerfRunner(ILoggerFactory loggerFactory, DnsLookupOptions options, int clients, int runs, string query)
        {
            _query = query;
            _loggerFactory = loggerFactory;
            _options = options;
            _clients = clients;
            _runs = runs;
        }

        public async Task Run()
        {
            Console.WriteLine($";;Starting perf run with {_clients} clients and {_runs} queries per client.");
            var sw = Stopwatch.StartNew();

            var tasks = new List<Task>();

            for (var i = 0; i < _clients; i++)
            {
                var client = new PerfClient(i + 1, _loggerFactory, _options, _runs, _query);
                tasks.Add(client.Run());
            }

            await Task.WhenAll(tasks);

            var elapsed = sw.ElapsedMilliseconds;

            var avgRuntime = (1000.0d / elapsed) * (_clients * _runs);
            Console.WriteLine($";;Run finished after {elapsed}ms for {_clients} clients and {_runs} queries => {avgRuntime:N0}queries per second.");
        }
    }
}