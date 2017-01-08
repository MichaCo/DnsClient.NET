using System;
using System.Linq;
using Xunit;

namespace DnsClient.Tests
{
    public class DnsNameTest
    {
        [Fact]
        public void DnsName_NewEmpty()
        {
            var name = (DnsName)"";

            Assert.Equal(DnsName.Root, name);
            Assert.Equal(name.ToString(), ".");
        }

        [Fact]
        public void DnsName_Indexer()
        {
            DnsName name = "test";

            Assert.Equal(name[0], "test");
            Assert.Equal(name.IsHostName, true);
            Assert.Equal(name.IsEmpty, false);
            Assert.Equal(name.Octets, 6);
            Assert.Equal(name.Size, 1);
        }

        [Fact]
        public void DnsName_IndexerOver()
        {
            DnsName name = "test";

            Action act = () => Assert.Equal(name[1], "test");

            Assert.ThrowsAny<ArgumentOutOfRangeException>(act);
        }

        [Fact]
        public void DnsName_IndexerUnder()
        {
            DnsName name = "test";

            Action act = () => Assert.Equal(name[-1], "test");

            Assert.ThrowsAny<ArgumentOutOfRangeException>(act);
        }

        [Fact]
        public void DnsName_IndexerUnderEmpty()
        {
            DnsName name = "";

            Action act = () => Assert.Equal(name[0], "test");

            Assert.ThrowsAny<ArgumentOutOfRangeException>(act);
        }

        [Fact]
        public void DnsName_InitWithNull()
        {
            DnsName name;
            Action act = () => name = new DnsName(null);

            Assert.ThrowsAny<ArgumentNullException>(act);
        }

        [Fact]
        public void DnsName_WrongRootLabel()
        {
            DnsName name;
            Action act = () => name = new DnsName(".lol");

            Assert.ThrowsAny<ArgumentException>(act);
        }

        [Fact]
        public void DnsName_NewDot()
        {
            var name = (DnsName)".";

            Assert.Equal(DnsName.Root, name);
            Assert.Equal(name.ToString(), ".");
        }

        [Fact]
        public void DnsName_Hashcode()
        {
            var val = "Aäb.bÜlöb.c\\@{}scöälüpqläö.d.com.";
            var name = (DnsName)val;
            var name2 = (DnsName)val;

            Assert.NotNull(name.GetHashCode());
            Assert.True(name.GetHashCode().Equals(name2.GetHashCode()));
        }

        [Fact]
        public void DnsName_HashcodeNot()
        {
            var val = "Aäb.bÜlöb.c\\@{}scöälüpqläö.d.com.";
            var name = (DnsName)val;
            var name2 = (DnsName)"asdasd";

            Assert.NotNull(name.GetHashCode());
            Assert.False(name.GetHashCode().Equals(name2.GetHashCode()));
        }

        [Fact]
        public void DnsName_Equals()
        {
            var val = "Aäb.bÜlöb.c\\@{}scöälüpqläö.d.com.";
            var name = (DnsName)val;
            var name2 = (DnsName)val;

            Assert.True(name.Equals(name2));
        }

        [Fact]
        public void DnsName_NotEquals()
        {
            DnsName name = "abc";
            DnsName name2 = "abc2";

            Assert.False(name.Equals(name2));
        }

        [Fact]
        public void DnsName_NotEqualsNull()
        {
            DnsName name = "abc";
            DnsName name2 = null;

            Assert.False(name.Equals(name2));
        }

        [Fact]
        public void DnsName_NotEqualsNotName()
        {
            DnsName name = "abc";
            object name2 = new object();

            Assert.False(name.Equals(name2));
        }

        [Fact]
        public void DnsName_CompareTo()
        {
            var val = "Aäb.bÜlöb.c\\@{}scöälüpqläö.d.com.";
            var name = (DnsName)val;
            var name2 = (DnsName)val;

            Assert.True(name.CompareTo(name) == 0);
        }

        [Fact]
        public void DnsName_CompareToEqual()
        {
            DnsName name = "abc";
            DnsName name2 = "abc";

            Assert.True(name.CompareTo(name2) == 0);
        }

        [Fact]
        public void DnsName_CompareToLower()
        {
            DnsName name = "abc";
            DnsName name2 = "bc";

            Assert.True(name.CompareTo(name2) == -1);
        }

        [Fact]
        public void DnsName_CompareToHigher()
        {
            DnsName name = "bcd";
            DnsName name2 = "abc";

            Assert.True(name.CompareTo(name2) == 1);
        }

