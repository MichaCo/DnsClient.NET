using System;
using System.Linq;
using Xunit;

namespace DnsClient.Tests
{
    public class DatagramReaderTest
    {
        public DatagramReaderTest()
        {
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

            Assert.Equal(result, 1);
            Assert.Equal(reader.Index, 2);
        }

        [Fact]
        public void DatagramReader_ReadUIntReverse()
        {
            var reader = new DnsDatagramReader(new ArraySegment<byte>(new byte[2] { 0, 1 }));

            var result = reader.ReadUInt16NetworkOrder();

            Assert.Equal(result, 1);
            Assert.Equal(reader.Index, 2);
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

        [Fact]
        public void DatagramReader_ReadName()
        {
            var host = "www.cachemanager.net";
            var name = new DnsName(host);
            var reader = new DnsDatagramReader(new ArraySegment<byte>((name.GetBytes().ToArray())));

            var result = reader.ReadName();

            Assert.Equal(result.ToString(), name.ToString());
            Assert.Equal(reader.Index, name.Octets);
        }

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