using System;
using System.Net;
using System.Threading.Tasks;
using DnsClient;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace DigApp
{
    internal abstract class DnsCommand
    {
        public CommandOption ConnectTimeoutArg { get; private set; }

        public CommandOption LogLevelArg { get; private set; }

        public CommandOption NoRecurseArg { get; private set; }

        public string[] OriginalArgs { get; }

        public CommandOption PortArg { get; private set; }

        public CommandOption ServerArg { get; private set; }

        public CommandOption TriesArg { get; private set; }

        public CommandOption UseTcpArg { get; private set; }

        protected CommandLineApplication App { get; }

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

        public int GetPortValue() => PortArg.HasValue() ? int.Parse(PortArg.Value()) : 53;

        public int GetTimeoutValue() => ConnectTimeoutArg.HasValue() ? int.Parse(ConnectTimeoutArg.Value()) : 1000;

        public TransportType GetTransportTypeValue() => UseTcpArg.HasValue() ? TransportType.Tcp : TransportType.Udp;

        public int GetTriesValue() => TriesArg.HasValue() ? int.Parse(TriesArg.Value()) : 3;

        public bool GetUseRecursionValue() => !NoRecurseArg.HasValue();

        public bool GetUseTcpValue() => UseTcpArg.HasValue();

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

        protected abstract Task<int> Execute();
    }
}