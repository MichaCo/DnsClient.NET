////using System;
////using System.Diagnostics;
////using System.Linq;
////using System.Net;
////using System.Reflection;
////using System.Runtime.InteropServices;
////using System.Threading.Tasks;
////using DnsClient;
////using Microsoft.Extensions.CommandLineUtils;
////using Microsoft.Extensions.Logging;

////namespace DigApp
////{
////    internal class DigCommand2 : DnsCommand
////    {
////        public static string OS
////        {
////            get
////            {
////                return RuntimeInformation.OSDescription.Trim();
////            }
////        }

////        public static string Version
////        {
////            get
////            {
////                return typeof(DigCommand).GetTypeInfo().Assembly.GetName().Version.ToString();
////            }
////        }

////        public CommandArgument DomainArg { get; }

////        public CommandArgument QClassArg { get; }

////        public CommandArgument QTypeArg { get; }

////        public CommandOption ReversArg { get; }

////        public DigCommand2(CommandLineApplication app, string[] originalArgs) : base(app, originalArgs)
////        {
////            DomainArg = app.Argument("domain", "domain name", false);
////            QTypeArg = app.Argument("q-type", "QType", false);
////            QClassArg = app.Argument("q-class", "QClass", false);
////            ReversArg = app.Option("-x", "Reverse lookup shortcut", CommandOptionType.NoValue);
////        }

////        protected override async Task<int> Execute()
////        {
////            var loggerFactory = new LoggerFactory().AddConsole(GetLoglevelValue());

////            string useDomain = string.IsNullOrWhiteSpace(DomainArg.Value) ? "." : DomainArg.Value;
////            QType useQType = 0;
////            QClass useQClass = 0;

////            if (!string.IsNullOrWhiteSpace(QClassArg.Value))
////            {
////                // q class != null => 3 params (domain already set above).
////                Enum.TryParse(QClassArg.Value, true, out useQClass);
////                Enum.TryParse(QTypeArg.Value, true, out useQType);
////            }
////            else
////            {
////                // q class is null => 2 params only
////                // test if no domain is specified and first param is either qtype or qclass
////                if (Enum.TryParse(DomainArg.Value, true, out useQType))
////                {
////                    useDomain = ".";
////                    Enum.TryParse(QTypeArg.Value, true, out useQClass);
////                }
////                else if (Enum.TryParse(DomainArg.Value, true, out useQClass))
////                {
////                    useDomain = ".";
////                }
////                else if (!Enum.TryParse(QTypeArg.Value, true, out useQType))
////                {
////                    // could be q class as second and no QType
////                    Enum.TryParse(QTypeArg.Value, true, out useQClass);
////                }
////            }

////            if (ReversArg.HasValue())
////            {
////                useQType = QType.PTR;
////                useQClass = QClass.IN;
////                IPAddress ip;
////                if (!IPAddress.TryParse(useDomain, out ip))
////                {
////                    Console.WriteLine(";; WARNING: recursion requested but not available");
////                    return 1;
////                }

////                useDomain = DnsLookup.GetArpaFromIp(ip);
////            }

////            if (useQType == 0)
////            {
////                if (string.IsNullOrWhiteSpace(useDomain) || useDomain == ".")
////                {
////                    useQType = QType.NS;
////                }
////                else
////                {
////                    useQType = QType.A;
////                }
////            }

////            // finally running the command
////            DnsLookupOptions options = GetDnsLookupOptions();
////            var lookup = new DnsLookup(loggerFactory, options);

////            var swatch = Stopwatch.StartNew();

////            var result = useQClass == 0 ?
////                await lookup.QueryAsync(useDomain, useQType) :
////                await lookup.QueryAsync(useDomain, useQType, useQClass);

////            var elapsed = swatch.ElapsedMilliseconds;

////            // Printing infomrational stuff
////            var useServers = GetEndpointsValue();

////            Console.WriteLine();
////            Console.WriteLine($"; <<>> MiniDiG {Version} {OS} <<>> {string.Join(" ", OriginalArgs)}");
////            Console.WriteLine($"; ({useServers.Length} server found)");

////            if (!string.IsNullOrWhiteSpace(result.Error))
////            {
////                Console.WriteLine($";; {result.Error}");
////            }
////            else
////            {
////                Console.WriteLine(";; Got answer:");
////                Console.WriteLine($";; ->>HEADER<<- opcode: {result.Header.OPCode}, id: {result.Header.Id}");
////                var flags = new string[] {
////                        result.Header.HasQuery ? "qr" : "",
////                        result.Header.RecursionAvailable ? "ra" : "",
////                        result.Header.RecursionEnabled ? "rd" : "",
////                        result.Header.TruncationEnabled ? "tc" : ""
////                    };
////                var flagsString = string.Join(" ", flags.Where(p => p != ""));

////                Console.WriteLine($";; flags: {flagsString}; QUERY: {result.Header.QuestionCount}, " +
////                    $"ANSWER: {result.Header.AnswerCount}, AUTORITY: {result.Header.NameServerCount}, ADDITIONAL: {result.Header.AdditionalCount}");

////                Console.WriteLine();

////                if (result.Questions.Count > 0)
////                {
////                    Console.WriteLine(";; QUESTION SECTION:");
////                    foreach (var question in result.Questions)
////                    {
////                        Console.WriteLine(question);
////                    }

////                    Console.WriteLine();
////                }

////                if (result.Answers.Count > 0)
////                {
////                    Console.WriteLine(";; ANSWER SECTION:");
////                    foreach (var answer in result.Answers)
////                    {
////                        Console.WriteLine(answer);
////                    }

////                    Console.WriteLine();
////                }

////                if (result.Additionals.Count > 0)
////                {
////                    Console.WriteLine(";; ADDITIONALS SECTION:");
////                    foreach (var additional in result.Additionals)
////                    {
////                        Console.WriteLine(additional);
////                    }

////                    Console.WriteLine();
////                }

////                if (result.Authorities.Count > 0)
////                {
////                    Console.WriteLine(";; AUTHORITIES SECTION:");
////                    foreach (var auth in result.Authorities)
////                    {
////                        Console.WriteLine(auth);
////                    }

////                    Console.WriteLine();
////                }
////            }

////            // footer
////            Console.WriteLine($";; Query time: {elapsed:N0} msec");
////            Console.WriteLine($";; SERVER: {useServers.FirstOrDefault()}");
////            Console.WriteLine($";; WHEN: {DateTime.Now.ToString("R")}");

////            return 0;
////        }
////    }
////}