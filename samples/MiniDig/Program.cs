using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DnsClient;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace ConsoleApp4
{
    public class PerfClient
    {
        private readonly DnsLookup lookup;

        public PerfClient()
        {
            lookup = new DnsLookup();
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false);

            var perfApplication = app.Command(
                "perf",
                (perfApp) => new PerfCommand(perfApp, args),
                false);

            var defaultCommand = new DigCommand(app, args);

            app.Execute(args);
        }
    }

    internal class DigCommand : DnsCommand
    {
        public static string OS
        {
            get
            {
                return RuntimeInformation.OSDescription.Trim();
            }
        }

        public static string Version
        {
            get
            {
                return typeof(DnsLookup).GetTypeInfo().Assembly.GetName().Version.ToString();
            }
        }

        public CommandArgument DomainArg { get; }

        public CommandArgument QClassArg { get; }

        public CommandArgument QTypeArg { get; }

        public DigCommand(CommandLineApplication app, string[] originalArgs) : base(app, originalArgs)
        {
            DomainArg = app.Argument("domain", "domain name", false);
            QTypeArg = app.Argument("q-type", "QType", false);
            QClassArg = app.Argument("q-class", "QClass", false);
        }

        protected override async Task<int> Execute()
        {
            var loggerFactory = new LoggerFactory().AddConsole(GetLoglevelValue());

            string useDomain = string.IsNullOrWhiteSpace(DomainArg.Value) ? "." : DomainArg.Value;
            QType useQType = 0;
            QClass useQClass = 0;

            if (!string.IsNullOrWhiteSpace(QClassArg.Value))
            {
                // q class != null => 3 params (domain already set above).
                Enum.TryParse(QClassArg.Value, true, out useQClass);
                Enum.TryParse(QTypeArg.Value, true, out useQType);
            }
            else
            {
                // q class is null => 2 params only
                // test if no domain is specified and first param is either qtype or qclass
                if (Enum.TryParse(DomainArg.Value, true, out useQType))
                {
                    useDomain = ".";
                    Enum.TryParse(QTypeArg.Value, true, out useQClass);
                }
                else if (Enum.TryParse(DomainArg.Value, true, out useQClass))
                {
                    useDomain = ".";
                }
                else if (!Enum.TryParse(QTypeArg.Value, true, out useQType))
                {
                    // could be q class as second and no QType
                    Enum.TryParse(QTypeArg.Value, true, out useQClass);
                }
            }

            if (useQType == 0)
            {
                if (string.IsNullOrWhiteSpace(useDomain) || useDomain == ".")
                {
                    useQType = QType.NS;
                }
                else
                {
                    useQType = QType.A;
                }
            }

            // finally running the command
            DnsLookupOptions options = GetDnsLookupOptions();
            var lookup = new DnsLookup(loggerFactory, options);

            var swatch = Stopwatch.StartNew();

            var result = useQClass == 0 ?
                await lookup.QueryAsync(useDomain, useQType) :
                await lookup.QueryAsync(useDomain, useQType, useQClass);

            var elapsed = swatch.ElapsedMilliseconds;

            // Printing infomrational stuff
            var useServers = GetEndpointsValue();

            Console.WriteLine();
            Console.WriteLine($"; <<>> MiniDiG {Version} {OS} <<>> {string.Join(" ", OriginalArgs)}");
            Console.WriteLine($"; ({useServers.Length} server found)");

            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                Console.WriteLine($";; {result.Error}");
            }
            else
            {
                Console.WriteLine(";; Got answer:");
                Console.WriteLine($";; ->>HEADER<<- opcode: {result.Header.OPCode}, id: {result.Header.Id}");
                var flags = new string[] {
                        result.Header.HasQuery ? "qr" : "",
                        result.Header.RecursionAvailable ? "ra" : "",
                        result.Header.RecursionEnabled ? "rd" : "",
                        result.Header.TruncationEnabled ? "tc" : ""
                    };
                var flagsString = string.Join(" ", flags.Where(p => p != ""));

                Console.WriteLine($";; flags: {flagsString}; QUERY: {result.Header.QuestionCount}, ANSWER: {result.Header.AnswerCount}, AUTORITY: {result.Header.NameServerCount}, ADDITIONAL: {result.Header.AdditionalCount}");
                Console.WriteLine();

                if (result.Questions.Count > 0)
                {
                    Console.WriteLine(";; QUESTION SECTION:");
                    foreach (var question in result.Questions)
                    {
                        Console.WriteLine(question);
                    }

                    Console.WriteLine();
                }

                if (result.Answers.Count > 0)
                {
                    Console.WriteLine(";; ANSWER SECTION:");
                    foreach (var answer in result.Answers)
                    {
                        Console.WriteLine(answer);
                    }

                    Console.WriteLine();
                }

                if (result.Additionals.Count > 0)
                {
                    Console.WriteLine(";; ADDITIONALS SECTION:");
                    foreach (var additional in result.Additionals)
                    {
                        Console.WriteLine(additional);
                    }

                    Console.WriteLine();
                }

                if (result.Authorities.Count > 0)
                {
                    Console.WriteLine(";; AUTHORITIES SECTION:");
                    foreach (var auth in result.Authorities)
                    {
                        Console.WriteLine(auth);
                    }

                    Console.WriteLine();
                }
            }

            // footer
            Console.WriteLine($";; Query time: {elapsed:N0} msec");
            Console.WriteLine($";; SERVER: {useServers.FirstOrDefault()}");
            Console.WriteLine($";; WHEN: {DateTime.Now.ToString("R")}");

            return 0;
        }
    }

    internal abstract class DnsCommand
    {
        protected CommandLineApplication App { get; }

        public CommandOption LogLevelArg { get; private set; }

        public CommandOption ServerArg { get; private set; }

        public CommandOption PortArg { get; private set; }

        public CommandOption UseTcpArg { get; private set; }

        public CommandOption TriesArg { get; private set; }

        public CommandOption ConnectTimeoutArg { get; private set; }

        public CommandOption NoRecurseArg { get; private set; }

        public string[] OriginalArgs { get; }

        public DnsCommand(CommandLineApplication app, string[] originalArgs)
        {
            App = app;
            OriginalArgs = originalArgs;
            App.OnExecute(() => Execute());
            Configure();
        }

        public DnsLookupOptions GetDnsLookupOptions()
        {
            return new DnsLookupOptions(GetEndpointsValue())
            {
                TransportType = GetTransportTypeValue(),
                Recursion = GetUseRecursionValue(),
                Retries = GetTriesValue(),
                Timeout = GetTimeoutValue()
            };
        }

        public IPEndPoint[] GetEndpointsValue() =>
            ServerArg.HasValue() ?
                new[] { new IPEndPoint(IPAddress.Parse(ServerArg.Value()), GetPortValue()) } :
                DnsLookup.GetDnsServers();

        public LogLevel GetLoglevelValue()
        {
            LogLevel logginglevel;
            return LogLevelArg.HasValue() && Enum.TryParse(LogLevelArg.Value(), true, out logginglevel) ? logginglevel : LogLevel.Warning;
        }

        public bool GetUseTcpValue() => UseTcpArg.HasValue();

        public TransportType GetTransportTypeValue() => UseTcpArg.HasValue() ? TransportType.Tcp : TransportType.Udp;

        public int GetPortValue() => PortArg.HasValue() ? int.Parse(PortArg.Value()) : 53;

        public int GetTriesValue() => TriesArg.HasValue() ? int.Parse(TriesArg.Value()) : 3;

        public bool GetUseRecursionValue() => !NoRecurseArg.HasValue();

        public int GetTimeoutValue() => ConnectTimeoutArg.HasValue() ? int.Parse(ConnectTimeoutArg.Value()) : 1000;

        protected abstract Task<int> Execute();

        protected virtual void Configure()
        {
            ServerArg = App.Option(
                "-s | --server",
                "The DNS server [local network].",
                CommandOptionType.SingleValue);

            PortArg = App.Option(
                "-p | --port",
                $"The port to use to connect to the DNS server [{DnsLookup.DefaultPort}].",
                CommandOptionType.SingleValue);

            NoRecurseArg = App.Option(
                "-nr | --norecurse",
                "Non recurive mode.",
                CommandOptionType.NoValue);

            UseTcpArg = App.Option(
                "--tcp",
                "Use TCP connection [Udp].",
                CommandOptionType.NoValue);

            TriesArg = App.Option(
                "--tries",
                "Number of tries [3].",
                CommandOptionType.SingleValue);

            ConnectTimeoutArg = App.Option(
                "--time",
                "Query timeout [1000].",
                CommandOptionType.SingleValue);

            LogLevelArg = App.Option(
                "--log-level",
                "Sets the log level [Warning].",
                CommandOptionType.SingleValue);

            App.HelpOption("-? | -h | --help");
        }
    }

    internal class PerfCommand : DnsCommand
    {
        public CommandOption Clients { get; private set; }

        public CommandOption Runs { get; private set; }

        public PerfCommand(CommandLineApplication app, string[] originalArgs) : base(app, originalArgs)
        {
        }

        protected override async Task<int> Execute()
        {
            var useClients = Clients.HasValue() ? int.Parse(Clients.Value()) : 10;
            var useRuns = Runs.HasValue() ? int.Parse(Runs.Value()) : 100;
            var usePort = PortArg.HasValue() ? int.Parse(PortArg.Value()) : 53;
            var useServers = ServerArg.HasValue() ?
                new[] { new IPEndPoint(IPAddress.Parse(ServerArg.Value()), usePort) } :
                DnsLookup.GetDnsServers();

            LogLevel logginglevel;
            logginglevel = LogLevelArg.HasValue() && Enum.TryParse(LogLevelArg.Value(), true, out logginglevel) ? logginglevel : LogLevel.Information;
            var loggerFactory = new LoggerFactory().AddConsole(logginglevel);
            var logger = loggerFactory.CreateLogger("Dig_Perf");

            var options = new DnsLookupOptions(useServers)
            {
                TransportType = UseTcpArg.HasValue() ? TransportType.Tcp : TransportType.Udp,
                Retries = TriesArg.HasValue() ? int.Parse(TriesArg.Value()) : 3,
                Recursion = !NoRecurseArg.HasValue(),
                Timeout = ConnectTimeoutArg.HasValue() ? int.Parse(ConnectTimeoutArg.Value()) : 1000
            };

            logger.LogInformation($"Starting perf run with {useClients} clients and {useRuns} runs per client.");

            return 0;
        }

        protected override void Configure()
        {
            Clients = App.Option("-c | --clients", "Number of clients to run", CommandOptionType.SingleValue);
            Runs = App.Option("-r | --runs", "Number of runs", CommandOptionType.SingleValue);
            base.Configure();
        }
    }
}