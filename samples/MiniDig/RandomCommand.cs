// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DnsClient;
using McMaster.Extensions.CommandLineUtils;

namespace DigApp
{
    public class RandomCommand : DnsCommand
    {
        private static readonly Random s_randmom = new Random();

        private readonly ConcurrentDictionary<string, int> _errorsPerCode = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<NameServer, int> _successByServer = new ConcurrentDictionary<NameServer, int>();
        private readonly ConcurrentDictionary<NameServer, int> _failByServer = new ConcurrentDictionary<NameServer, int>();

        private ConcurrentQueue<string> _domainNames;
        private int _clients;
        private int _runtime;
        private long _reportExcecutions;
        private long _allExcecutions;
        private bool _running;
        private LookupClientOptions _settings;
        private int _errors;
        private int _success;
        private Spiner _spinner;
        private bool _runSync;

        public CommandOption ClientsArg { get; private set; }

        public CommandOption RuntimeArg { get; private set; }

        public CommandOption SyncArg { get; private set; }

        public RandomCommand(CommandLineApplication app, string[] originalArgs) : base(app, originalArgs)
        {
        }

        public string NextDomainName()
        {
            while (true)
            {
                if (_domainNames.TryDequeue(out string result))
                {
                    _domainNames.Enqueue(result);

                    return result;
                }
            }
        }

        protected override void Configure()
        {
            ClientsArg = App.Option("-c | --clients", "Number of clients to run", CommandOptionType.SingleValue);
            RuntimeArg = App.Option("-r | --run", "Time in seconds to run", CommandOptionType.SingleValue);
            SyncArg = App.Option("--sync", "Run synchronous api", CommandOptionType.NoValue);
            base.Configure();
        }

        protected override async Task<int> Execute()
        {
            var lines = File.ReadAllLines("names.txt");
            _domainNames = new ConcurrentQueue<string>(lines.Select(p => p.Substring(p.IndexOf(';') + 1)).OrderBy(x => s_randmom.Next(0, lines.Length * 2)));

            _clients = ClientsArg.HasValue() ? int.Parse(ClientsArg.Value(), CultureInfo.InvariantCulture) : 10;
            _runtime = RuntimeArg.HasValue()
                ? int.Parse(RuntimeArg.Value(), CultureInfo.InvariantCulture) <= 1
                ? 5 : int.Parse(RuntimeArg.Value(), CultureInfo.InvariantCulture)
                : 5;

            _runSync = SyncArg.HasValue();

            _settings = GetLookupSettings();
            _settings.EnableAuditTrail = false;
            _settings.ThrowDnsErrors = false;
            _settings.ContinueOnDnsError = false;
            _running = true;

            Console.WriteLine($"; <<>> Starting random run with {_clients} clients running for {_runtime} seconds <<>>");
            Console.WriteLine($"; ({_settings.NameServers.Count} Servers, caching:{_settings.UseCache}, minttl:{_settings.MinimumCacheTimeout?.TotalMilliseconds})");
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
            try
            {
                await Task.WhenAny(tasks.ToArray()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            double elapsedSeconds = sw.ElapsedMilliseconds / 1000d;

            // results
            _spinner.Stop();

            Console.WriteLine(string.Join("-", Enumerable.Repeat("-", 50)));
            Console.WriteLine($";; results:\t\t");
            Console.WriteLine(string.Join("-", Enumerable.Repeat("-", 50)));
            Console.WriteLine($";; run for {elapsedSeconds}sec {_clients} clients.");

            //var successPercent = _errors == 0 ? 100 : _success == 0 ? 0 : (100 - (double)_success / (_errors * (double)_success));

            var successPercent = _errors == 0 ? 100 : _success == 0 ? 0 : (100 - ((double)_errors / (_success) * 100));
            Console.WriteLine($";; {_errors:N0} errors {_success:N0} ok {successPercent:N2}% success.");
            foreach (var code in _errorsPerCode.Keys)
            {
                Console.WriteLine($"{code,30}:\t {_errorsPerCode[code]}");
            }

            var execPerSec = _allExcecutions / elapsedSeconds;
            Console.WriteLine($";; {execPerSec:N2} queries per second.");
            return 0;
        }

        private async Task CollectPrint()
        {
            var waitCount = 0;
            while (_running && waitCount < _runtime)
            {
                waitCount++;
                await Task.Delay(1000).ConfigureAwait(false);

                var serverUpdate =
                    from good in _successByServer
                    join fail in _failByServer on good.Key equals fail.Key into all
                    from row in all.DefaultIfEmpty()
                    select new
                    {
                        good.Key,
                        Fails = row.Value,
                        Success = good.Value
                    };

                var updateString = string.Join(" | ", serverUpdate.Select((p, i) => $"Server{i}: +{p.Success} -{p.Fails}"));

                _spinner.Status = $"{_reportExcecutions:N2} req/sec {_allExcecutions:N0} total - [{updateString}]";
                Interlocked.Exchange(ref _reportExcecutions, 0);
            }
            _running = false;
        }

        private int _runNumber;

        private async Task ExcecuteRun()
        {
            var number = Interlocked.Increment(ref _runNumber);
            var lookup = GetDnsLookup(_settings);

            //var swatch = Stopwatch.StartNew();
            while (_running)
            {
                var query = NextDomainName();

                try
                {
                    IDnsQueryResponse response = null;

                    foreach (var type in new[]
                    {
                        QueryType.A,
                        QueryType.AAAA,
                        QueryType.MX,
                        QueryType.NS,
                        QueryType.SOA,
                        QueryType.TXT,
                        QueryType.CAA
                    })
                    {
                        _spinner.Message = $"[{number}] {query} {type}";

                        if (!_runSync)
                        {
                            response = await lookup.QueryAsync(query, type).ConfigureAwait(false);
                        }
                        else
                        {
                            response = await Task.Run(() => lookup.Query(query, type)).ConfigureAwait(false);
                        }

                        Interlocked.Increment(ref _allExcecutions);
                        Interlocked.Increment(ref _reportExcecutions);
                        if (response.HasError)
                        {
                            _errorsPerCode.AddOrUpdate(response.Header.ResponseCode.ToString(), 1, (c, v) => v + 1);
                            _failByServer.AddOrUpdate(response.NameServer, 1, (n, v) => v + 1);
                            Interlocked.Increment(ref _errors);
                        }
                        else
                        {
                            _successByServer.AddOrUpdate(response.NameServer, 1, (n, v) => v + 1);
                            Interlocked.Increment(ref _success);
                        }
                    }
                }
                catch (DnsResponseException ex)
                {
                    _errorsPerCode.AddOrUpdate(ex.Code.ToString(), 1, (c, v) => v + 1);
                    Interlocked.Increment(ref _errors);
                }
                catch (Exception ex)
                {
                    _errorsPerCode.AddOrUpdate(ex.GetType().Name, 1, (c, v) => v + 1);
                    Interlocked.Increment(ref _errors);
                }
            }
        }
    }
}
