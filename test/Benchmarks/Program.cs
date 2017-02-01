using System;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            do
            {
                BenchmarkSwitcher
                    .FromAssembly(typeof(DnsClientBenchmarks).GetTypeInfo().Assembly)
                    .Run(args);

                Console.WriteLine("done!");
                Console.WriteLine("Press escape to exit or any key to continue...");
            } while (Console.ReadKey().Key != ConsoleKey.Escape);
        }
    }

    public class MediumConfiguration : ManualConfig
    {
        public MediumConfiguration()
        {
            Add(Job.MediumRun
                .With(Platform.X64));
        }
    }

    public class ShortConfiguration : BenchmarkDotNet.Configs.ManualConfig
    {
        public ShortConfiguration()
        {
            // The same, using the .With() factory methods:
            Add(
                Job.Dry
                .With(Platform.X64)
                .With(Jit.RyuJit)
                .With(Runtime.Clr)
                .With(Runtime.Core)
                .WithLaunchCount(5)
                .WithId("ShortRun"));

            Add(DefaultConfig.Instance.GetLoggers().ToArray());
            Add(DefaultConfig.Instance.GetAnalysers().ToArray());
            Add(DefaultConfig.Instance.GetColumnProviders().ToArray());
            Add(DefaultConfig.Instance.GetDiagnosers().ToArray());
            Add(DefaultConfig.Instance.GetExporters().ToArray());
            Add(DefaultConfig.Instance.GetValidators().ToArray());
        }
    }
}