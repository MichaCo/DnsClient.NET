using System;
using System.Linq;
using System.Threading.Tasks;
using DnsClient.Internal;
using Xunit;

namespace DnsClient.Tests
{

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class DatagramReaderTest
    {
        private static readonly byte[] s_referenceBitData = new byte[]
        {
            2, 97, 97, 3, 99, 111, 109, 0, // aa.com.       0-8
            2, 98, 192, 0,                 // b.ref to 0    8-11
            1, 99, 192, 0, 0,              // c.ref to 0    12-16
            2, 100, 100, 192, 12, 0,       // dd.ref to 12  17-22
            5, 101, 101, 101, 101, 101, 192, 17, 0, 0, 0,     // eeeee.ref to 17 23-33
            0, // 34
            0, // 35
        };

        static DatagramReaderTest()
        {
            Tracing.Source.Switch.Level = System.Diagnostics.SourceLevels.All;
        }

        [Fact]
        public void DatagramReader_LabelTest_DnsName()
        {
            var data = s_referenceBitData.Concat(new byte[] { 192, 0 });
            var reader = new DnsDatagramReader(new ArraySegment<byte>(data.ToArray()));

            reader.Advance(36);
            DnsString name = reader.ReadDnsName();
            Assert.Equal("aa.com.", name.Value);
        }

        [Fact]
        public void DatagramReader_LabelTest_DnsName_LengthOutOfBounds()
        {
            var data = s_referenceBitData.Concat(new byte[] { 192, 23 });
            var reader = new DnsDatagramReader(new ArraySegment<byte>(data.ToArray()));

            reader.Advance(36);
            DnsString name = reader.ReadDnsName();
            Assert.Equal("eeeee.dd.c.aa.com.", name.Value);
        }

        [Fact]
        public void DatagramReader_Labels_FromBytesValid()
        {
            var bytes = new byte[] { 5, 90, 90, 92, 46, 90, 2, 56, 56, 0 };

            var reader = new DnsDatagramReader(new ArraySegment<byte>(bytes));
            var recursion = 0;
            var labels = reader.ReadLabels(ref recursion);
            Assert.Equal(2, labels.Count);
        }

        [Fact]
        public void DatagramReader_Labels_FromBytesRecursionMax()
        {
            var bytes = new byte[] { 5, 90, 90, 92, 46, 90, 2, 56, 56, 0 };

            var reader = new DnsDatagramReader(new ArraySegment<byte>(bytes), maxRecursion: 1);
            Assert.Throws<DnsResponseParseException>(() =>
            {
                var recursion = 0;
                var _ = reader.ReadLabels(ref recursion);
            });
        }

        [Fact]
        public void DatagramReader_DnsName_FromBytesInvalidLength()
        {
            var bytes = new byte[] { 3, 90, 90, 90, 6, 56, 56, 0 };

            var reader = new DnsDatagramReader(new ArraySegment<byte>(bytes));

            Action act = () => reader.ReadDnsName();
            var ex = Assert.ThrowsAny<DnsResponseParseException>(act);
            Assert.Equal(5, ex.Index);
            Assert.Equal(6, ex.ReadLength);
        }

        [Fact]
        public void DatagramReader_DnsName_FromBytesInvalidOffset()
        {
            var reader = new DnsDatagramReader(new ArraySegment<byte>(new byte[] { 2 }));

            Action act = () => reader.ReadDnsName();
            var ex = Assert.ThrowsAny<DnsResponseParseException>(act);
            Assert.Equal(1, ex.Index);
            Assert.Single(ex.ResponseData);
            Assert.Equal(2, ex.ReadLength);
        }

        [Fact]
        public void DatagramReader_IndexOutOfRange()
        {
            Action act = () => _ = new DnsDatagramReader(new ArraySegment<byte>(new byte[10]), 11);
            Assert.ThrowsAny<ArgumentOutOfRangeException>(act);
        }

        [Fact]
        public void DatagramReader_ReadByte_IndexOutOfRange()
        {
            var reader = new DnsDatagramReader(new ArraySegment<byte>(new byte[10]), 9);
            reader.ReadByte();

            Action act = () => reader.ReadByte();
            var ex = Assert.ThrowsAny<DnsResponseParseException>(act);

            Assert.Equal(10, ex.Index);
            Assert.Equal(1, ex.ReadLength);
        }

        [Fact]
        public void DatagramReader_IndexOutOfRangeNegativ()
        {
            Action act = () => _ = new DnsDatagramReader(new ArraySegment<byte>(new byte[10]), -1);
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
            var ex = Assert.ThrowsAny<DnsResponseParseException>(act);

            Assert.Equal(2, ex.Index);
            Assert.Equal(2, ex.ReadLength);
        }

        [Fact]
        public void DatagramReader_ReadUIntReverseIndexOutOfRange()
        {
            var reader = new DnsDatagramReader(new ArraySegment<byte>(new byte[2] { 0, 1 }));

            var result = reader.ReadUInt16NetworkOrder();

            Action act = () => reader.ReadUInt16NetworkOrder();
            var ex = Assert.ThrowsAny<DnsResponseParseException>(act);

            Assert.Equal(2, ex.Index);
            Assert.Equal(2, ex.ReadLength);
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

        [Fact]
        public void DatagramReader_IndexBounds()
        {
            var bytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 };
            var reader = new DnsDatagramReader(new ArraySegment<byte>(bytes, 2, 4));

            Assert.True(reader.DataAvailable);
            reader.Advance(4);
            Assert.False(reader.DataAvailable);

            Action act = () => reader.Advance(1);
            var ex = Assert.Throws<DnsResponseParseException>(act);
            Assert.Equal(4, ex.Index);
            Assert.Equal(1, ex.ReadLength);
        }

        [Fact]
        public void Pool_ParallelTest()
        {
            for (var i = 0; i < 100; i++)
            {
                Parallel.Invoke(
                    new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = 16
                    },
                    Enumerable.Repeat<Action>(() => BuildSomething(), 200).ToArray());
            }

            void BuildSomething()
            {
                var a = StringBuilderObjectPool.Default.Get();
                var b = StringBuilderObjectPool.Default.Get();

                for (var i = 0; i < 100; i++)
                {
                    a.Append("something");
                    b.Append("something else");
                }

                var x = a.ToString();
                var y = b.ToString();

                StringBuilderObjectPool.Default.Return(a);
                StringBuilderObjectPool.Default.Return(b);
            }
        }
    }
}
