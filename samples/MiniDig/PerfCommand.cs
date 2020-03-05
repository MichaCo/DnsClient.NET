﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DnsClient;
using McMaster.Extensions.CommandLineUtils;

namespace DigApp
{
    public class PerfCommand : DnsCommand
    {
        private int _clients;
        private string _query;
        private int _runtime;
        private long _reportExcecutions = 0;
        private long _allExcecutions = 0;
        private bool _running;
        private LookupClientOptions _settings;
        private LookupClient _lookup;
        private int _errors;
        private int _success;
        private Spiner _spinner;
        private bool _runSync;

        public CommandOption ClientsArg { get; private set; }

        public CommandArgument QueryArg { get; private set; }

        public CommandOption RuntimeArg { get; private set; }

        public CommandOption SyncArg { get; private set; }

        public PerfCommand(CommandLineApplication app, string[] originalArgs) : base(app, originalArgs)
        {
        }

        protected override void Configure()
        {
            QueryArg = App.Argument("query", "the domain query to run.", false);
            ClientsArg = App.Option("-c | --clients", "Number of clients to run", CommandOptionType.SingleValue);
            RuntimeArg = App.Option("-r | --runtime", "Time in seconds to run", CommandOptionType.SingleValue);
            SyncArg = App.Option("--sync", "Run synchronous api", CommandOptionType.NoValue);
            base.Configure();
        }

        protected override async Task<int> Execute()
        {
            _clients = ClientsArg.HasValue() ? int.Parse(ClientsArg.Value()) : 10;
            _runtime = RuntimeArg.HasValue() ? int.Parse(RuntimeArg.Value()) <= 1 ? 5 : int.Parse(RuntimeArg.Value()) : 5;
            _query = string.IsNullOrWhiteSpace(QueryArg.Value) ? string.Empty : QueryArg.Value;
            _runSync = SyncArg.HasValue();

            _settings = GetLookupSettings();
            _settings.EnableAuditTrail = false;
            _lookup = GetDnsLookup(_settings);
            _running = true;

            Console.WriteLine($"; <<>> Starting perf run with {_clients} clients running for {_runtime} seconds <<>>");
            Console.WriteLine($"; ({_settings.NameServers.Count} Servers, caching:{_settings.UseCache}, minttl:{_settings.MinimumCacheTimeout?.TotalMilliseconds}, maxttl:{_settings.MaximumCacheTimeout?.TotalMilliseconds})");
            _spinner = new Spiner();
            _spinner.Start();

            var sw = Stopwatch.StartNew();

            var timeoutTask = Task.Delay(_runtime * 1000).ContinueWith((t) =>
            {
                _running = false;
            });

            var tasks = new List<Task>
            {
                timeoutTask
            };

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
            Console.WriteLine($";; run for {elapsedSeconds}sec {_clients} clients.");

            var successPercent = _errors == 0 ? 100 : _success == 0 ? 0 : (100 - ((double)_errors / (_success) * 100));
            Console.WriteLine($";; {_errors:N0} errors {_success:N0} ok {successPercent:N2}% success.");

            var execPerSec = _allExcecutions / elapsedSeconds;
            Console.WriteLine($";; {execPerSec:N2} queries per second.");
            return 0;
        }

        private async Task CollectPrint()
        {
            var waitCount = 0;
            while (waitCount < _runtime)
            {
                waitCount++;
                await Task.Delay(1000);

                _spinner.Message = $"Requests per sec: {_reportExcecutions:N2}.";
                Interlocked.Exchange(ref _reportExcecutions, 0);
            }
            _running = false;
        }

        private async Task ExcecuteRun()
        {
            //var swatch = Stopwatch.StartNew();
            var options = GetLookupSettings();
            options.EnableAuditTrail = false;
            var lookup = GetDnsLookup(options);

            while (_running)
            {
                try
                {
                    IDnsQueryResponse response;
                    if (!_runSync)
                    {
                        response = await lookup.QueryAsync(_query, QueryType.A);
                    }
                    else
                    {
                        response = await Task.Run(() => lookup.Query(_query, QueryType.A));
                        await Task.Delay(0);
                    }

                    Interlocked.Increment(ref _allExcecutions);
                    Interlocked.Increment(ref _reportExcecutions);
                    if (response.HasError)
                    {
                        Interlocked.Increment(ref _errors);
                    }
                    else
                    {
                        Interlocked.Increment(ref _success);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Interlocked.Increment(ref _errors);
                }
            }
        }
    }
}