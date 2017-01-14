using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public static partial class DnsClientBenchmarks
    {
        public class CharValidate
        {
            private static readonly Regex c_regex = new Regex("^[a-zA-Z0-9._-]+$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
            public const string c_longValue = "a.very.long.hostname.with.all.kind.of.valid.values.in.it.so.the.check.loops.through.all.of.thema.very.long.hostname.with.all.kind.of.valid.values.in.it.so.the.check.loops.through.all.of.thema.very.long.hostname.with.all.kind.of.valid.values.in.it.so.the.check.loops.through.all.of.them";
            public const string c_shortValue = "a.very.short.hostname";
            
            private static bool IsHostNameChar(char c)
            {
                return c == '-' || c == '.' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
            }
            
            [Benchmark]
            public bool ForeachLongName()
            {
                var result = true;
                foreach (var chr in c_longValue)
                {
                    if (!IsHostNameChar(chr)) throw new Exception("not expected");
                }

                return result;
            }

            [Benchmark(Baseline = true)]
            public bool Foreach()
            {
                var result = true;
                foreach (var chr in c_shortValue)
                {
                    if (!IsHostNameChar(chr)) throw new Exception("not expected");
                }

                return result;
            }

            [Benchmark]
            public bool LinqAnyLongName()
            {
                var result = c_longValue.Any(p => !IsHostNameChar(p));

                return result;
            }

            [Benchmark]
            public bool LinqAny()
            {
                var result = c_shortValue.Any(p => !IsHostNameChar(p));

                return result;
            }

            [Benchmark]
            public bool RegExLongName()
            {
                var result = true;
                if (!c_regex.IsMatch(c_longValue))
                {
                    throw new Exception();
                }

                return result;
            }

            [Benchmark]
            public bool RegEx()
            {
                var result = true;
                if (!c_regex.IsMatch(c_shortValue))
                {
                    throw new Exception();
                }

                return result;
            }
        }
    }
}