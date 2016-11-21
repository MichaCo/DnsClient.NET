using System;
using System.Net;

namespace DnsClient
{
    public class DnsDatagramWriter
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
        /// Creates a new writer instance with a new length.
        /// </summary>
        /// <param name="byLength">The amount of bytes the current buffer should be extended by.</param>
        /// <returns>A new writer.</returns>
        public void Extend(int byLength)
        {
            if (byLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byLength));
            }

            var fullLength = byLength + _buffer.Length;
            var newBuffer = new byte[fullLength];
            Array.Copy(_buffer, 0, newBuffer, 0, _buffer.Length);
            //return new DnsDatagramWriter(newBuffer, fullLength) { Offset = Offset };
            _buffer = newBuffer;
        }

        public void SetBytes(byte[] data, int length) => SetBytes(data, 0, Index, length);

        public void SetInt(int value) => SetInt(value, Index);

        public void SetInt16(short value) => SetInt16(value, Index);

        public void SetInt16Network(short value) => SetInt16Network(value, Index);

        [CLSCompliant(false)]
        public void SetUInt32Network(uint value)
        {
            var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)value));
            SetBytes(bytes, Index, bytes.Length);
        }

        public void SetIntNetwork(int value) => SetIntNetwork(value, Index);

        [CLSCompliant(false)]
        public void SetUInt16(ushort value) => SetInt16((short)value, Index);

        [CLSCompliant(false)]
        public void SetUInt16Network(ushort value) => SetInt16Network((short)value, Index);

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