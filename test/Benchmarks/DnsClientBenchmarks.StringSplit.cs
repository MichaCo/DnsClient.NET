using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public static partial class DnsClientBenchmarks
    {
        public class StringSplit
        {
            private const string Source = ".www.lala.lolol.blub.de.com.org.eu.gov.";

            public StringSplit()
            {
            }

            [Benchmark(Baseline = true)]
            public object Split()
            {
                var x = SplitString(Source).ToArray();
                return x;
            }

            [Benchmark]
            public object ManualSplit()
            {
                var x = ManualSplitBytes(Source).ToArray();
                return x;
            }

            public static IEnumerable<byte[]> SplitString(string input)
            {
                foreach (var label in input.Split(new char[] { '.' }, StringSplitOptions.None))
                {
                    yield return Encoding.UTF8.GetBytes(label);
                }
            }

            private const byte Dot = (byte)'.';

            public static IEnumerable<ArraySegment<byte>> ManualSplitBytes(string input)
            {
                var bytes = Encoding.UTF8.GetBytes(input);

                int lastStop = 0;
                for (int index = 0; index < input.Length; index++)
                {
                    if (bytes[index] == Dot)
                    {
                        yield return new ArraySegment<byte>(bytes, lastStop, index - lastStop);
                        lastStop = index + 1;
                    }
                }

                if (lastStop < bytes.Length)
                {
                    yield return new ArraySegment<byte>(bytes, lastStop, bytes.Length - lastStop);
                }
            }
        }
    }
}