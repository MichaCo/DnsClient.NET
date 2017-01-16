using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace DnsClient
{
    internal class DnsDatagramReader
    {
        public const int IPv6Length = 16;
        public const int IPv4Length = 4;
        private const byte ReferenceByte = 0xc0;
        private const string ACEPrefix = "xn--";

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

        public string ReadString(int length)
        {
            var result = Encoding.ASCII.GetString(_data.Array, _data.Offset + _index, length);
            _index += length;
            return result;
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

        public static string ReadUTF8String(ArraySegment<byte> data)
        {
            return Encoding.UTF8.GetString(data.Array, data.Offset, data.Count);
        }

        public byte ReadByte()
        {
            if (_data.Count < _index + 1)
            {
                throw new IndexOutOfRangeException($"Cannot read byte {_index + 1}, out of range.");
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

        // needed for IPAddress ctor as it doesn't work with ArraySegment<>
        private byte[] ReadByteArray(int length)
        {
            var result = new byte[length];
            Buffer.BlockCopy(_data.Array, _data.Offset + Index, result, 0, length);
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

            return new IPAddress(ReadByteArray(4));
        }

        public IPAddress ReadIPv6Address()
        {
            if (_data.Count < _index + IPv6Length)
            {
                throw new IndexOutOfRangeException($"Reading IPv6 address, expected {IPv6Length} bytes.");
            }

            return new IPAddress(ReadByteArray(IPv6Length));
        }

        public DnsString ReadDnsName()
        {
            var builder = new StringBuilder();
            var original = new StringBuilder();
            foreach (var labelArray in ReadLabels())
            {
                foreach (var b in labelArray)
                {
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

                builder.Append(".");

                var label = Encoding.UTF8.GetString(labelArray.Array, labelArray.Offset, labelArray.Count);
                if (label.Contains(ACEPrefix))
                {
                    try
                    {
                        label = DnsString.IDN.GetUnicode(label);
                    }
                    catch { /* just do nothing in case the IDN is invalid, better to return something at least */ }
                }

                original.Append(label);
                original.Append(".");
            }

            var value = builder.ToString();
            if (value.Length == 0 || value[value.Length - 1] != '.')
            {
                value += '.';
            }

            var orig = original.ToString();
            if (orig.Length == 0 || orig[orig.Length - 1] != '.')
            {
                orig += '.';
            }

            return new DnsString(orig, value);
        }

        // only used by the DnsQuestion as we don't expect any escaped chars in the actual query posted to and send back from the DNS Server (not supported).
        public DnsString ReadQuestionQueryString()
        {
            var result = new StringBuilder();
            foreach (var labelArray in ReadLabels())
            {
                var label = Encoding.UTF8.GetString(labelArray.Array, labelArray.Offset, labelArray.Count);
                result.Append(label);
                result.Append(".");
            }

            string value = result.ToString();
            return DnsString.FromResponseQueryString(value);
        }

        public ICollection<ArraySegment<byte>> ReadLabels()
        {
            var result = new List<ArraySegment<byte>>();

            // read the length byte for the label, then get the content from offset+1 to length
            // proceed till we reach zero length byte.
            byte length;
            while ((length = ReadByte()) != 0)
            {
                // respect the reference bit and lookup the name at the given position
                // the reader will advance only for the 2 bytes read.
                if ((length & ReferenceByte) != 0)
                {
                    int subIndex = (length & 0x3f) << 8 | ReadByte();
                    if (subIndex >= _data.Array.Length - 1)
                    {
                        // invalid length pointer, seems to be actual length of a label which exceeds 63 chars...
                        // get back one and continue other labels
                        Index--;
                        result.Add(_data.SubArray(Index, length));
                        Index += length;
                        continue;
                    }

                    var subReader = new DnsDatagramReader(_data.SubArrayFromOriginal(subIndex));
                    var newLabels = subReader.ReadLabels();
                    result.AddRange(newLabels); // add range actually much faster than Concat and equal to or faster than foreach.. (array copy would work maybe)
                    return result;
                }

                if (Index + length >= _data.Count)
                {
                    throw new IndexOutOfRangeException(
                        $"Found invalid label position '{Index - 1}' with length '{length}' in the source data.");
                }

                var label = _data.SubArray(Index, length);

                // maybe store orignial bytes in this instance too?
                result.Add(label);

                Index += length;
            }

            return result;
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

            byte a = _data.Array[_data.Offset + _index++];
            byte b = _data.Array[_data.Offset + _index++];
            return (ushort)(a << 8 | b);
        }

        public uint ReadUInt32NetworkOrder()
        {
            return (uint)(ReadUInt16NetworkOrder() << 16 | ReadUInt16NetworkOrder());
        }
    }

    internal static class ArraySegmentExtensions
    {
        public static ArraySegment<T> SubArray<T>(this ArraySegment<T> array, int startIndex, int length)
        {
            return new ArraySegment<T>(array.Array, array.Offset + startIndex, length);
        }

        public static ArraySegment<T> SubArrayFromOriginal<T>(this ArraySegment<T> array, int startIndex)
        {
            return new ArraySegment<T>(array.Array, startIndex, array.Array.Length - startIndex);
        }
    }
}