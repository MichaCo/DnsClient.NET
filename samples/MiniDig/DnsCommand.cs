using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DnsClient;
using DnsClient.Protocol.Record;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace DigApp
{
    public class LookupSettings
    {
        public IPEndPoint[] Endpoints { get; set; }

        public TimeSpan MinTTL { get; set; }

        public bool Recursion { get; set; }

        public int Retries { get; set; }

        public TimeSpan Timeout { get; set; }

        public bool UseCache { get; set; }
    }

    internal abstract class DnsCommand
    {
        public CommandOption ConnectTimeoutArg { get; set; }

        public CommandOption LogLevelArg { get; set; }

        public CommandOption MinimumTTLArg { get; set; }

        public CommandOption NoRecurseArg { get; set; }

        public string[] OriginalArgs { get; }

        public CommandOption PortArg { get; set; }

        public CommandOption ServerArg { get; set; }

        public CommandOption ServersArg { get; set; }

        public CommandOption TriesArg { get; set; }

        public CommandOption UseCacheArg { get; set; }

        public CommandOption UseTcpArg { get; set; }

        protected CommandLineApplication App { get; }

        public DnsCommand(CommandLineApplication app, string[] originalArgs)
        {
            App = app;
            OriginalArgs = originalArgs;
            App.OnExecute(() => Execute());
            Configure();
        }

        public LookupClient GetDnsLookup(LookupSettings props = null)
        {
            if (UseTcp())
            {
                throw new NotImplementedException();
            }
            else
            {
                var settings = props ?? GetLookupSettings();
                return new LookupClient(settings.Endpoints)
                {
                    Recursion = settings.Recursion,
                    Retries = settings.Retries,
                    Timeout = settings.Timeout,
                    MimimumCacheTimeout = settings.MinTTL,
                    UseCache = settings.UseCache
                };
            }
        }

        public IPEndPoint[] GetEndpointsValue()
        {
            if (ServerArg.HasValue())
            {
                string server = ServerArg.Value();
                IPAddress ip;
                if (!IPAddress.TryParse(server, out ip))
                {
                    try
                    {
                        var lookup = new LookupClient();
                        var result = lookup.QueryAsync(server, QueryType.A).Result;
                        ip = result.Answers.OfType<ARecord>().FirstOrDefault()?.Address;
                    }
                    catch
                    {
                        throw new Exception("Cannot resolve server.");
                    }
                }

                return new[] { new IPEndPoint(ip, GetPortValue()) };
            }
            else if (ServersArg.HasValue())
            {
                var lookup = new LookupClient();
                var values = ServersArg.Values.Select(p => p.Split('#').ToArray());
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

                    result.Add(new IPEndPoint(ip, port));
                }

                return result.ToArray();
            }
            else
            {
                return NameServer.ResolveNameServers().ToArray();
            }
        }

        public LogLevel GetLoglevelValue()
        {
            LogLevel logginglevel;
            return LogLevelArg.HasValue() && Enum.TryParse(LogLevelArg.Value(), true, out logginglevel) ? logginglevel : LogLevel.Warning;
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
                UseCache = GetUseCache()
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

        public bool UseTcp() => UseTcpArg.HasValue() ? true : false;

        protected virtual void Configure()
        {
            ServerArg = App.Option(
                "-s | --server",
                "The DNS server.",
                CommandOptionType.SingleValue);

            ServersArg = App.Option(
                "-servers | --servers",
                "The DNS servers to use <name1>#<port1> <nameN>#<portN>.",
                CommandOptionType.MultipleValue);

            PortArg = App.Option(
                "-p | --port",
                $"The port to use to connect to the DNS server [{NameServer.DefaultPort}].",
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

            UseCacheArg = App.Option(
                "--cache",
                "Enable caching.",
                CommandOptionType.NoValue);

            MinimumTTLArg = App.Option(
                "--minttl",
                "Minimum cache ttl.",
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