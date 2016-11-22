using System;
using System.Net;
using System.Text;

namespace DnsClient.Protocol
{
    public class DnsDatagramReader
    {
        private readonly byte[] _data;
        private int _index;

        public int Index
        {
            get
            {
                return _index;
            }
            set
            {
                if (value < 0 || value > _data.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _index = value;
            }
        }

        public DnsDatagramReader(byte[] data, int startIndex = 0)
        {
            _data = data;
            Index = startIndex;
        }

        /*
         https://tools.ietf.org/html/rfc1035#section-3.3:
        <character-string> is a single
        length octet followed by that number of characters.  <character-string>
        is treated as binary information, and can be up to 256 characters in
        length (including the length octet).
         * */
        /// <summary>
        /// Reads the single length octet and the following characters as ASCII text.
        /// </summary>
        /// <returns></returns>
        public string ReadString()
        {
            var length = ReadByte();

            var result = Encoding.ASCII.GetString(_data, _index, length);
            _index += length;
            return result;
        }

        public byte ReadByte()
        {
            if (_index >= _data.Length)
            {
                throw new IndexOutOfRangeException("Cannot read byte.");
            }
            else
            {
                return _data[_index++];
            }
        }

        public byte[] ReadBytes(int length)
        {
            if (_data.Length < _index + length)
            {
                throw new IndexOutOfRangeException($"Cannot read that many bytes: '{length}'.");
            }

            var result = new byte[length];
            Array.Copy(_data, _index, result, 0, length);
            _index += length;
            return result;
        }

        /// <summary>
        /// Reads an IP address from the next 4 bytes.
        /// </summary>
        /// <returns>The <see cref="IPAddress"/>.</returns>
        /// <exception cref="IndexOutOfRangeException">If there are no 4 bytes to read.</exception>
        public IPAddress ReadIPAddress()
        {
            if (_data.Length < _index + 4)
            {
                throw new IndexOutOfRangeException("IPAddress expected exactly 4 bytes.");
            }

            return new IPAddress(ReadBytes(4));
        }

        public IPAddress ReadIPv6Address()
        {
            var address = new IPAddress(ReadBytes(8 * 2));

            return address;
        }

        public DnsName ReadName()
        {
            return DnsName.FromBytes(_data, ref _index);
        }

        [CLSCompliant(false)]
        public ushort ReadUInt16()
        {
            if (_data.Length < Index + 2)
            {
                throw new IndexOutOfRangeException("Cannot read more data.");
            }

            var result = BitConverter.ToUInt16(_data, _index);
            _index += 2;
            return result;
        }

        [CLSCompliant(false)]
        public ushort ReadUInt16Reverse()
        {
            if (_data.Length < Index + 2)
            {
                throw new IndexOutOfRangeException("Cannot read more data.");
            }

            byte a = _data[_index++], b = _data[_index++];
            return (ushort)(a << 8 | b);
        }

        [CLSCompliant(false)]
        public uint ReadUInt32Reverse()
        {
            return (uint)(ReadUInt16Reverse() << 16 | ReadUInt16Reverse());
        }
    }
}