        [Fact]
        public void DnsName_CompareToNull()
        {
            DnsName name = "bcd";
            DnsName name2 = null;

            Assert.True(name.CompareTo(name2) == 1);
        }

        [Fact]
        public void DnsName_CompareToString()
        {
            DnsName name = "abc";
            string name2 = "abc";

            Assert.True(name.CompareTo(name2) == 0);
        }

        [Fact]
        public void DnsName_CompareToStringNull()
        {
            DnsName name = "abc";
            string name2 = null;

            Assert.True(name.CompareTo(name2) == 1);
        }

        [Fact]
        public void DnsName_CompareToStringToName()
        {
            string name = "abc";
            DnsName name2 = "abc";

            // not the same (ToString of name2 contains a "."
            Assert.True(name.CompareTo(name2) == -1);
        }

        [Fact]
        public void DnsName_CompareToStringToNameSameAsToString()
        {
            string name = "abc.";
            DnsName name2 = "abc";

            // not the same
            Assert.True(name.CompareTo(name2) == 0);
        }

        [Fact]
        public void DnsName_CompareToObject()
        {
            DnsName name = "abc.";
            object name2 = "abc.";

            // not the same
            Assert.True(name.CompareTo(name2) == 0);
        }

        [Fact]
        public void DnsName_CompareToObject2()
        {
            DnsName name = "123";
            int name2 = 123;

            // not the same
            Assert.True(name.CompareTo(name2) == 1);
        }

        [Fact]
        public void DnsName_CompareToObject3()
        {
            DnsName name = "123";
            object name2 = null;

            // not the same
            Assert.True(name.CompareTo(name2) == 1);
        }

        [Fact]
        public void DnsName_ReceiveEscapedName()
        {
            var bytes = new byte[] { 4, 65, 195, 164, 98, 7, 98, 195, 156, 108, 195, 182, 98, 4, 99, 92, 46, 100, 3, 99, 111, 109, 0 };

            var reader = new DnsDatagramReader(new ArraySegment<byte>(bytes));
            var dnsName = reader.ReadDnsName();

            //  should be "Aäb.bÜlöb.c\.d.com."
            Assert.Equal(dnsName.ToString(true), "Aäb.bÜlöb.c\\.d.com.");

            //  escaped should be A\195\164b.b\195\156l\195\182b.c\\.d.com.
            Assert.Equal(dnsName.ToString(false), "A\\195\\164b.b\\195\\156l\\195\\182b.c\\.d.com.");
        }

        [Fact]
        public void DnsName_SimpleValid()
        {
            var name = new DnsName("abc.xyz.example.com");
            var strValue = name.ToString();

            // ending
            Assert.Equal(name, "abc.xyz.example.com.");

            // domain name has 4 labels
            Assert.Equal(name.Size, 4);

            Assert.True(name.IsHostName);

            // Octet string version: 3abc3xyz7example3com0 => 21 chars
            Assert.Equal(name.Octets, 21);
        }

        [Fact]
        public void DnsName_HasEnding()
        {
            var name = new DnsName("abc.xyz.example.com.");
            var strValue = name.ToString();

            // ending
            Assert.Equal(name, "abc.xyz.example.com.");

            // domain name has 4 labels
            Assert.Equal(name.Size, 4);

            Assert.True(name.IsHostName);

            // Octet string version: 3abc3xyz7example3com0 => 21 chars
            Assert.Equal(name.Octets, 21);
        }

        [Fact]
        public void DnsName_LongName()
        {
            var longName = new DnsName("12341234123412341234123412341234123412341234.123123123123123123123123123123123123123123123123.123123123123123123");

            Assert.Equal(longName.Size, 3);
            Assert.True(longName.IsHostName);
        }

        [Fact]
        public void DnsName_EscapingNoHostName()
        {
            var name = (DnsName)"abc.zy\\.z.com";

            Assert.Equal(name[1], "zy\\.z");
            Assert.Equal(name.ToString(true), "abc.zy\\.z.com.");
            Assert.Equal(name.Size, 3);
            Assert.False(name.IsHostName);
        }

        [Fact]
        public void DnsName_EscapingWrongNumberEscape()
        {
            var name = (DnsName)"abc.zy\\1as.z.com";

            Assert.Equal(name.Value, "abc.zy\\\\1as.z.com.");
            Assert.Equal(name.ValueUTF8, "abc.zy\\1as.z.com.");
        }

