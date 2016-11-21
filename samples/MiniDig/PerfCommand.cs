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
    internal class PerfClient
    {
        private readonly int _id;
        private readonly ILogger<PerfClient> _logger;
        private readonly LookupClient _lookup;
        private readonly string _query;
        private readonly int _runs;

        public PerfClient(int clientId, ILoggerFactory loggerFactory, LookupClient lookup, int runs, string query)
        {
            lookup.UseCache = false;

            _query = query;
            _id = clientId;
            _lookup = lookup;
            _runs = runs;
            _logger = loggerFactory.CreateLogger<PerfClient>();
            _logger.LogInformation("PerfClient2 started...");
        }

        public async Task Run()
        {
            for (var index = 0; index < _runs; index++)
            {
                var result = await _lookup.QueryAsync(_query, QueryType.ANY);
                _logger.LogInformation("[{0}] Query to {1} {2}/{3} => {4} answers.", _id, _query, index + 1, _runs, result.Answers.Count);
            }
        }
    }

    internal class PerfCommand : DnsCommand
    {
        public CommandOption ClientsArg { get; private set; }

        public CommandArgument QueryArg { get; private set; }

        public CommandOption RunsArg { get; private set; }

        public CommandOption ClientsTypeAArg { get; private set; }

        public CommandOption ClientsTypeBArg { get; private set; }

        public PerfCommand(CommandLineApplication app, string[] originalArgs) : base(app, originalArgs)
        {
        }

        protected override void Configure()
        {
            QueryArg = App.Argument("query", "the domain query to run.", false);
            ClientsArg = App.Option("-c | --clients", "Number of clients to run", CommandOptionType.SingleValue);
            RunsArg = App.Option("-r | --runs", "Number of runs", CommandOptionType.SingleValue);
            ClientsTypeAArg = App.Option("-1", "Use old client", CommandOptionType.NoValue);
            ClientsTypeBArg = App.Option("-2", "Use old client", CommandOptionType.NoValue);
            base.Configure();
        }

        protected override async Task<int> Execute()
        {
            var useClients = ClientsArg.HasValue() ? int.Parse(ClientsArg.Value()) : 10;
            var useRuns = RunsArg.HasValue() ? int.Parse(RunsArg.Value()) : 100;
            var useQuery = string.IsNullOrWhiteSpace(QueryArg.Value) ? "google.com" : QueryArg.Value;
            var lookup = GetDnsLookup();
            var useImpl = ClientsTypeAArg.HasValue() ? 0 : ClientsTypeBArg.HasValue() ? 1 : 0;

            var loggerFactory = new LoggerFactory().AddConsole(GetLoglevelValue());
            var logger = loggerFactory.CreateLogger("Dig_Perf");

            var runner = new PerfRunner(loggerFactory, lookup, useClients, useRuns, useQuery, useImpl);
            await runner.Run();

            return 0;
        }
    }

    internal class PerfRunner
    {
        private readonly int _clients;
        private readonly ILoggerFactory _loggerFactory;
        private readonly LookupClient _lookup;
        private readonly string _query;
        private readonly int _runs;
        private readonly int _useImpl;

        public PerfRunner(ILoggerFactory loggerFactory, LookupClient lookup, int clients, int runs, string query, int useImpl)
        {
            _useImpl = useImpl;
            _query = query;
            _loggerFactory = loggerFactory;
            _lookup = lookup;
            _clients = clients;
            _runs = runs;
        }

        public async Task Run()
        {
            Console.WriteLine($";;Starting perf run with {_clients} clients and {_runs} queries per client. Using impl {_useImpl}.");
            var sw = Stopwatch.StartNew();

            var tasks = new List<Task>();

            for (var i = 0; i < _clients; i++)
            {
                if (_useImpl == 0)
                {
                    var client = new PerfClient(i + 1, _loggerFactory, _lookup, _runs, _query);
                    tasks.Add(client.Run());
                }
                else
                {
                    var client = new PerfClient(i + 1, _loggerFactory, _lookup, _runs, _query);
                    tasks.Add(client.Run());
                }
            }

            await Task.WhenAll(tasks);

            var elapsed = sw.ElapsedMilliseconds;

            var avgRuntime = (1000.0d / elapsed) * (_clients * _runs);
            Console.WriteLine($";;Run finished after {elapsed}ms for {_clients} clients and {_runs} queries => {avgRuntime:N0}queries per second.");
        }
    }
}