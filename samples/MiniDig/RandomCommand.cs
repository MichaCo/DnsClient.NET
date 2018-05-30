using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DnsClient;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace DigApp
{
    public class RandomCommand : DnsCommand
    {
        private static ConcurrentQueue<string> _domainNames;
        private static readonly object _nameLock = new object();
        private static Random _randmom = new Random();
        private int _clients;
        private int _runtime;
        private long _reportExcecutions = 0;
        private long _allExcecutions = 0;
        private bool _running;
        private LookupSettings _settings;
        private LookupClient _lookup;
        private int _errors;
        private ConcurrentDictionary<string, int> _errorsPerCode = new ConcurrentDictionary<string, int>();
        private int _success;
        private Spiner _spinner;
        private bool _runSync;

        public CommandOption ClientsArg { get; private set; }

        public CommandOption RuntimeArg { get; private set; }

        public CommandOption SyncArg { get; private set; }

        static RandomCommand()
        {
            var lines = File.ReadAllLines("names.txt");
            _domainNames = new ConcurrentQueue<string>(lines.Select(p => p.Substring(p.IndexOf(',') + 1)));
        }

        public RandomCommand(CommandLineApplication app, ILoggerFactory loggerFactory, string[] originalArgs) : base(app, loggerFactory, originalArgs)
        {
        }

        public static string NextDomainName()
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
            RuntimeArg = App.Option("-r | --runtime", "Time in seconds to run", CommandOptionType.SingleValue);
            SyncArg = App.Option("--sync", "Run synchronous api", CommandOptionType.NoValue);
            base.Configure();
        }

        protected override async Task<int> Execute()
        {
            _clients = ClientsArg.HasValue() ? int.Parse(ClientsArg.Value()) : 10;
            _runtime = RuntimeArg.HasValue() ? int.Parse(RuntimeArg.Value()) <= 1 ? 5 : int.Parse(RuntimeArg.Value()) : 5;
            _runSync = SyncArg.HasValue();
            _lookup = GetDnsLookup();
            _lookup.EnableAuditTrail = true;
            _running = true;
            _settings = GetLookupSettings();

            Console.WriteLine($"; <<>> Starting random run with {_clients} clients running for {_runtime} seconds <<>>");
            Console.WriteLine($"; ({_settings.Endpoints.Length} Servers, caching:{_settings.UseCache}, minttl:{_settings.MinTTL.TotalMilliseconds})");
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
                await Task.Delay(1000);

                _spinner.Status = $"{_reportExcecutions:N2} req/sec";
                Interlocked.Exchange(ref _reportExcecutions, 0);
            }
            _running = false;
        }

        private async Task ExcecuteRun()
        {
            //var swatch = Stopwatch.StartNew();
            while (_running)
            {
                var query = NextDomainName();
                try
                {
                    _spinner.Message = query;
                    IDnsQueryResponse response;
                    if (!_runSync)
                    {
                        response = await _lookup.QueryAsync(query, QueryType.ANY);
                    }
                    else
                    {
                        response = await Task.Run(() => _lookup.Query(query, QueryType.ANY));
                        await Task.Delay(0);
                    }

                    if (Logger.IsEnabled(LogLevel.Information))
                    {
                        Logger.LogInformation("Response received for {0} {1} {2}", query, response.Header, response.Answers.ARecords().FirstOrDefault());
                    }

                    Interlocked.Increment(ref _allExcecutions);
                    Interlocked.Increment(ref _reportExcecutions);
                    if (response.HasError)
                    {
                        Logger.LogWarning("Response has error.\n{0}", response.AuditTrail);
                        _errorsPerCode.AddOrUpdate(response.Header.ResponseCode.ToString(), 1, (c, v) => v + 1);
                        Interlocked.Increment(ref _errors);
                    }
                    else
                    {
                        Interlocked.Increment(ref _success);
                    }
                }
                catch (DnsResponseException ex)
                {
                    _errorsPerCode.AddOrUpdate(ex.Code.ToString(), 1, (c, v) => v + 1);
                    Logger.LogError(101, ex, "Error running query {0}.", query);
                    Interlocked.Increment(ref _errors);
                }
                catch (Exception ex)
                {
                    _errorsPerCode.AddOrUpdate(ex.GetType().Name, 1, (c, v) => v + 1);
                    Logger.LogError(101, ex, "Error running query {0}.", query);
                    Interlocked.Increment(ref _errors);
                }
            }
        }
    }
}