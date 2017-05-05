using System;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using DnsClient;

namespace Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var port = 5053;
            var server = new StaticDnsServer(
                printStats: false,
                port: port,
                workers: 1);

            server.Start();

            new DnsClientBenchmarks.StaticServerQuery().RequestSync();

            do
            {
                var config = ManualConfig.Create(DefaultConfig.Instance)
                    //.With(exporters: BenchmarkDotNet.Exporters.DefaultExporters.)
                    .With(BenchmarkDotNet.Analysers.EnvironmentAnalyser.Default)
                    .With(BenchmarkDotNet.Exporters.MarkdownExporter.GitHub)
                    .With(BenchmarkDotNet.Exporters.MarkdownExporter.StackOverflow)
                    .With(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default)
                    .With(Job.Core
                        .WithTargetCount(10)
                        .WithWarmupCount(5)
                        .WithLaunchCount(1))
                    .With(Job.Clr
                        .WithTargetCount(10)
                        .WithWarmupCount(5)
                        .WithLaunchCount(1));

                BenchmarkSwitcher
                    .FromAssembly(typeof(DnsClientBenchmarks).GetTypeInfo().Assembly)
                    .Run(args, config);

                Console.WriteLine("done!");
                Console.WriteLine("Press escape to exit or any key to continue...");
            } while (Console.ReadKey().Key != ConsoleKey.Escape);

            server.Stop();
        }
    }

    //[MarkdownExporter, AsciiDocExporter, HtmlExporter, CsvExporter, RPlotExporter]
    //[MinColumn, MaxColumn]
    //[ClrJob, CoreJob, MediumRunJob]
    public class CustomConfiguration : ManualConfig
    {
        public CustomConfiguration()
        {
            Add(new Job(EnvMode.Clr)
            {
                Env = { Runtime = Runtime.Clr },
                Run = { LaunchCount = 3, WarmupCount = 5, TargetCount = 10 },
            });

            Add(new Job(EnvMode.Core)
            {
                Env = { Runtime = Runtime.Core },
                Run = { LaunchCount = 3, WarmupCount = 5, TargetCount = 10 },
            });
        }
    }
}