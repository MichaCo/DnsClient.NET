using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DnsClient
{
    /// <summary>
    /// Represents a host name.
    /// The implementation is eventually not thread safe. Don't concat or modify the name instance concurrently!
    /// </summary>
    public class DnsName : IComparable, IComparable<DnsName>, IComparable<string>
    {
        /// <summary>
        /// Domain root label '.'.
        /// </summary>
        public static readonly DnsName Root = new DnsName(DnsNameLabel.Root);

        /// <summary>
        /// The ASCII Compatible Encoding, used to identify punycode encoded labels.
        /// </summary>
        public const string ACEPrefix = "xn--";

        private static readonly byte[] ACEPrefixBytes = ACEPrefix.Select(p => (byte)p).ToArray();

        private const char Dot = '.';
        private const string DotStr = ".";
        private const byte DotByte = 46;
        private const byte BackslashByte = 92;

        private static readonly IdnMapping _idn = new IdnMapping() { UseStd3AsciiRules = true };
        private const byte ReferenceByte = 0xc0;

        private readonly DnsNameLabel[] _labels;
        private readonly int _octets = 1;

        // tostring cache
        private string _string = null;

        private string _stringUnescaped = null;

        private DnsName(params DnsNameLabel[] labels)
        {
            _labels = ValidateLabels(labels, out _octets);
        }

        private DnsName(ICollection<DnsNameLabel> labels)
        {
            _labels = ValidateLabels(labels, out _octets);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DnsName"/> by parsing the <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The input name.</param>
        public DnsName(string name)
        {
            OriginalString = name;
            _labels = ValidateLabels(ParseInternal(name), out _octets);
        }
        
        public string OriginalString { get; }

        public bool IsEmpty => Size == 0;

        public bool IsHostName => !_labels.Any(p => !p.IsHostNameLabel());

        public int Octets => _octets;

        public int Size => _labels.Length - 1;

        public string Value => ToString(false);

        public string ValueUTF8 => ToString(true);

        public string this[int index]
        {
            get
            {
                // exclude ""
                if (index < 0 || index >= Size)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return _labels[index].ToString();
            }
        }

        public static implicit operator DnsName(string name) => new DnsName(name);

        public static implicit operator string(DnsName name) => name.ToString();

        public static DnsName ParsePuny(string unicodeName)
        {
            return _idn.GetAscii(unicodeName);
        }

        public static DnsName ParsePuny(string unicodeName, int index)
        {
            return _idn.GetAscii(unicodeName, index);
        }

        public static DnsName ParsePuny(string unicodeName, int index, int count)
        {
            return _idn.GetAscii(unicodeName, index, count);
        }

        public int CompareTo(object other)
        {
            if (other == null) return 1;
            return CompareTo(other as string);
        }

        public int CompareTo(DnsName other)
        {
            if (other == null)
            {
                return 1;
            }

            return ToString().CompareTo(other.ToString());
        }

        public int CompareTo(string other)
        {
            if (other == null) return 1;
            return CompareTo((DnsName)other);
        }

        public DnsName Concat(DnsName other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            var result = new List<DnsNameLabel>();
            result.AddRange(_labels.Where(p => !p.IsRoot));
            result.AddRange(other._labels);

            return new DnsName(result.ToArray());
        }

        public override string ToString() => ToString(false);

        public string ToString(bool unescaped)
        {
            if (unescaped)
            {
                if (_stringUnescaped == null)
                {
                    _stringUnescaped = string.Join(DotStr, _labels.Select(p => p.ToUnescapedString()).ToArray(), 0, _labels.Length - 1) + DotStr;
                }

                return _stringUnescaped;
            }

            if (_string == null)
            {
                _string = string.Join(DotStr, _labels.Select(p => p.ToString()).ToArray(), 0, _labels.Length - 1) + DotStr;
            }

            return _string;
        }

        public static DnsName Parse(string name)
        {
            return new DnsName(name);
        }
        
        private static ICollection<DnsNameLabel> ParseInternal(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Length > 1 && name[0] == Dot)
            {
                throw new ArgumentException($"'{name}' is not a legal name, found empty label.", nameof(name));
            }

            if (name.Length == 0 || (name.Length == 1 && name[0] == Dot))
            {
                return new DnsNameLabel[] { DnsNameLabel.Root };
            }

            var actualLength = Encoding.UTF8.GetByteCount(name);

            var result = new List<DnsNameLabel>();
            using (var bytes = new PooledBytes(actualLength))
            {
                int offset = 0;
                int count = 0;
                byte lastByte = 0;
                Encoding.UTF8.GetBytes(name, 0, name.Length, bytes.Buffer, 0);

                for (int index = 0; index < actualLength; index++)
                {
                    byte b = bytes.Buffer[index];
                    count++;

                    if (b == DotByte && lastByte != BackslashByte)
                    {
                        result.Add(ParseLabel(new ArraySegment<byte>(bytes.Buffer, offset, count - 1)));
                        offset += count;
                        count = 0;
                    }

                    lastByte = b;
                }
                if (count > 0)
                {
                    result.Add(ParseLabel(new ArraySegment<byte>(bytes.Buffer, offset, count)));
                }
            }

            return result;
        }

        internal static DnsName FromBytes(ArraySegment<byte> utf8Bytes, out int bytesRead)
        {
            // check utf8Bytes .Offset & .Count; ?

            if (utf8Bytes == null || utf8Bytes.Count == 0)
            {
                throw new ArgumentNullException(nameof(utf8Bytes));
            }

            var result = new List<DnsNameLabel>();

            // read the length byte for the label, then get the content from offset+1 to length
            // proceed till we reach zero length byte.
            byte length;
            int offset = 0;
            while ((length = utf8Bytes.ElementAt(offset++)) != 0)
            {
                // respect the reference bit and lookup the name at the given position
                // the reader will advance only for the 2 bytes read.
                if ((length & ReferenceByte) != 0)
                {
                    int subset = (length & 0x3f) << 8 | utf8Bytes.ElementAt(offset++);
                    var sub = FromBytes(new ArraySegment<byte>(utf8Bytes.Array, subset, utf8Bytes.Array.Length - subset), out subset);
                    bytesRead = offset;
                    return new DnsName(result).Concat(sub);
                }

                if (offset + length > utf8Bytes.Count - 1)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(utf8Bytes),
                        $"Found invalid label position {offset - 1} or length {length} in the source data.");
                }

                var label = ParseLabel(new ArraySegment<byte>(utf8Bytes.Array, utf8Bytes.Offset + offset, length));

                // maybe store orignial bytes in this instance too?
                result.Add(label);

                offset += length;
            }

            bytesRead = offset;
            return new DnsName(result);
        }

        private static DnsNameLabel ParseLabel(ArraySegment<byte> bytes)
        {
            if (bytes.Count == 0)
            {
                return DnsNameLabel.Root;
            }

            byte current = bytes.Array[bytes.Offset];

            if (bytes.Count >= 4)
            {
                // check ACE prefix
                byte a = bytes.Array[bytes.Offset + 1], b = bytes.Array[bytes.Offset + 2], c = bytes.Array[bytes.Offset + 3];

                if (current == ACEPrefixBytes[0] && a == ACEPrefixBytes[1] && b == ACEPrefixBytes[2] && c == ACEPrefixBytes[3])
                {
                    var stringRep = Encoding.ASCII.GetString(bytes.ToArray());
                    try
                    {
                        var unicode = _idn.GetUnicode(stringRep);
                        return new DnsNameLabel(stringRep, unicode, bytes.Count);
                    }
                    catch
                    {
                        throw new InvalidOperationException("Found lable with ACE-prefix, but the name is invalid and cannot be parsed.");
                    }
                }
            }

            var unescapedBytes = new List<byte>(66);
            var escapedBytes = new List<byte>(66);
            for (var index = 0; index < bytes.Count; index++)
            {
                current = bytes.Array[bytes.Offset + index];

                if (current == BackslashByte)
                {
                    // eof check
                    if (index == bytes.Count - 1)
                    {
                        // escape backslash
                        unescapedBytes.Add(BackslashByte);

                        // escape the escape
                        escapedBytes.Add(BackslashByte);
                        escapedBytes.Add(BackslashByte);
                        continue;
                    }

                    // escape sequence started
                    // continue
                    current = bytes.Array[bytes.Offset + ++index];

                    // determine if we found escaped digitsin \DDD format
                    if (current.IsDigit() && bytes.Count > index + 2)
                    {
                        var a = bytes.Array[bytes.Offset + index + 1];
                        var b = bytes.Array[bytes.Offset + index + 2];
                        if (a.IsDigit() && b.IsDigit())
                        {
                            unescapedBytes.Add(Convert.ToByte("" + (char)current + (char)a + (char)b));

                            escapedBytes.Add(BackslashByte);
                            escapedBytes.Add(current);
                            escapedBytes.Add(a);
                            escapedBytes.Add(b);

                            index += 2;
                        }
                        else
                        {
                            // escape backslash
                            unescapedBytes.Add(BackslashByte);

                            // escape the escape
                            escapedBytes.Add(BackslashByte);
                            escapedBytes.Add(BackslashByte);
                            // and parse current again
                            index--;
                            continue;
                        }
                    }
                    else if (current == (byte)'.' || current == (byte)';' || current == BackslashByte)
                    {
                        // calid escape

                        unescapedBytes.Add(BackslashByte);
                        escapedBytes.Add(BackslashByte);
                        unescapedBytes.Add(current);
                        escapedBytes.Add(current);
                    }
                    else
                    {
                        // escape backslash
                        unescapedBytes.Add(BackslashByte);

                        // escape the escape
                        escapedBytes.Add(BackslashByte);
                        escapedBytes.Add(BackslashByte);
                        // and parse current again
                        index--;
                        continue;
                    }
                }
                else if (current <= 33 || current >= 126)
                {
                    unescapedBytes.Add(current);

                    // non supported ASCI char
                    // escape it \000
                    escapedBytes.Add(BackslashByte);
                    var byteOfByte = ExctractBytesToEscape(current);
                    escapedBytes.Add(byteOfByte[0]);
                    escapedBytes.Add(byteOfByte[1]);
                    escapedBytes.Add(byteOfByte[2]);
                }
                else
                {
                    if (current == (byte)';')
                    {
                        unescapedBytes.Add(BackslashByte);
                        escapedBytes.Add(BackslashByte);
                    }

                    unescapedBytes.Add(current);
                    escapedBytes.Add(current);
                }
            }

            return new DnsNameLabel(escapedBytes.ToArray(), unescapedBytes.ToArray());
        }

        private static byte[] ExctractBytesToEscape(int current)
        {
            int a = 0, b = 0, c = 0;
            int p;
            a = current / 100;
            p = current % 100;
            b = a > 0 ? p / 10 : current / 10;
            c = current % 10;

            //  ASCI number 0 baseline => 48
            return new byte[] { (byte)(a + 48), (byte)(b + 48), (byte)(c + 48) };
        }

                //TODO: private optimize? don't allocate?
        internal void WriteBytes(DnsDatagramWriter writer)
        {
            //var bytes = new byte[_octets];
            //var offset = 0;
            foreach (var label in _labels)
            {
                if (label.IsRoot)
                {
                    writer.WriteByte(0);
                    //offset++;
                    //bytes[offset++] = 0;
                    break;
                }

                // should never cause issues as each label's length is limited to 64 chars.
                var len = checked((byte)label.OctetLength);

                // set the label length byte
                writer.WriteByte(len);
                //offset++;
                //bytes[offset++] = len;

                // set the label's content
                writer.WriteBytes(label.GetBytes(), len);
                //Array.ConstrainedCopy(label.GetBytes(), 0, bytes, offset, len);

                //offset += len;
            }
        }

        public byte[] GetBytes()
        {
            using (var writer = new DnsDatagramWriter())
            {
                WriteBytes(writer);
                return writer.Data;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var otherName = obj as DnsName;
            if (otherName == null)
            {
                return false;
            }

            return CompareTo(otherName) == 0;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        // stays as it is
        private DnsNameLabel[] ValidateLabels(ICollection<DnsNameLabel> labels, out int octets)
        {
            octets = 0;
            var result = new List<DnsNameLabel>();

            for (var index = 0; index < labels.Count; index++)
            {
                var label = labels.ElementAt(index);
                if (label == null)
                {
                    throw new ArgumentNullException(nameof(label));
                }

                // http://www.freesoft.org/CIE/RFC/1035/9.htm
                // dns name limits are 63octets per label
                // (63 letters).(63 letters).(63 letters).(62 letters)
                if (label.OctetLength > 63)
                {
                    throw new InvalidOperationException($"Label exceeds 63 octets: '{label}'.");
                }

                // Check for empty labels: we want to have only one, and only at end.
                int len = label.OctetLength;
                if (len == 0)
                {
                    if (index != labels.Count - 1)
                    {
                        // not the end
                        throw new InvalidOperationException("Only one root label is allowed at the end.");
                    }

                    // adding root at the end anyways
                    break;
                }

                // Total length must not be larger than 255 characters (including the ending zero).
                if (len > 0)
                {
                    if (octets + len + 1 >= 256)
                    {
                        throw new InvalidOperationException("Name too long");
                    }

                    octets += (short)(len + 1);
                }

                result.Add(label);
            }

            result.Add(DnsNameLabel.Root);
            octets++;

            return result.ToArray();
        }

        internal class DnsNameLabel
        {
            public static readonly DnsNameLabel Root = new DnsNameLabel(new byte[0], new byte[0]);

            private readonly byte[] _escapedBytes;
            private readonly byte[] _unescapedBytes;
            private string _toString = null;
            private string _toStringUnescaped = null;

            public DnsNameLabel(byte[] escapedBytes, byte[] unescapedBytes)
            {
                OctetLength = escapedBytes.Length;
                _escapedBytes = escapedBytes;
                _unescapedBytes = unescapedBytes;
                IsRoot = OctetLength == 0;
            }

            public DnsNameLabel(string asci, string unicode, int byteLength)
            {
                if (string.IsNullOrWhiteSpace(asci)) throw new ArgumentNullException(nameof(asci));
                if (string.IsNullOrWhiteSpace(unicode)) throw new ArgumentNullException(nameof(unicode));
                _toString = asci;
                _toStringUnescaped = unicode;
                _escapedBytes = Encoding.ASCII.GetBytes(_toString);
                OctetLength = byteLength;
            }

            /// <summary>
            /// The actual number of bytes. Can be used to validate the length of the label.
            /// </summary>
            public int OctetLength { get; }

            public bool IsRoot { get; }

            public byte[] GetBytes()
            {
                return _escapedBytes;
            }

            public string ToUnescapedString()
            {
                if (_toStringUnescaped == null)
                {
                    _toStringUnescaped = Encoding.UTF8.GetString(_unescapedBytes, 0, _unescapedBytes.Length);
                }

                return _toStringUnescaped;
            }

            public override string ToString()
            {
                if (_toString == null)
                {
                    _toString = Encoding.ASCII.GetString(_escapedBytes, 0, _escapedBytes.Length);
                }

                return _toString;
            }
        }
    }

    internal static class DnsNameExtentions
    {
        public static bool IsDigit(this char c)
        {
            return (c >= '0' && c <= '9');
        }

        public static bool IsDigit(this byte c)
        {
            return (c >= 48 && c <= 57);
        }

        public static bool IsHostNameChar(this char c)
        {
            return (c == '-' ||
                    c >= 'a' && c <= 'z' ||
                    c >= 'A' && c <= 'Z' ||
                    c >= '0' && c <= '9');
        }

        public static bool IsHostNameLabel(this DnsName.DnsNameLabel label)
        {
            if (label.IsRoot) return true;
            var str = label.ToString();
            for (int i = 0; i < str.Length; i++)
            {
                char c = str.ElementAt(i);
                if (!c.IsHostNameChar())
                {
                    return false;
                }
            }

            return !(str.StartsWith("-") || str.EndsWith("-"));
        }
    }
}