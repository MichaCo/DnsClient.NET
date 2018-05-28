using System;
using System.Linq;
using Xunit;

namespace DnsClient.Tests
{
    public class DatagramReaderTest
    {
        private static byte[] ReferenceBitData = new byte[]
        {
            2, 97, 97, 3, 99, 111, 109, 0, // aa.com.       0-8
            2, 98, 192, 0,                 // b.ref to 0    8-11
            1, 99, 192, 0, 0,              // c.ref to 0    12-16
            2, 100, 100, 192, 12, 0,       // dd.ref to 12  17-22
            5, 101, 101, 101, 101, 101, 192, 17, 0, 0, 0,     // eeeee.ref to 17 23-33
            0, // 34
            0, // 35
        };

        ////[Fact]
        ////public void DatagramReader_LabelTest_QueryName()
        ////{
        ////    var data = ReferenceBitData.Concat(new byte[] { 192, 0 });
        ////    var reader = new DnsDatagramReader(new ArraySegment<byte>(data.ToArray()));

        ////    reader.Index = 36;
        ////    QueryName name = reader.ReadQueryName();
        ////    Assert.Equal(name, "aa.com.");
        ////}

        ////[Fact]
        ////public void DatagramReader_LabelTest_QueryName2()
        ////{
        ////    var data = ReferenceBitData.Concat(new byte[] { 192, 23 });
        ////    var reader = new DnsDatagramReader(new ArraySegment<byte>(data.ToArray()));

        ////    reader.Index = 36;
        ////    QueryName name = reader.ReadQueryName();
        ////    Assert.Equal(name, "eeeee.dd.c.aa.com.");
        ////}

        [Fact]
        public void DatagramReader_LabelTest_DnsName()
        {
            var data = ReferenceBitData.Concat(new byte[] { 192, 0 });
            var reader = new DnsDatagramReader(new ArraySegment<byte>(data.ToArray()));

            reader.Index = 36;
            DnsString name = reader.ReadDnsName();
            Assert.Equal("aa.com.", name.Value);
        }

        [Fact]
        public void DatagramReader_LabelTest_DnsName2()
        {
            var data = ReferenceBitData.Concat(new byte[] { 192, 23 });
            var reader = new DnsDatagramReader(new ArraySegment<byte>(data.ToArray()));

            reader.Index = 36;
            DnsString name = reader.ReadDnsName();
            Assert.Equal("eeeee.dd.c.aa.com.", name.Value);
        }

        [Fact]
        public void DatagramReader_DnsName_FromBytesValid()
        {
            var bytes = new byte[] { 5, 90, 90, 92, 46, 90, 2, 56, 56, 0 };

            var reader = new DnsDatagramReader(new ArraySegment<byte>(bytes));
            var name = reader.ReadDnsName();

            //Assert.Equal(name.Size, 2);
            //Assert.Equal(name.Octets, 10);
            //Assert.False(name.IsHostName);
        }

        [Fact]
        public void DatagramReader_DnsName_FromBytesInvalidLength()
        {
            var bytes = new byte[] { 3, 90, 90, 90, 6, 56, 56, 0 };

            var reader = new DnsDatagramReader(new ArraySegment<byte>(bytes));
            Action act = () => reader.ReadDnsName();

            Assert.ThrowsAny<IndexOutOfRangeException>(act);
        }

        [Fact]
        public void DatagramReader_DnsName_FromBytesInvalidOffset()
        {
            var reader = new DnsDatagramReader(new ArraySegment<byte>(new byte[] { 2 }));
            Action act = () => reader.ReadDnsName();

            Assert.ThrowsAny<IndexOutOfRangeException>(act);
        }

        [Fact]
        public void DatagramReader_IndexOutOfRange()
        {
            Action act = () => new DnsDatagramReader(new ArraySegment<byte>(new byte[10]), 11);

            Assert.ThrowsAny<ArgumentOutOfRangeException>(act);
        }

        [Fact]
        public void DatagramReader_ReadByte_IndexOutOfRange()
        {
            var reader = new DnsDatagramReader(new ArraySegment<byte>(new byte[10]), 9);

            reader.ReadByte();
            Action act = () => reader.ReadByte();
            Assert.ThrowsAny<IndexOutOfRangeException>(act);
        }

        [Fact]
        public void DatagramReader_IndexOutOfRangeNegativ()
        {
            Action act = () => new DnsDatagramReader(new ArraySegment<byte>(new byte[10]), -1);

            Assert.ThrowsAny<ArgumentOutOfRangeException>(act);
        }

        [Fact]
        public void DatagramReader_ReadUInt()
        {
            var reader = new DnsDatagramReader(new ArraySegment<byte>(new byte[2] { 1, 0 }));

            var result = reader.ReadUInt16();

            Assert.Equal(1, result);
            Assert.Equal(2, reader.Index);
        }

        [Fact]
        public void DatagramReader_ReadUIntReverse()
        {
            var reader = new DnsDatagramReader(new ArraySegment<byte>(new byte[2] { 0, 1 }));

            var result = reader.ReadUInt16NetworkOrder();

            Assert.Equal(1, result);
            Assert.Equal(2, reader.Index);
        }

        [Fact]
        public void DatagramReader_ReadUIntIndexOutOfRange()
        {
            var reader = new DnsDatagramReader(new ArraySegment<byte>(new byte[2] { 0, 1 }));

            var result = reader.ReadUInt16();
            Action act = () => reader.ReadUInt16();

            Assert.ThrowsAny<IndexOutOfRangeException>(act);
        }

        [Fact]
        public void DatagramReader_ReadUIntReverseIndexOutOfRange()
        {
            var reader = new DnsDatagramReader(new ArraySegment<byte>(new byte[2] { 0, 1 }));

            var result = reader.ReadUInt16NetworkOrder();
            Action act = () => reader.ReadUInt16NetworkOrder();

            Assert.ThrowsAny<IndexOutOfRangeException>(act);
        }

        //[Fact]
        //public void DatagramReader_ReadName()
        //{
        //    var host = "www.cachemanager.net";
        //    var name = DnsString.ParseQueryString(host);
        //    var reader = new DnsDatagramReader(new ArraySegment<byte>((name.GetBytes().ToArray())));

        //    var result = reader.ReadDnsName();

        //    Assert.Equal(result.ToString(), name.ToString());
        //    Assert.Equal(reader.Index, name.Octets);
        //}

        [Fact]
        public void DatagramReader_ReadBytes()
        {
            var reader = new DnsDatagramReader(new ArraySegment<byte>(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }));

            reader.ReadUInt16();
            reader.ReadUInt16();
            var result = reader.ReadBytes(4);

            Assert.Equal(result, new byte[] { 4, 5, 6, 7 });
        }
    }
}