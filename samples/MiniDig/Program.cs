using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using DnsClient;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace ConsoleApp4
{
    public class Program
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
                return typeof(Client).GetTypeInfo().Assembly.GetName().Version.ToString();
            }
        }

        public static void Main(string[] args)
        {
            var loggerFactory = new LoggerFactory().AddConsole(LogLevel.Trace);

            CommandLineApplication commandLineApplication = new CommandLineApplication(throwOnUnexpectedArg: false);

            CommandOption server = commandLineApplication.Option(
                "-s | --server",
                "The DNS server.",
                CommandOptionType.SingleValue);

            CommandOption port = commandLineApplication.Option(
                "-p | --port",
                "The port to use to connect to the DNS server.",
                CommandOptionType.SingleValue);

            CommandOption noRecure = commandLineApplication.Option(
                "-nr | --norecurse",
                "Non recurive mode.",
                CommandOptionType.NoValue);

            CommandOption useTcp = commandLineApplication.Option(
                "--tcp",
                "Use TCP connection.",
                CommandOptionType.NoValue);

            CommandOption tries = commandLineApplication.Option(
                "--tries",
                "Number of tries.",
                CommandOptionType.SingleValue);

            CommandOption timeout = commandLineApplication.Option(
                "--time",
                "Query timeout.",
                CommandOptionType.SingleValue);

            CommandArgument domain = commandLineApplication.Argument("domain", "domain name", false);
            CommandArgument qType = commandLineApplication.Argument("q-type", "QType", false);
            CommandArgument qClass = commandLineApplication.Argument("q-class", "QClass", false);

            commandLineApplication.HelpOption("-? | -h | --help");
            commandLineApplication.OnExecute(() =>
            {
                var usePort = port.HasValue() ? int.Parse(port.Value()) : 53;
                var useServers = server.HasValue() ?
                    new[] { new IPEndPoint(IPAddress.Parse(server.Value()), usePort) } :
                    Client.GetDnsServers();

                string useDomain = string.IsNullOrWhiteSpace(domain.Value) ? "." : domain.Value;

                QType useQType = 0;
                QClass useQClass = 0;

                if (!string.IsNullOrWhiteSpace(qClass.Value))
                {
                    // q class != null => 3 params (domain already set above).
                    Enum.TryParse(qClass.Value, true, out useQClass);
                    Enum.TryParse(qType.Value, true, out useQType);
                }
                else
                {
                    // q class is null => 2 params only
                    // test if no domain is specified and first param is either qtype or qclass
                    if (Enum.TryParse(domain.Value, true, out useQType))
                    {
                        useDomain = ".";
                        Enum.TryParse(qType.Value, true, out useQClass);
                    }
                    else if (Enum.TryParse(domain.Value, true, out useQClass))
                    {
                        useDomain = ".";
                    }
                    else if (!Enum.TryParse(qType.Value, true, out useQType))
                    {
                        // could be q class as second and no QType
                        Enum.TryParse(qType.Value, true, out useQClass);
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

                var options = new DnsClientOptions(useServers)
                {
                    TransportType = useTcp.HasValue() ? TransportType.Tcp : TransportType.Udp,
                    Recursion = !noRecure.HasValue(),
                    Retries = tries.HasValue() ? int.Parse(tries.Value()) : 3,
                    Timeout = timeout.HasValue() ? int.Parse(timeout.Value()) : 1000
                };

                var dnsClient = new Client(loggerFactory, options);
                
                var swatch = Stopwatch.StartNew();

                var result = useQClass == 0 ?
                    dnsClient.QueryAsync(useDomain, useQType).Result :
                    dnsClient.QueryAsync(useDomain, useQType, useQClass).Result;
                
                var elapsed = swatch.ElapsedMilliseconds;

                // Printing infomrational stuff
                Console.WriteLine();
                Console.WriteLine($"; <<>> MiniDiG {Version} {OS} <<>> {string.Join(" ", args)}");
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
            });
            commandLineApplication.Execute(args);
        }
    }
}