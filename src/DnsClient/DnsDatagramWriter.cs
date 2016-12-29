using System;
using System.Net;

namespace DnsClient
{
    internal class DnsDatagramWriter
    {
        private byte[] _buffer;

        public byte[] Data => _buffer;

        public int Index { get; set; }

        public DnsDatagramWriter(int length)
        {
            _buffer = new byte[length];
        }

        private DnsDatagramWriter(byte[] data, int newLength)
        {
            if (data.Length > newLength)
            {
                throw new ArgumentOutOfRangeException(nameof(newLength));
            }

            _buffer = new byte[newLength];
            Array.Copy(data, _buffer, data.Length);
        }

        /// <summary>
        /// Extends the buffer of this <see cref="DnsDatagramWriter"/> instance by <paramref name="length"/>.
        /// </summary>
        /// <param name="length">The amount of bytes the current buffer should be extended by.</param>
        public void Extend(int length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            var fullLength = length + _buffer.Length;
            var newBuffer = new byte[fullLength];
            Array.Copy(_buffer, 0, newBuffer, 0, _buffer.Length);
            _buffer = newBuffer;
        }

        public void WriteBytes(byte[] data, int length) => SetBytes(data, 0, Index, length);

        public void WriteInt32(int value) => SetInt(value, Index);

        public void WriteInt32NetworkOrder(int value) => SetIntNetwork(value, Index);

        public void WriteInt16(short value) => SetInt16(value, Index);

        public void WriteInt16NetworkOrder(short value) => SetInt16Network(value, Index);
        
        public void WriteUInt32NetworkOrder(uint value)
        {
            var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)value));
            SetBytes(bytes, Index, bytes.Length);
        }
        
        public void WriteUInt16(ushort value) => SetInt16((short)value, Index);
        
        public void WriteUInt16NetworkOrder(ushort value) => SetInt16Network((short)value, Index);

        private void SetBytes(byte[] data, int destOffset, int length)
        {
            SetBytes(data, 0, destOffset, length);
        }

        private void SetBytes(byte[] data, int dataOffset, int destOffset, int length)
        {
            if (length + dataOffset > data.Length || length + dataOffset > _buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            if (destOffset + dataOffset + length > _buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(destOffset));
            }

            Array.ConstrainedCopy(data, dataOffset, _buffer, destOffset, length);
            Index = destOffset + length;
        }

        private void SetInt(int value, int offset)
        {
            var bytes = BitConverter.GetBytes(value);
            SetBytes(bytes, offset, bytes.Length);
        }

        private void SetInt16(short value, int offset)
        {
            var bytes = BitConverter.GetBytes(value);
            SetBytes(bytes, offset, bytes.Length);
        }

        private void SetInt16Network(short value, int offset)
        {
            var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
            SetBytes(bytes, offset, bytes.Length);
        }

        private void SetIntNetwork(int value, int offset)
        {
            var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
            SetBytes(bytes, offset, bytes.Length);
        }
    }
}