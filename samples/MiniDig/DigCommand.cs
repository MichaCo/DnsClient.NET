// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DnsClient;
using McMaster.Extensions.CommandLineUtils;

namespace DigApp
{
    public class DigCommand : DnsCommand
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
                return typeof(DigCommand).GetTypeInfo().Assembly.GetName().Version.ToString();
            }
        }

        public CommandArgument DomainArg { get; }

        public CommandArgument QClassArg { get; }

        public CommandArgument QTypeArg { get; }

        public CommandOption ReversArg { get; }

        public DigCommand(CommandLineApplication app, string[] originalArgs)
            : base(app, originalArgs)
        {
            DomainArg = app.Argument("domain", "domain name", false);
            QTypeArg = app.Argument("q-type", "QType", false);
            QClassArg = app.Argument("q-class", "QClass", false);
            ReversArg = app.Option("-x", "Reverse lookup shortcut", CommandOptionType.NoValue);
        }

        protected override async Task<int> Execute()
        {
            Console.WriteLine();
            Console.WriteLine($"; <<>> MiniDiG {Version} {OS} <<>> {string.Join(" ", OriginalArgs)}");

            string useDomain = string.IsNullOrWhiteSpace(DomainArg.Value) ? "." : DomainArg.Value;
            QueryType useQType = 0;
            QueryClass useQClass = 0;

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

            if (ReversArg.HasValue())
            {
                useQType = QueryType.PTR;
                useQClass = QueryClass.IN;
                if (!IPAddress.TryParse(useDomain, out IPAddress ip))
                {
                    Console.WriteLine($";; Error: Reverse lookup for invalid ip {useDomain}.");
                    return 1;
                }

                useDomain = ip.GetArpaName();
            }

            if (useQType == 0)
            {
                if (string.IsNullOrWhiteSpace(useDomain) || useDomain == ".")
                {
                    useQType = QueryType.NS;
                }
                else
                {
                    useQType = QueryType.A;
                }
            }

            try
            {
                // finally running the command
                var options = GetLookupSettings();
                options.EnableAuditTrail = true;
                var lookup = GetDnsLookup(options);

                Console.WriteLine($"; Servers: {string.Join(", ", lookup.NameServers)}");

                var parsedDnsString = DnsString.Parse(useDomain);
                if (parsedDnsString.NumberOfLabels == 1 && !parsedDnsString.Original.EndsWith(".", StringComparison.Ordinal))
                {
                    foreach (var server in lookup.NameServers)
                    {
                        if (server.DnsSuffix != null)
                        {
                            var newQuery = parsedDnsString + server.DnsSuffix;

                            var serverResult = useQClass == 0 ?
                                await lookup.QueryServerAsync(new[] { server }, newQuery, useQType).ConfigureAwait(false) :
                                await lookup.QueryServerAsync(new[] { server }, newQuery, useQType, useQClass).ConfigureAwait(false);

                            if (!serverResult.HasError)
                            {
                                Console.WriteLine(serverResult.AuditTrail);
                                return 0;
                            }
                        }
                    }
                }

                var result = useQClass == 0 ?
                    await lookup.QueryAsync(useDomain, useQType).ConfigureAwait(false) :
                    await lookup.QueryAsync(useDomain, useQType, useQClass).ConfigureAwait(false);

                Console.WriteLine(result.AuditTrail);
            }
            catch (Exception ex)
            {
                var agg = ex as AggregateException;
                var dns = ex as DnsResponseException;
                agg?.Handle(e =>
                {
                    dns = e as DnsResponseException;
                    if (dns != null)
                    {
                        return true;
                    }

                    return false;
                });

                if (dns != null)
                {
                    Console.WriteLine(dns.AuditTrail);
                    Console.WriteLine($";; Error: {ex.Message}");
                    return 24;
                }
                else
                {
                    Console.WriteLine($";; Error: {ex.Message}");
                    return 40;
                }
            }

            return 0;
        }
    }
}
