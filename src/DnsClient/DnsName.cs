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
        public static readonly DnsName Root = new DnsName(Dot);

        /// <summary>
        /// The ASCII Compatible Encoding, used to identify punycode encoded labels.
        /// </summary>
        public const string ACEPrefix = "xn--";

        private static readonly byte[] ACEPrefixBytes = ACEPrefix.Select(p => (byte)p).ToArray();

        internal const string Dot = ".";
        internal const byte DotByte = 46;
        internal const byte BackslashByte = 92;

        private static readonly IdnMapping _idn = new IdnMapping() { UseStd3AsciiRules = true };
        private const byte ReferenceByte = 0xc0;
        private List<DnsNameLabel> _labels = new List<DnsNameLabel>(10);
        private short _octets = 1;
        private string _string = null;
        private string _stringUnescaped = null;
        private bool _hasRoot = false;

        /// <summary>
        /// Creates an empty <see cref="DnsName"/> instance.
        /// </summary>
        private DnsName()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DnsName"/> by parsing the <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The input name.</param>
        public DnsName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Length > 1 && name.StartsWith(Dot))
            {
                throw new ArgumentException($"'{name}' is not a legal name, found empty label.", nameof(name));
            }

            if (name.Length > 0 && !name.Equals(".", StringComparison.Ordinal))
            {
                Parse(name);
            }

            if (!_hasRoot)
            {
                AddLabel(DnsNameLabel.Root);
            }
        }

        public bool IsEmpty => Size == 0;

        public bool IsHostName => !_labels.Any(p => !p.IsHostNameLabel());

        public int Octets => _octets;

        public int Size => _labels.Count - 1;

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

            var result = new DnsName();
            foreach (var label in _labels.Where(p => !p.IsRoot))
            {
                result.AddLabel(label);
            }
            foreach (var label in other._labels)
            {
                result.AddLabel(label);
            }

            if (!result._hasRoot)
            {
                result.AddLabel(DnsNameLabel.Root);
            }

            return result;
        }

        public string ToString(bool unescaped)
        {
            if (_labels.Count == 1)
            {
                return Dot;
            }
            if (unescaped)
            {
                if (_stringUnescaped == null)
                {
                    _stringUnescaped = string.Join(Dot, _labels.Select(p => p.ToUnescapedString()));
                }

                return _stringUnescaped;
            }

            if (_string == null)
            {
                _string = string.Join(Dot, _labels);
            }

            return _string;
        }

        private void Parse(string name)
        {
            var actualLength = Encoding.UTF8.GetByteCount(name);

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
                        AddLabel(ParseLabel(new ArraySegment<byte>(bytes.Buffer, offset, count - 1)));
                        offset += count;
                        count = 0;
                    }

                    lastByte = b;
                }
                if (count > 0)
                {
                    AddLabel(ParseLabel(new ArraySegment<byte>(bytes.Buffer, offset, count)));
                }
            }

            if (!_hasRoot)
            {
                AddLabel(DnsNameLabel.Root);
            }
        }

        internal static DnsName FromBytes(ArraySegment<byte> utf8Bytes, out int bytesRead)
        {
            // check utf8Bytes .Offset & .Count; ?

            if (utf8Bytes == null || utf8Bytes.Count == 0)
            {
                throw new ArgumentNullException(nameof(utf8Bytes));
            }

            var result = new DnsName();

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
                    return result.Concat(sub);
                }

                if (offset + length > utf8Bytes.Count - 1)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(utf8Bytes),
                        $"Found invalid label position {offset - 1} or length {length} in the source data.");
                }

                var label = ParseLabel(new ArraySegment<byte>(utf8Bytes.Array, utf8Bytes.Offset + offset, length));

                // maybe store orignial bytes in this instance too?
                result.AddLabel(label);

                offset += length;
            }

            if (!result._hasRoot)
            {
                result.AddLabel(DnsNameLabel.Root);
            }

            bytesRead = offset;
            return result;
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
                    catch { /*do nothing*/ }
                }
            }

            var unescapedBytes = new List<byte>(66);
            var escapedBytes = new List<byte>(66);
            for (var index = 0; index < bytes.Count; index++)
            {
                current = bytes.Array[bytes.Offset + index];

                if (current == BackslashByte)
                {
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
        public ArraySegment<byte> GetBytes()
        {
            var bytes = new byte[_octets];
            var offset = 0;
            foreach (var label in _labels)
            {
                // should never cause issues as each label's length is limited to 64 chars.
                var len = checked((byte)label.OctetLength);

                // set the label length byte
                bytes[offset++] = len;

                // set the label's content
                if (!label.IsRoot)
                {
                    Array.ConstrainedCopy(label.GetBytes(), 0, bytes, offset, len);
                }

                offset += len;
            }

            return new ArraySegment<byte>(bytes, 0, offset);
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

        public override string ToString() => ToString(false);

        // stays as it is
        private void AddLabel(DnsNameLabel label)
        {
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
                if (_hasRoot)
                {
                    throw new InvalidOperationException("Only one root label is allowed at the end.");
                }

                _hasRoot = true;
            }

            // Total length must not be larger than 255 characters (including the ending zero).
            if (len > 0)
            {
                if (_octets + len + 1 >= 256)
                {
                    throw new InvalidOperationException("Name too long");
                }

                _octets += (short)(len + 1);
            }

            _labels.Add(label);

            _string = null;
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
                _toString = asci;
                _toStringUnescaped = unicode;
                OctetLength = byteLength;
            }

            /// <summary>
            /// The actual number of bytes. Can be used to validate the length of the label.
            /// </summary>
            public int OctetLength { get; }

            public bool IsRoot { get; }

            public byte[] GetBytes()
            {
                if (_escapedBytes == null && _toString != null)
                {
                    return Encoding.ASCII.GetBytes(_toString);
                }

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