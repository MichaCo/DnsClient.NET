using System;
using System.Net;
using System.Text;

namespace DnsClient
{
    internal class DnsDatagramReader
    {
        public const int IPv6Length = 16;
        public const int IPv4Length = 4;

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

        public string ParseString()
        {
            var length = ReadByte();
            return ParseString(this, length);
        }

        /// <summary>
        /// As defined in https://tools.ietf.org/html/rfc1035#section-5.1 except '()' or '@' or '.'
        /// </summary>
        public static string ParseString(byte[] data, int index, int length)
        {
            var result = ParseString(new DnsDatagramReader(data, index), length);
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

        public static string ReadUTF8String(byte[] data, int index, int length)
        {
            return Encoding.UTF8.GetString(data, index, length);
        }

        public static string ReadUTF8String(DnsDatagramReader reader, int length)
        {
            var data = reader.ReadBytes(length);
            return ReadUTF8String(data, 0, length);
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
            if (_data.Length < _index + IPv4Length)
            {
                throw new IndexOutOfRangeException($"Reading IPv4 address, expected {IPv4Length} bytes.");
            }

            return new IPAddress(ReadBytes(4));
        }

        public IPAddress ReadIPv6Address()
        {
            if (_data.Length < _index + IPv6Length)
            {
                throw new IndexOutOfRangeException($"Reading IPv6 address, expected {IPv6Length} bytes.");
            }

            var address = new IPAddress(ReadBytes(IPv6Length));

            return address;
        }

        public DnsName ReadName()
        {
            var bytesRead = 0;
            var name = DnsName.FromBytes(new ArraySegment<byte>(_data, _index, _data.Length - _index), out bytesRead);
            _index += bytesRead;
            return name;
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