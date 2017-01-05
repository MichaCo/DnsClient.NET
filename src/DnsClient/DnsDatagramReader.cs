using System;
using System.Linq;
using System.Net;
using System.Text;

namespace DnsClient
{
    internal class DnsDatagramReader
    {
        public const int IPv6Length = 16;
        public const int IPv4Length = 4;

        private readonly ArraySegment<byte> _data;
        private int _index;

        public int Index
        {
            get
            {
                return _index;
            }
            set
            {
                if (value < 0 || value > _data.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _index = value;
            }
        }

        public DnsDatagramReader(ArraySegment<byte> data, int startIndex = 0)
        {
            _data = data;
            Index = startIndex;
        }

        public string ReadString()
        {
            var length = ReadByte();

            var result = Encoding.ASCII.GetString(_data.Array, _data.Offset + _index, length);
            _index += length;
            return result;
        }

        public string ParseString()
        {
            var length = ReadByte();
            return ParseString(this, length);
        }

        /// <summary>
        /// As defined in https://tools.ietf.org/html/rfc1035#section-5.1 except '()' or '@' or '.'
        /// </summary>
        public static string ParseString(ArraySegment<byte> data)
        {
            var result = ParseString(new DnsDatagramReader(data, 0), data.Count);
            return result;
        }

        public static string ParseString(DnsDatagramReader reader, int length)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < length; i++)
            {
                byte b = reader.ReadByte();
                char c = (char)b;

                if (b < 32 || b > 126)
                {
                    builder.Append("\\" + b.ToString("000"));
                }
                else if (c == ';')
                {
                    builder.Append("\\;");
                }
                else if (c == '\\')
                {
                    builder.Append("\\\\");
                }
                else if (c == '"')
                {
                    builder.Append("\\\"");
                }
                else
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        public string ReadUTF8String()
        {
            var length = ReadByte();
            return ReadUTF8String(this, length);
        }

        public static string ReadUTF8String(ArraySegment<byte> data)
        {
            return Encoding.UTF8.GetString(data.Array, data.Offset, data.Count);
        }

        public static string ReadUTF8String(DnsDatagramReader reader, int length)
        {
            var data = reader.ReadBytes(length);
            return ReadUTF8String(data);
        }

        public byte ReadByte()
        {
            if (_data.Count < _index + 1)
            {
                throw new IndexOutOfRangeException("Cannot read byte.");
            }
            else
            {
                return _data.Array[_data.Offset + _index++];
            }
        }

        public ArraySegment<byte> ReadBytes(int length)
        {
            if (_data.Count < _index + length)
            {
                throw new IndexOutOfRangeException($"Cannot read that many bytes: '{length}'.");
            }

            var result = new ArraySegment<byte>(_data.Array, _data.Offset + _index, length);
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
            if (_data.Count < _index + IPv4Length)
            {
                throw new IndexOutOfRangeException($"Reading IPv4 address, expected {IPv4Length} bytes.");
            }

            return new IPAddress(ReadBytes(4).ToArray());
        }

        public IPAddress ReadIPv6Address()
        {
            if (_data.Count < _index + IPv6Length)
            {
                throw new IndexOutOfRangeException($"Reading IPv6 address, expected {IPv6Length} bytes.");
            }

            return new IPAddress(ReadBytes(IPv6Length).ToArray());
        }

        public DnsName ReadName()
        {
            var bytesRead = 0;
            var name = DnsName.FromBytes(new ArraySegment<byte>(_data.Array, _data.Offset + _index, _data.Count - _index), out bytesRead);
            _index += bytesRead;
            return name;
        }

        public ushort ReadUInt16()
        {
            if (_data.Count < Index + 2)
            {
                throw new IndexOutOfRangeException("Cannot read more data.");
            }

            var result = BitConverter.ToUInt16(_data.Array, _data.Offset + _index);
            _index += 2;
            return result;
        }

        public ushort ReadUInt16NetworkOrder()
        {
            if (_data.Count < Index + 2)
            {
                throw new IndexOutOfRangeException("Cannot read more data.");
            }

            byte a = _data.ElementAt(_index++), b = _data.ElementAt(_index++);
            return (ushort)(a << 8 | b);
        }

        public uint ReadUInt32NetworkOrder()
        {
            return (uint)(ReadUInt16NetworkOrder() << 16 | ReadUInt16NetworkOrder());
        }
    }
}