        [Fact]
        public void DnsName_EscapingComplex()
        {
            DnsName name = "aa\\es\\.\\.\\;;12\\34\\195\\156abc;abc";

            Assert.Equal(name.Value, "aa\\\\es\\.\\.\\;\\;12\\\\34\\195\\156abc\\;abc.");
            Assert.Equal(name.ValueUTF8, "aa\\es\\.\\.\\;\\;12\\34Üabc\\;abc.");
        }

        [Fact]
        public void DnsName_EscapingSemi()
        {
            DnsName name = ";.\\;\\.";
            string expected = "\\;.\\;\\..";

            Assert.Equal(expected, name.Value);
            Assert.Equal(expected, name.ValueUTF8);
        }

        [Fact]
        public void DnsName_EscapingMix()
        {
            DnsName name = "abc\\195\\156\\1Ü\\.";
            string expected = "abc\\195\\156\\\\1\\195\\156\\..";
            string expectedUtf = "abcÜ\\1Ü\\..";

            Assert.Equal(name.Value, expected);
            Assert.Equal(name.ValueUTF8, expectedUtf);
        }

        [Fact]
        public void DnsName_EscapingMix2()
        {
            DnsName name = "\\Ü\\.";
            string expected = "\\\\\\195\\156\\..";
            string expectedUtf = "\\Ü\\..";

            Assert.Equal(expected, name.Value);
            Assert.Equal(expectedUtf, name.ValueUTF8);
        }

        [Fact]
        public void DnsName_EscapingLowAsci()
        {
            DnsName name = "\t\r\n";
            string expected = "\\009\\013\\010.";
            string expectedUtf = "\t\r\n.";

            Assert.Equal(expected, name.Value);
            Assert.Equal(expectedUtf, name.ValueUTF8);
        }

        [Fact]
        public void DnsName_EscapingLowRevers()
        {
            DnsName name = "\\009\\013\\010";
            string expected = "\\009\\013\\010.";
            string expectedUtf = "\t\r\n.";

            Assert.Equal(expected, name.Value);
            Assert.Equal(expectedUtf, name.ValueUTF8);
        }

        [Fact]
        public void DnsName_EscapingTheEscape()
        {
            string value = "\\abc\\\\d\\\\\\f\\";
            DnsName name = value;
            string expected = "\\\\abc\\\\d\\\\\\\\f\\\\.";

            Assert.Equal(expected, name.Value);
            Assert.Equal(value + ".", name.ValueUTF8);
        }

        [Fact]
        public void DnsName_EscapingTheEscapeGetBytes()
        {
            string value = "\\abc\\\\d\\\\\\f\\";
            DnsName name = DnsName.Parse(value);
            string expected = "\\\\abc\\\\d\\\\\\\\f\\\\.";
            Assert.Equal(expected, name.Value);

            var bytes = name.GetBytes();
            var lastBytes = bytes.Skip(bytes.Length - 3).ToArray();
            Assert.Equal(92, lastBytes[0]);
            Assert.Equal(92, lastBytes[1]);
            Assert.Equal(0, lastBytes[2]);
            var reader = new DnsDatagramReader(new ArraySegment<byte>(bytes));
            var dnsName = reader.ReadDnsName();

            Assert.Equal(expected, dnsName.Value);
        }

        [Fact]
        public void DnsName_ReadEscapedBytes()
        {
            var name = (DnsName)"l\\195\\156\\195\\164'la\\195\\188\\195\\182#\\.2x";

            var s = name.ToString();
            var utf8 = name.ToString(true);
            Assert.Equal(name[0], "l\\195\\156\\195\\164'la\\195\\188\\195\\182#\\.2x");
            Assert.Equal(utf8, "lÜä'laüö#\\.2x.");
            Assert.Equal(name.Size, 1);
            Assert.False(name.IsHostName);
        }

        [Fact]
        public void DnsName_ReadPuny()
        {
            var val = "xn--4gbrim.xn----ymcbaaajlc6dj7bxne2c.xn--wgbh1c";
            var name = (DnsName)val;

            var s = name.ToString();
            var utf8 = name.ToString(true);
            Assert.Equal(name[0], "xn--4gbrim");
            Assert.Equal(s, val + ".");
            Assert.Equal(utf8, "موقع.وزارة-الاتصالات.مصر.");
            Assert.Equal(name.Size, 3);
            Assert.True(name.IsHostName);
        }

        [Fact]
        public void DnsName_PunyGetBytes()
        {
            var val = "xn--4gbrim.xn----ymcbaaajlc6dj7bxne2c.xn--wgbh1c";
            var name = (DnsName)val;
            var bytes = name.GetBytes();

            var reader = new DnsDatagramReader(new ArraySegment<byte>(bytes));
            var dnsName = reader.ReadDnsName();

            var s = dnsName.ToString();
            var utf8 = dnsName.ToString(true);
            Assert.Equal(dnsName[0], "xn--4gbrim");
            Assert.Equal(s, val + ".");
        }

