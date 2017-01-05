using System;
using System.Linq;
using System.Net;
using System.Text;

namespace DnsClient
{
    internal class DnsDatagramWriter : IDisposable
    {
        // queries can only be 255 octets + some header bytes, so that size is pretty safe...
        private const int MaxBufferSize = 4096;

        private readonly PooledBytes _pooledBytes;

        private ArraySegment<byte> _buffer;

        public byte[] Data
        {
            get
            {
                if (Index >= MaxBufferSize)
                {
                    throw new NotSupportedException("Buffer size exceeded.");
                }

                return new ArraySegment<byte>(_buffer.Array, 0, Index).ToArray();
            }
        }

        public int Index { get; set; }

        public DnsDatagramWriter()
        {
            _pooledBytes = new PooledBytes(MaxBufferSize);
            _buffer = new ArraySegment<byte>(_pooledBytes.Buffer, 0, 4096);
        }
        
        public void WriteByte(byte b)
        {
            _buffer.Array[_buffer.Offset + Index++] = b;
        }

        public void WriteBytes(byte[] data, int length) => SetBytes(data, 0, Index, length);

        public void WriteQueryName(string queryName)
        {
            _buffer.Array[_buffer.Offset + Index++] = (byte)queryName.Length;
            Encoding.ASCII.GetBytes(queryName, 0, queryName.Length, _buffer.Array, _buffer.Offset + Index);
            Index += queryName.Length;
            _buffer.Array[_buffer.Offset + Index++] = 0;
        }

        public void WriteInt32NetworkOrder(int value) => SetInt32Network(value, Index);

        public void WriteInt16NetworkOrder(short value) => SetInt16Network(value, Index);

        public void WriteUInt32NetworkOrder(uint value)
        {
            var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)value));
            SetBytes(bytes, Index, bytes.Length);
        }

        public void WriteUInt16NetworkOrder(ushort value) => SetInt16Network((short)value, Index);

        private void SetBytes(byte[] data, int destOffset, int length)
        {
            SetBytes(data, 0, destOffset, length);
        }

        private void SetBytes(byte[] data, int dataOffset, int destOffset, int length)
        {
            if (length + dataOffset > data.Length || length + dataOffset > _buffer.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            //if (destOffset + dataOffset + length > _buffer.Count)
            //{
            //    throw new ArgumentOutOfRangeException(nameof(destOffset));
            //}

            //Array.ConstrainedCopy(data, dataOffset, _buffer, destOffset, length);

            Buffer.BlockCopy(data, dataOffset, _buffer.Array, _buffer.Offset + destOffset, length);
            Index = destOffset + length;
        }

        private void SetInt16Network(short value, int offset)
        {
            var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
            SetBytes(bytes, offset, bytes.Length);
        }

        private void SetInt32Network(int value, int offset)
        {
            var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
            SetBytes(bytes, offset, bytes.Length);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pooledBytes.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}