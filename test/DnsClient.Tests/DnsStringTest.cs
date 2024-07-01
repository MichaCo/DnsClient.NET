// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using Xunit;

namespace DnsClient.Tests
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class DnsStringTest
    {
        [Fact]
        public void DnsString_SimpleValid()
        {
            DnsString query = DnsString.Parse("www.google.com");

            Assert.Equal("www.google.com.", query.Value);
            Assert.Equal("www.google.com", query.Original);
            Assert.Equal(3, query.NumberOfLabels);
        }

        [Fact]
        public void DnsString_SimpleValidWithRoot()
        {
            var name = DnsString.Parse("abc.xyz.example.com.");

            Assert.Equal("abc.xyz.example.com.", name.Value);
            Assert.Equal(4, name.NumberOfLabels);
        }

        [Fact]
        public void DnsString_SimpleValidShort()
        {
            var name = DnsString.Parse("host-name");

            Assert.Equal("host-name.", name.Value);
            Assert.Equal(1, name.NumberOfLabels);
        }

        [Fact]
        public void QueryName_AcceptsService()
        {
            var name = DnsString.Parse("_abc._xyz._example.com");
            var strValue = name.ToString();

            // ending
            Assert.Equal("_abc._xyz._example.com.", strValue);
        }

        [Fact]
        public void DnsString_AcceptsPuny()
        {
            var val = "xn--4gbrim.xn----ymcbaaajlc6dj7bxne2c.xn--wgbh1c";
            DnsString name = DnsString.Parse(val);

            Assert.Equal(name.ToString(), val + ".");
            Assert.Equal(3, name.NumberOfLabels);
        }

#if !NET6_0_OR_GREATER // Actually changed behavior in NET50
        [Fact]
        public void DnsString_ReadPuny_IDNA2003_Invalid()
        {
            // IDNA new would be: "xn--fuball-cta.example"
            var expected = "fussball.example.";
            var val = "fußball.example";
            DnsString result = DnsString.Parse(val);
            Assert.Equal(expected, result.Value);
        }
#endif

        [Fact]
        public void QDnsString_ParsePuny()
        {
            var domain = "müsli.de";
            DnsString name = DnsString.Parse(domain);
            Assert.Equal("xn--msli-0ra.de.", name.Value);
            Assert.Equal(2, name.NumberOfLabels);
        }

        [Fact]
        public void DnsString_ParseNullQueryString()
        {
            static void act() => DnsString.Parse(null);

            Assert.ThrowsAny<ArgumentNullException>(act);
        }

        [Fact]
        public void DnsString_EmptyToRoot()
        {
            DnsString query = DnsString.Parse("");

            Assert.Equal(".", query.Value);
        }

        [Fact]
        public void DnsString_LongLabel()
        {
            var ex = Record.Exception(() => DnsString.Parse("www.goo0000000000000000000000000000000000000000000000000000000000001.com"));

            Assert.NotNull(ex);
            Assert.Contains("is longer than " + DnsString.LabelMaxLength, ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void DnsString_LongQuery()
        {
            var ex = Record.Exception(() => DnsString.Parse("www.goo000000000000000000000000000000000000000000000000000000000000.goo000000000000000000000000000000000000000000000000000000000000.goo000000000000000000000000000000000000000000000000000000000000.goo000000000000000000000000000000000000000000000000000.com"));

            Assert.NotNull(ex);
            Assert.Contains("maximum of " + DnsString.QueryMaxLength, ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        //// relaxed puny code rules
        ////[Fact]
        ////public void DnsString_IllegalEscape()
        ////{
        ////    Action act = () => DnsString.Parse("abc.zy\\.z.com");

        ////    var ex = Record.Exception(act);
        ////    Assert.Contains("not a valid hostname", ex.Message);
        ////}

        [Fact]
        public void DnsString_StartsWithDot()
        {
            var ex = Record.Exception(() => DnsString.Parse(".www.google.com"));

            Assert.NotNull(ex);
            Assert.Contains("found leading root", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void DnsString_Hashcode()
        {
            var val = "Aäb.bÜlöb.cscöälüpqläö.d.com.";
            DnsString name = DnsString.Parse(val);
            DnsString name2 = DnsString.Parse(val);

            Assert.True(name.GetHashCode().Equals(name2.GetHashCode()));
        }

        [Fact]
        public void DnsString_HashcodeNot()
        {
            var val = "Aäb.bÜlöb.cscöälüpqläö.d.com.";
            DnsString name = DnsString.Parse(val);
            DnsString name2 = DnsString.Parse("asdasd");
            Assert.False(name.GetHashCode().Equals(name2.GetHashCode()));
        }

        [Fact]
        public void DnsString_Equals()
        {
            var val = "Aäb.bÜlöb.cscöälüpqläö.d.com.";
            DnsString name = DnsString.Parse(val);
            DnsString name2 = DnsString.Parse(val);

            Assert.True(name.Equals(name2));
        }

        [Fact]
        public void DnsString_EqualsString()
        {
            var val = "abc.";
            DnsString name = DnsString.Parse(val);
            string name2 = val;

            Assert.True(name.Equals(name2));
        }

        [Fact]
        public void DnsString_NotEquals()
        {
            DnsString name = DnsString.Parse("abc");
            DnsString name2 = DnsString.Parse("abc2");

            Assert.False(name.Equals(name2));
        }

        [Fact]
        public void DnsString_NotEqualsNull()
        {
            DnsString name = DnsString.Parse("abc");
            DnsString name2 = null;

            Assert.False(name.Equals(name2));
        }

        [Fact]
        public void DnsString_NotEqualsNotName()
        {
            DnsString name = DnsString.Parse("abc");
            object name2 = new object();

            Assert.False(name.Equals(name2));
        }

        [Fact]
        public void DnsString_ConcatA()
        {
            DnsString a = DnsString.Parse("hostname");
            DnsString b = DnsString.Parse("some.domain");

            var result = a + b;
            Assert.Equal("hostname.some.domain.", result.Value);
        }

        [Fact]
        public void DnsString_ConcatB()
        {
            DnsString a = DnsString.Parse("hostname.");
            DnsString b = DnsString.Parse("some.domain.");

            var result = a + b;
            Assert.Equal("hostname.some.domain.", result.Value);
        }

        [Fact]
        public void DnsString_ConcatC()
        {
            var result = DnsString.RootLabel + DnsString.RootLabel;
            Assert.Equal(".", result.Value);
        }

        [Fact]
        public void DnsString_ConcatNullA()
        {
            Assert.Throws<ArgumentNullException>(() => DnsString.RootLabel + null);
        }

        [Fact]
        public void DnsString_ConcatNullB()
        {
            Assert.Throws<ArgumentNullException>(() => null + DnsString.RootLabel);
        }

        [Fact]
        public void DnsString_ConcatStringA()
        {
            DnsString a = DnsString.Parse("hostname");
            var b = "some.domain";

            var result = a + b;
            Assert.Equal("hostname.some.domain.", result.Value);
        }

        [Fact]
        public void DnsString_ConcatStringB()
        {
            DnsString a = DnsString.Parse("hostname.");
            var b = "some.domain.";

            var result = a + b;
            Assert.Equal("hostname.some.domain.", result.Value);
        }

        [Fact]
        public void DnsString_ConcatStringC()
        {
            DnsString a = DnsString.Parse(".");
            var b = ".";

            var result = a + b;
            Assert.Equal(".", result.Value);
        }

        [Fact]
        public void DnsString_ConcatStringNull()
        {
            Assert.Throws<ArgumentException>(() => DnsString.RootLabel + (string)null);
        }

        [Fact]
        public void DnsString_ConcatStringNullB()
        {
            Assert.Throws<ArgumentException>(() => DnsString.RootLabel + "  ");
        }

        [Fact]
        public void DnsString_ConcatStringNullC()
        {
            Assert.Throws<ArgumentNullException>(() => (DnsString)null + "");
        }
    }
}
