using System;
using System.Net;

namespace DnsClient2
{
    public class DnsDatagramWriter
    {
        private readonly byte[] _buffer;

        public byte[] Data => _buffer;

        public int Offset { get; set; }

        public DnsDatagramWriter(int length)
        {
            _buffer = new byte[length];
        }

        public DnsDatagramWriter(byte[] data, int newLength)
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
        public DnsDatagramWriter Extend(int byLength)
        {
            if (byLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byLength));
            }

            var fullLength = byLength + _buffer.Length;
            var newBuffer = new byte[fullLength];
            Array.Copy(_buffer, 0, newBuffer, 0, _buffer.Length);
            return new DnsDatagramWriter(newBuffer, fullLength) { Offset = Offset };
        }

        public void SetBytes(byte[] data, int length) => SetBytes(data, Offset, length);

        public void SetBytes(byte[] data, int destOffset, int length)
        {
            SetBytes(data, 0, destOffset, length);
        }

        public void SetBytes(byte[] data, int dataOffset, int destOffset, int length)
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
            Offset = destOffset + length;
        }

        public void SetInt(int value) => SetInt(value, Offset);

        public void SetInt(int value, int offset)
        {
            var bytes = BitConverter.GetBytes(value);
            SetBytes(bytes, offset, bytes.Length);
        }

        public void SetIntNetwork(int value) => SetIntNetwork(value, Offset);

        public void SetIntNetwork(int value, int offset)
        {
            var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
            SetBytes(bytes, offset, bytes.Length);
        }

        public void SetShort(short value) => SetShort(value, Offset);

        public void SetShort(short value, int offset)
        {
            var bytes = BitConverter.GetBytes(value);
            SetBytes(bytes, offset, bytes.Length);
        }

        public void SetShortNetwork(short value) => SetShortNetwork(value, Offset);

        public void SetShortNetwork(short value, int offset)
        {
            var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
            SetBytes(bytes, offset, bytes.Length);
        }

        public void SetUShort(ushort value) => SetUShort(value, Offset);

        public void SetUShort(ushort value, int offset) => SetShort((short)value, offset);

        public void SetUShortNetwork(ushort value) => SetUShortNetwork(value, Offset);

        public void SetUShortNetwork(ushort value, int offset) => SetShortNetwork((short)value, offset);
    }
}