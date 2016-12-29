using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DnsClient;
using DnsClient.Protocol.Record;
using Microsoft.Extensions.CommandLineUtils;

namespace DigApp
{
    public class LookupSettings
    {
        public IPEndPoint[] Endpoints { get; set; }

        public TimeSpan MinTTL { get; set; }

        public bool NoTcp { get; set; }

        public bool Recursion { get; set; }

        public int Retries { get; set; }

        public bool TcpOnly { get; set; }

        public TimeSpan Timeout { get; set; }

        public bool UseCache { get; set; }
    }

    public abstract class DnsCommand
    {
        public CommandOption ConnectTimeoutArg { get; set; }

        public CommandOption MinimumTTLArg { get; set; }

        public CommandOption NoRecurseArg { get; set; }

        public string[] OriginalArgs { get; }

        public CommandOption PortArg { get; set; }

        public CommandOption ServerArg { get; set; }

        public CommandOption TriesArg { get; set; }

        public CommandOption UseCacheArg { get; set; }

        public CommandOption UseTcpArg { get; set; }

        protected CommandLineApplication App { get; }

        public CommandOption NoTcpArg { get; set; }

        public DnsCommand(CommandLineApplication app, string[] originalArgs)
        {
            App = app;
            OriginalArgs = originalArgs;
            App.OnExecute(() => Execute());
            Configure();
        }

        public LookupClient GetDnsLookup(LookupSettings props = null)
        {
            var settings = props ?? GetLookupSettings();
            return new LookupClient(settings.Endpoints)
            {
                Recursion = settings.Recursion,
                Retries = settings.Retries,
                Timeout = settings.Timeout,
                MimimumCacheTimeout = settings.MinTTL,
                UseCache = settings.UseCache,
                UseTcpFallback = !settings.NoTcp,
                UseTcpOnly = settings.TcpOnly
            };
        }

        public IPEndPoint[] GetEndpointsValue()
        {
            if (ServerArg.HasValue())
            {
                var lookup = new LookupClient();
                var values = ServerArg.Values.Select(p => p.Split('#').ToArray());
                var result = new List<IPEndPoint>();
                foreach (var serverPair in values)
                {
                    var server = serverPair[0];
                    var port = serverPair.Length > 1 ? int.Parse(serverPair[1]) : 53;

                    IPAddress ip;
                    if (!IPAddress.TryParse(server, out ip))
                    {
                        try
                        {
                            var lResult = lookup.QueryAsync(server, QueryType.A).Result;
                            ip = lResult.Answers.OfType<ARecord>().FirstOrDefault()?.Address;
                        }
                        catch
                        {
                            throw new Exception("Cannot resolve server.");
                        }
                    }

                    if (ip == null)
                    {
                        throw new ArgumentException($"Invalid address or hostname '{server}'.");
                    }

                    result.Add(new IPEndPoint(ip, port));
                }

                return result.ToArray();
            }
            else
            {
                return NameServer.ResolveNameServers().ToArray();
            }
        }

        public LookupSettings GetLookupSettings()
        {
            return new LookupSettings()
            {
                Endpoints = GetEndpointsValue(),
                Recursion = GetUseRecursionValue(),
                Retries = GetTriesValue(),
                Timeout = TimeSpan.FromMilliseconds(GetTimeoutValue()),
                MinTTL = GetMinimumTTL(),
                UseCache = GetUseCache(),
                TcpOnly = GetUseTcp(),
                NoTcp = GetNoTcp()
            };
        }

        public TimeSpan GetMinimumTTL()
        {
            if (MinimumTTLArg.HasValue())
            {
                return TimeSpan.FromMilliseconds(int.Parse(MinimumTTLArg.Value()));
            }

            return TimeSpan.Zero;
        }

        public int GetPortValue() => PortArg.HasValue() ? int.Parse(PortArg.Value()) : 53;

        public int GetTimeoutValue() => ConnectTimeoutArg.HasValue() ? int.Parse(ConnectTimeoutArg.Value()) : 5000;

        public int GetTriesValue() => TriesArg.HasValue() ? int.Parse(TriesArg.Value()) : 10;

        public bool GetUseCache()
        {
            return UseCacheArg.HasValue();
        }

        public bool GetUseRecursionValue() => !NoRecurseArg.HasValue();

        public bool GetUseTcpValue() => UseTcpArg.HasValue();

        public bool GetUseTcp() => UseTcpArg.HasValue() ? true : false;

        public bool GetNoTcp() => NoTcpArg.HasValue() ? true : false;

        protected virtual void Configure()
        {
            ServerArg = App.Option(
                "-s | --server",
                "The DNS server.",
                CommandOptionType.MultipleValue);

            PortArg = App.Option(
                "-p | --port",
                $"The port to use to connect to the DNS server [{NameServer.DefaultPort}].",
                CommandOptionType.SingleValue);

            NoRecurseArg = App.Option(
                "-nr | --norecurse",
                "Non recurive mode.",
                CommandOptionType.NoValue);

            UseTcpArg = App.Option("--tcp", "Enable Tcp only mode.", CommandOptionType.NoValue);
            NoTcpArg = App.Option("--notcp", "Disable Tcp fallback.", CommandOptionType.NoValue);

            TriesArg = App.Option(
                "--tries",
                "Number of tries [3].",
                CommandOptionType.SingleValue);

            ConnectTimeoutArg = App.Option(
                "--time",
                "Query timeout [1000].",
                CommandOptionType.SingleValue);

            UseCacheArg = App.Option(
                "--cache",
                "Enable caching.",
                CommandOptionType.NoValue);

            MinimumTTLArg = App.Option(
                "--minttl",
                "Minimum cache ttl.",
                CommandOptionType.SingleValue);

            App.HelpOption("-? | -h | --help");
        }

        protected abstract Task<int> Execute();
    }
}