using System;
using System.Net;
using System.Text;

namespace DnsClient
{
    internal class DnsDatagramReader
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
        
        public ushort ReadUInt16NetworkOrder()
        {
            if (_data.Length < Index + 2)
            {
                throw new IndexOutOfRangeException("Cannot read more data.");
            }

            byte a = _data[_index++], b = _data[_index++];
            return (ushort)(a << 8 | b);
        }
        
        public uint ReadUInt32NetworkOrder()
        {
            return (uint)(ReadUInt16NetworkOrder() << 16 | ReadUInt16NetworkOrder());
        }
    }
}