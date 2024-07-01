// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using DnsClient;

namespace Benchmarks
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        public static void Main(string[] args)
        {
            var port = 5053;
            using var server = new StaticDnsServer(
                printStats: false,
                port: port,
                workers: 1);

            server.Start();

            do
            {
                //var config = ManualConfig.Create(DefaultConfig.Instance)
                //.With(exporters: BenchmarkDotNet.Exporters.DefaultExporters.)
                //.With(BenchmarkDotNet.Analysers.EnvironmentAnalyser.Default)
                //.With(BenchmarkDotNet.Exporters.MarkdownExporter.GitHub)
                //.With(BenchmarkDotNet.Exporters.MarkdownExporter.StackOverflow)
                //.With(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default)
                //.With(Job.Core
                //    .WithIterationCount(10)
                //    .WithWarmupCount(5)
                //    .WithLaunchCount(1))
                //.With(Job.Clr
                //    .WithIterationCount(10)
                //    .WithWarmupCount(5)
                //    .WithLaunchCount(1));

                BenchmarkSwitcher
                    .FromAssembly(typeof(DnsClientBenchmarks).Assembly)
                    .Run(args, new Config());

                Console.WriteLine("done!");
                Console.WriteLine("Press escape to exit or any key to continue...");
            } while (Console.ReadKey().Key != ConsoleKey.Escape);

            server.Stop();
        }

        private class Config : ManualConfig
        {
            public Config()
            {
                Add(DefaultConfig.Instance);

                var coreJob = Job
                    .MediumRun
                    .WithLaunchCount(1)
                    .WithWarmupCount(3)
                    .WithIterationCount(10)
                    .WithEvaluateOverhead(false);

                AddJob(coreJob);

                //var clrJob = Job.MediumRun
                //    .With(Runtime.Clr)
                //    .With(Jit.Default)
                //    .WithLaunchCount(1)
                //    .WithWarmupCount(3)
                //    .WithIterationCount(10)
                //    .WithEvaluateOverhead(false);

                //Add(clrJob);

                AddColumn(StatisticColumn.OperationsPerSecond);

                AddAnalyser(EnvironmentAnalyser.Default);
                AddDiagnoser(MemoryDiagnoser.Default);

                AddExporter(MarkdownExporter.GitHub);
                AddExporter(MarkdownExporter.StackOverflow);
            }
        }
    }
}