        [Fact]
        public void DnsName_ReadPuny_IDNA2003_Invalid()
        {
            var val = "xn--fuball-cta.example";
            string result;
            Action act = () => result = (DnsName)val;
            Assert.ThrowsAny<InvalidOperationException>(act);
        }

        [Fact]
        public void DnsName_ReadPuny_IDNA2003_2()
        {
            var val = "xn--n3h.example";
            var name = (DnsName)val;

            var s = name.ToString();
            var utf8 = name.ToString(true);
            Assert.Equal(s, val + ".");
            Assert.Equal(utf8, "☃.example.");
            Assert.Equal(name.Size, 2);
            Assert.True(name.IsHostName);
        }

        [Fact]
        public void DnsName_ParsePuny()
        {
            var domain = "müsli.de";

            DnsName name = DnsName.ParsePuny(domain);

            Assert.Equal("xn--msli-0ra.de.", name);
        }

        [Fact]
        public void DnsName_ConcatNull()
        {
            DnsName name = "abc";

            Func<DnsName> act = () => name.Concat(null);

            Assert.ThrowsAny<ArgumentNullException>(act);
        }

        [Fact]
        public void DnsName_Concat3()
        {
            var name = DnsName.Root;
            name = name.Concat("abc.com");

            Assert.Equal(name.ToString(), "abc.com.");
            Assert.Equal(name.Size, 2);
            Assert.True(name.IsHostName);
        }

        [Fact]
        public void DnsName_Concat2()
        {
            var name = new DnsName("üöäxyz.ns1");
            name = name.Concat("hallo.com");

            Assert.Equal(name.ToString(true), "üöäxyz.ns1.hallo.com.");
            Assert.Equal(name.Size, 4);
            Assert.False(name.IsHostName);
        }

        [Fact]
        public void DnsName_Concat()
        {
            var name = new DnsName("xyz.ns1");
            name = name.Concat("abc.com");

            Assert.Equal(name.ToString(), "xyz.ns1.abc.com.");
            Assert.Equal(name.Size, 4);
            Assert.True(name.IsHostName);
        }

        [Fact]
        public void DnsName_ImplicitToString()
        {
            var name = new DnsName("xyz.ns1.hallo.com");
            Assert.Equal(name, "xyz.ns1.hallo.com.");
        }

        [Fact]
        public void DnsName_LabelTooLarge()
        {
            // max length of one label is 63
            var label = string.Join("", Enumerable.Repeat("a", 64));
            Action act = () => new DnsName(label + ".org");

            Assert.ThrowsAny<InvalidOperationException>(act);
        }

        [Fact]
        public void DnsName_NameTooLarge()
        {
            // max length of the name in total is 255 counting the octets including zero ending
            var label = string.Join("", Enumerable.Repeat("a", 63));
            Action act = () => new DnsName(label + "." + label + "." + label + "." + label);

            Assert.ThrowsAny<InvalidOperationException>(act);
        }

        [Fact]
        public void DnsName_ManyLabels()
        {
            var label = string.Join(".", Enumerable.Repeat("a", 127));
            var name = new DnsName(label);

            Assert.Equal(name.Octets, 255);
            Assert.True(name.IsHostName);
        }

        [Fact]
        public void DnsName_FromBytesValid()
        {
            var bytes = new byte[] { 5, 90, 90, 92, 46, 90, 2, 56, 56, 0 };

            var reader = new DnsDatagramReader(new ArraySegment<byte>(bytes));
            var name = reader.ReadDnsName();

            Assert.Equal(name.Size, 2);
            Assert.Equal(name.Octets, 10);
            Assert.False(name.IsHostName);
        }

        [Fact]
        public void DnsName_FromBytesInvalidLength()
        {
            var bytes = new byte[] { 3, 90, 90, 90, 6, 56, 56, 0 };

            var reader = new DnsDatagramReader(new ArraySegment<byte>(bytes));
            Action act = () => reader.ReadDnsName();

            Assert.ThrowsAny<IndexOutOfRangeException>(act);
        }

        [Fact]
        public void DnsName_FromBytesInvalidOffset()
        {
            var reader = new DnsDatagramReader(new ArraySegment<byte>(new byte[] { 2 }));
            Action act = () => reader.ReadDnsName();

            Assert.ThrowsAny<IndexOutOfRangeException>(act);
        }
    }
}