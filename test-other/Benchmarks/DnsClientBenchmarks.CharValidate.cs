// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Linq;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public static partial class DnsClientBenchmarks
    {
        public class CharValidate
        {
            private static readonly Regex s_regex = new Regex("^[a-zA-Z0-9._-]+$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
            public const string LongValue = "a.very.long.hostname.with.all.kind.of.valid.values.in.it.so.the.check.loops.through.all.of.thema.very.long.hostname.with.all.kind.of.valid.values.in.it.so.the.check.loops.through.all.of.thema.very.long.hostname.with.all.kind.of.valid.values.in.it.so.the.check.loops.through.all.of.them";
            public const string ShortValue = "a.very.short.hostname";

            private static bool IsHostNameChar(char c)
            {
                return c == '-' || c == '.' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
            }

            [Benchmark]
            public bool ForeachLongName()
            {
                var result = true;
                foreach (var chr in LongValue)
                {
                    if (!IsHostNameChar(chr))
                    {
                        throw new Exception("not expected");
                    }
                }

                return result;
            }

            [Benchmark(Baseline = true)]
            public bool Foreach()
            {
                var result = true;
                foreach (var chr in ShortValue)
                {
                    if (!IsHostNameChar(chr))
                    {
                        throw new Exception("not expected");
                    }
                }

                return result;
            }

            [Benchmark]
            public bool LinqAnyLongName()
            {
                var result = LongValue.Any(p => !IsHostNameChar(p));

                return result;
            }

            [Benchmark]
            public bool LinqAny()
            {
                var result = ShortValue.Any(p => !IsHostNameChar(p));

                return result;
            }

            [Benchmark]
            public bool RegExLongName()
            {
                var result = true;
                if (!s_regex.IsMatch(LongValue))
                {
                    throw new Exception();
                }

                return result;
            }

            [Benchmark]
            public bool RegEx()
            {
                var result = true;
                if (!s_regex.IsMatch(ShortValue))
                {
                    throw new Exception();
                }

                return result;
            }
        }
    }
}
