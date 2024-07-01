// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DnsClient;
using DnsClient.Protocol;
using McMaster.Extensions.CommandLineUtils;

namespace DigApp
{
    public abstract class DnsCommand
    {
        public CommandOption ConnectTimeoutArg { get; set; }

        public CommandOption MinimumTTLArg { get; set; }

        public CommandOption MaximumTTLArg { get; set; }

        public CommandOption MaximumBufferSizeArg { get; set; }

        public CommandOption RequestDnsSecRecordsArg { get; set; }

        public CommandOption NoRecurseArg { get; set; }

        public string[] OriginalArgs { get; }

        public CommandOption ServerArg { get; set; }

        public CommandOption TriesArg { get; set; }

        public CommandOption UseCacheArg { get; set; }

        public CommandOption UseTcpArg { get; set; }

        protected CommandLineApplication App { get; }

        public CommandOption NoTcpArg { get; set; }

        protected DnsCommand(CommandLineApplication app, string[] originalArgs)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            OriginalArgs = originalArgs;
            App.OnExecuteAsync((t) => Execute());
            Configure();
        }

        public LookupClient GetDnsLookup(LookupClientOptions props = null)
        {
            var settings = props ?? GetLookupSettings();
            return new LookupClient(settings);
        }

        public NameServer[] GetEndpointsValue()
        {
            if (ServerArg.HasValue())
            {
                var values = ServerArg.Values.Select(p => p.Split('#').ToArray());
                var result = new List<NameServer>();
                foreach (var serverPair in values)
                {
                    var server = serverPair[0];
                    var port = serverPair.Length > 1 ? int.Parse(serverPair[1], CultureInfo.InvariantCulture) : 53;

                    if (!IPAddress.TryParse(server, out IPAddress ip))
                    {
                        var lookup = new LookupClient();
                        var lResult = lookup.QueryAsync(server, QueryType.A).Result;
                        ip = lResult.Answers.OfType<ARecord>().FirstOrDefault()?.Address;
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

        public LookupClientOptions GetLookupSettings()
        {
            return new LookupClientOptions(GetEndpointsValue())
            {
                Recursion = GetUseRecursionValue(),
                Retries = GetTriesValue(),
                Timeout = TimeSpan.FromMilliseconds(GetTimeoutValue()),
                MinimumCacheTimeout = GetMinimumTTL(),
                UseCache = GetUseCache(),
                UseTcpOnly = GetUseTcp(),
                UseTcpFallback = !GetNoTcp(),
                MaximumCacheTimeout = GetMaximumTTL(),
                ExtendedDnsBufferSize = GetMaximumBufferSize(),
                RequestDnsSecRecords = GetRequestDnsSec()
            };
        }

        public TimeSpan? GetMinimumTTL()
        {
            if (MinimumTTLArg.HasValue())
            {
                return TimeSpan.FromMilliseconds(int.Parse(MinimumTTLArg.Value(), CultureInfo.InvariantCulture));
            }

            return null;
        }

        public TimeSpan? GetMaximumTTL()
        {
            if (MaximumTTLArg.HasValue())
            {
                return TimeSpan.FromMilliseconds(int.Parse(MaximumTTLArg.Value(), CultureInfo.InvariantCulture));
            }

            return null;
        }

        public int GetMaximumBufferSize()
            => MaximumBufferSizeArg.HasValue() ? int.Parse(MaximumBufferSizeArg.Value(), CultureInfo.InvariantCulture) : DnsQueryOptions.MaximumBufferSize;

        public bool GetRequestDnsSec() => RequestDnsSecRecordsArg.HasValue();

        public int GetTimeoutValue() => ConnectTimeoutArg.HasValue() ? int.Parse(ConnectTimeoutArg.Value(), CultureInfo.InvariantCulture) : 1000;

        public int GetTriesValue() => TriesArg.HasValue() ? int.Parse(TriesArg.Value(), CultureInfo.InvariantCulture) : 5;

        public bool GetUseCache()
        {
            return UseCacheArg.HasValue();
        }

        public bool GetUseRecursionValue() => !NoRecurseArg.HasValue();

        public bool GetUseTcpValue() => UseTcpArg.HasValue();

        public bool GetUseTcp() => UseTcpArg.HasValue();

        public bool GetNoTcp() => NoTcpArg.HasValue();

        protected virtual void Configure()
        {
            App.HelpOption("-? | -h | --help | --helpme");
            ServerArg = App.Option(
                "-s | --server",
                "The DNS server <name|ip>#<port> (multiple)",
                CommandOptionType.MultipleValue);

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
                "Query timeout [5000].",
                CommandOptionType.SingleValue);

            UseCacheArg = App.Option(
                "--cache",
                "Enable caching.",
                CommandOptionType.NoValue);

            MinimumTTLArg = App.Option(
                "--minttl",
                "Minimum cache ttl.",
                CommandOptionType.SingleValue);

            MaximumTTLArg = App.Option(
                "--maxttl",
                "Maximum cache ttl.",
                CommandOptionType.SingleValue);

            MaximumBufferSizeArg = App.Option(
                "--bufsize",
                "Maximum EDNS buffer size.",
                CommandOptionType.SingleValue);

            RequestDnsSecRecordsArg = App.Option(
                "--dnssec",
                "Request DNS SEC records (do flag).",
                CommandOptionType.NoValue);
        }

        protected abstract Task<int> Execute();
    }
}
