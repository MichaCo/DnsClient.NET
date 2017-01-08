using System;
using System.Linq;
using Xunit;

namespace DnsClient.Tests
{
    public class QueryNameTest
    {
        [Fact]
        public void QueryName_NewEmpty()
        {
            var name = (QueryName)"";

            Assert.Equal(QueryName.Root, name);
            Assert.Equal(name.ToString(), ".");
        }

        [Fact]
        public void QueryName_InitWithNull()
        {
            QueryName name;
            Action act = () => name = new QueryName(null);

            Assert.ThrowsAny<ArgumentNullException>(act);
        }

        [Fact]
        public void QueryName_WrongRootLabel()
        {
            QueryName name;
            Action act = () => name = new QueryName(".lol");

            Assert.ThrowsAny<ArgumentException>(act);
        }

        [Fact]
        public void QueryName_NewDot()
        {
            QueryName name = ".";

            Assert.Equal(QueryName.Root, name);
            Assert.Equal(name.ToString(), ".");
        }

        [Fact]
        public void QueryName_Hashcode()
        {
            var val = "Aäb.bÜlöb.cscöälüpqläö.d.com.";
            QueryName name = val;
            QueryName name2 = val;

            Assert.NotNull(name.GetHashCode());
            Assert.True(name.GetHashCode().Equals(name2.GetHashCode()));
        }

        [Fact]
        public void QueryName_HashcodeNot()
        {
            var val = "Aäb.bÜlöb.cscöälüpqläö.d.com.";
            QueryName name = val;
            QueryName name2 = "asdasd";

            Assert.NotNull(name.GetHashCode());
            Assert.False(name.GetHashCode().Equals(name2.GetHashCode()));
        }

        [Fact]
        public void QueryName_Equals()
        {
            var val = "Aäb.bÜlöb.cscöälüpqläö.d.com.";
            QueryName name = val;
            QueryName name2 = val;

            Assert.True(name.Equals(name2));
        }

        [Fact]
        public void QueryName_NotEquals()
        {
            QueryName name = "abc";
            QueryName name2 = "abc2";

            Assert.False(name.Equals(name2));
        }

        [Fact]
        public void QueryName_NotEqualsNull()
        {
            QueryName name = "abc";
            QueryName name2 = null;

            Assert.False(name.Equals(name2));
        }

        [Fact]
        public void QueryName_NotEqualsNotName()
        {
            QueryName name = "abc";
            object name2 = new object();

            Assert.False(name.Equals(name2));
        }

        [Fact]
        public void QueryName_CompareToStringToName()
        {
            string name = "abc";
            QueryName name2 = "abc";

            // not the same (ToString of name2 contains a "."
            Assert.True(name.CompareTo(name2) == -1);
        }

        [Fact]
        public void QueryName_CompareToStringToNameSameAsToString()
        {
            string name = "abc.";
            QueryName name2 = "abc";

            // not the same
            Assert.True(name.CompareTo(name2) == 0);
        }

        [Fact]
        public void QueryName_ReceiveEscapedName()
        {
            var bytes = new byte[] { 4, 65, 195, 164, 98, 7, 98, 195, 156, 108, 195, 182, 98, 2, 99, 100, 3, 99, 111, 109, 0 };

            var reader = new DnsDatagramReader(new ArraySegment<byte>(bytes));
            var queryName = reader.ReadQueryName();

            //  should be "Aäb.bÜlöb.cd.com."
            Assert.Equal(queryName.Original, "Aäb.bÜlöb.cd.com.");

            //  Puny escaped
            Assert.Equal(queryName, "xn--ab-via.xn--blb-tna2a.cd.com.");
        }

        [Fact]
        public void QueryName_SimpleValid()
        {
            var name = new QueryName("abc.xyz.example.com");
            var strValue = name.ToString();

            // ending
            Assert.Equal(name, "abc.xyz.example.com.");
        }

        [Fact]
        public void QueryName_HasEnding()
        {
            var name = new QueryName("abc.xyz.example.com.");
            var strValue = name.ToString();

            // ending
            Assert.Equal(name, "abc.xyz.example.com.");
        }

        [Fact]
        public void QueryName_EscapingNoHostName()
        {
            QueryName result;
            Action act = () => result = "abc.zy\\.z.com";

            var ex = Record.Exception(act);
            Assert.Contains("not a valid host name", ex.Message);
        }

        [Fact]
        public void QueryName_ReadPuny()
        {
            var val = "xn--4gbrim.xn----ymcbaaajlc6dj7bxne2c.xn--wgbh1c";
            QueryName name = val;

            Assert.Equal(name.ToString(), val + ".");
        }

        [Fact]
        public void QueryName_ReadPuny_IDNA2003_Invalid()
        {
            // IDNA new would be: "xn--fuball-cta.example"
            var expected = "fussball.example.";
            var val = "fußball.example";
            string result = (QueryName)val;
            Assert.Equal(expected, result);
        }

        [Fact]
        public void QueryName_ParsePuny()
        {
            var domain = "müsli.de";
            QueryName name = domain;
            Assert.Equal("xn--msli-0ra.de.", name);
        }

        [Fact]
        public void QueryName_ImplicitToString()
        {
            var name = new QueryName("xyz.ns1.hallo.com");
            Assert.Equal(name, "xyz.ns1.hallo.com.");
        }
    }
}