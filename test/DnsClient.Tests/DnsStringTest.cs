using System;
using System.Linq;
using Xunit;

namespace DnsClient.Tests
{
    public class DnsStringTest
    {
        [Fact]
        public void DnsString_SimpleValid()
        {
            DnsString query = DnsString.ParseQueryString("www.google.com");

            Assert.Equal("www.google.com.", query.Value);
            Assert.Equal("www.google.com", query.Original);
        }

        [Fact]
        public void DnsString_SimpleValidWithRoot()
        {
            var name = DnsString.ParseQueryString("abc.xyz.example.com.");

            Assert.Equal(name.Value, "abc.xyz.example.com.");
        }

        [Fact]
        public void QueryName_AcceptsService()
        {
            var name = DnsString.ParseQueryString("_abc._xyz._example.com");
            var strValue = name.ToString();

            // ending
            Assert.Equal("_abc._xyz._example.com.", strValue);
        }

        [Fact]
        public void DnsString_AcceptsPuny()
        {
            var val = "xn--4gbrim.xn----ymcbaaajlc6dj7bxne2c.xn--wgbh1c";
            DnsString name = DnsString.ParseQueryString(val);

            Assert.Equal(name.ToString(), val + ".");
        }

        [Fact]
        public void DnsString_ReadPuny_IDNA2003_Invalid()
        {
            // IDNA new would be: "xn--fuball-cta.example"
            var expected = "fussball.example.";
            var val = "fußball.example";
            DnsString result = DnsString.ParseQueryString(val);
            Assert.Equal(expected, result.Value);
        }

        [Fact]
        public void QDnsString_ParsePuny()
        {
            var domain = "müsli.de";
            DnsString name = DnsString.ParseQueryString(domain);
            Assert.Equal("xn--msli-0ra.de.", name.Value);
        }

        [Fact]
        public void DnsString_ParseNullQueryString()
        {
            Action act = () => DnsString.ParseQueryString(null);

            Assert.ThrowsAny<ArgumentNullException>(act);
        }

        [Fact]
        public void DnsString_EmptyToRoot()
        {
            DnsString query = DnsString.ParseQueryString("");

            Assert.Equal(".", query.Value);
        }

        [Fact]
        public void DnsString_LongLabel()
        {
            var ex = Record.Exception(() => DnsString.ParseQueryString("www.goo0000000000000000000000000000000000000000000000000000000000001.com"));

            Assert.NotNull(ex);
            Assert.Contains("is longer than " + DnsString.LabelMaxLength, ex.Message);
        }

        [Fact]
        public void DnsString_LongQuery()
        {
            var ex = Record.Exception(() => DnsString.ParseQueryString("www.goo000000000000000000000000000000000000000000000000000000000000.goo000000000000000000000000000000000000000000000000000000000000.goo000000000000000000000000000000000000000000000000000000000000.goo000000000000000000000000000000000000000000000000000.com"));

            Assert.NotNull(ex);
            Assert.Contains("maximum of " + DnsString.QueryMaxLength, ex.Message);
        }

        [Fact]
        public void DnsString_IllegalEscape()
        {
            Action act = () => DnsString.ParseQueryString("abc.zy\\.z.com");

            var ex = Record.Exception(act);
            Assert.Contains("not a valid host name", ex.Message);
        }

        [Fact]
        public void DnsString_StartsWithDot()
        {
            var ex = Record.Exception(() => DnsString.ParseQueryString(".www.google.com"));

            Assert.NotNull(ex);
            Assert.Contains("found leading root", ex.Message);
        }

        [Fact]
        public void DnsString_Hashcode()
        {
            var val = "Aäb.bÜlöb.cscöälüpqläö.d.com.";
            DnsString name = DnsString.ParseQueryString(val);
            DnsString name2 = DnsString.ParseQueryString(val);

            Assert.NotNull(name.GetHashCode());
            Assert.True(name.GetHashCode().Equals(name2.GetHashCode()));
        }

        [Fact]
        public void DnsString_HashcodeNot()
        {
            var val = "Aäb.bÜlöb.cscöälüpqläö.d.com.";
            DnsString name = DnsString.ParseQueryString(val);
            DnsString name2 = DnsString.ParseQueryString("asdasd");

            Assert.NotNull(name.GetHashCode());
            Assert.False(name.GetHashCode().Equals(name2.GetHashCode()));
        }

        [Fact]
        public void DnsString_Equals()
        {
            var val = "Aäb.bÜlöb.cscöälüpqläö.d.com.";
            DnsString name = DnsString.ParseQueryString(val);
            DnsString name2 = DnsString.ParseQueryString(val);

            Assert.True(name.Equals(name2));
        }

        [Fact]
        public void DnsString_EqualsString()
        {
            var val = "abc.";
            DnsString name = DnsString.ParseQueryString(val);
            string name2 = val;

            Assert.True(name.Equals(name2));
        }

        [Fact]
        public void DnsString_NotEquals()
        {
            DnsString name = DnsString.ParseQueryString("abc");
            DnsString name2 = DnsString.ParseQueryString("abc2");

            Assert.False(name.Equals(name2));
        }

        [Fact]
        public void DnsString_NotEqualsNull()
        {
            DnsString name = DnsString.ParseQueryString("abc");
            DnsString name2 = null;

            Assert.False(name.Equals(name2));
        }

        [Fact]
        public void DnsString_NotEqualsNotName()
        {
            DnsString name = DnsString.ParseQueryString("abc");
            object name2 = new object();

            Assert.False(name.Equals(name2));
        }
    }
}