using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;

namespace NetCoreApp
{
    public static partial class DnsClientBenchmarks
    {
        public class CharValidate
        {
            public const string c_value = "a.very.long.hostname.with.all.kind.of.valid.values.in.it.so.the.check.loops.through.all.of.them";
            public static List<ArraySegment<byte>> source;
            private const char cDash = '-';
            private const char cDot = '.';
            private const char ca = 'a';
            private const char cz = 'z';
            private const char cA = 'A';
            private const char cZ = 'Z';
            private const char cZero = '0';
            private const char cNine = '9';

            private static bool IsHostNameChar(char c)
            {
                return c == '-' || c == '.' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
            }

            private static bool IsHostNameCharConst(char c)
            {
                return c == cDash || c == cDot || (c >= ca && c <= cz) || (c >= cA && c <= cZ) || (c >= cZero && c <= cNine);
            }
            
            public CharValidate()
            {
            }
            
            [Benchmark(Baseline = true)]
            public bool Char()
            {
                var result = true;
                foreach (var chr in c_value)
                {
                    if (!IsHostNameChar(chr)) throw new Exception("not expected");
                }

                return result;
            }
            
            [Benchmark]
            public bool CharAny()
            {
                var result = c_value.Any(p => !IsHostNameChar(p));

                return result;
            }
            
            [Benchmark]
            public bool CharConst()
            {
                var result = true;
                foreach (var chr in c_value)
                {
                    if (!IsHostNameChar(chr)) throw new Exception("not expected");
                }

                return result;
            }

            private static readonly Regex c_regex = new Regex("^[a-zA-Z0-9._-]+$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

            [Benchmark]
            public bool CompareRegEx()
            {
                var result = true;
                if (!c_regex.IsMatch(c_value))
                {
                    throw new Exception();
                }

                return result;
            }
        }
    }
}