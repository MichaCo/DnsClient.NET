////using System;
////using System.Collections.Generic;
////using System.Globalization;
////using System.Linq;
////using System.Text;

////namespace DnsClient
////{
////    /// <summary>
////    /// Represents a host name.
////    /// The implementation is eventually not thread safe. Don't concat or modify the name instance concurrently!
////    /// </summary>
////    public class DnsName : IComparable, IComparable<DnsName>, IComparable<string>
////    {
////        /// <summary>
////        /// Domain root label '.'.
////        /// </summary>
////        public static readonly DnsName Root = new DnsName(DnsNameLabel.Root);

////        /// <summary>
////        /// The ASCII Compatible Encoding, used to identify punycode encoded labels.
////        /// </summary>
////        public const string ACEPrefix = "xn--";

////        private static readonly byte[] ACEPrefixBytes = ACEPrefix.Select(p => (byte)p).ToArray();

////        private const char Dot = '.';
////        private const string DotStr = ".";
////        private const byte DotByte = 46;
////        private const byte BackslashByte = 92;

////        private static readonly IdnMapping _idn = new IdnMapping() { UseStd3AsciiRules = true };
////        private const byte ReferenceByte = 0xc0;

////        private readonly DnsNameLabel[] _labels;
////        private readonly int _octets = 1;

////        // tostring cache
////        private string _string = null;

////        private string _stringUnescaped = null;

////        private DnsName(params DnsNameLabel[] labels)
////        {
////            _labels = ValidateLabels(labels, out _octets);
////        }

////        private DnsName(ICollection<DnsNameLabel> labels)
////        {
////            _labels = ValidateLabels(labels, out _octets);
////        }

////        /// <summary>
////        /// Initializes a new instance of <see cref="DnsName"/> by parsing the <paramref name="name"/>.
////        /// </summary>
////        /// <param name="name">The input name.</param>
////        public DnsName(string name)
////        {
////            OriginalString = name;
////            _labels = ValidateLabels(ParseInternal(name), out _octets);
////        }

////        public string OriginalString { get; }

////        public bool IsEmpty => Size == 0;

////        public bool IsHostName => !_labels.Any(p => !p.IsHostNameLabel());

////        public int Octets => _octets;

////        public int Size => _labels.Length - 1;

////        public string Value => ToString(false);

////        public string ValueUTF8 => ToString(true);

////        public string this[int index]
////        {
////            get
////            {
////                // exclude ""
////                if (index < 0 || index >= Size)
////                {
////                    throw new ArgumentOutOfRangeException(nameof(index));
////                }

////                return _labels[index].ToString();
////            }
////        }

////        public static implicit operator DnsName(string name) => new DnsName(name);

////        public static implicit operator string(DnsName name) => name.Value;

////        //public static implicit operator DnsName(QueryName name) => new DnsName(name.Name);

////        //public static implicit operator QueryName(DnsName name) => new QueryName(name.Value);

////        public static DnsName ParsePuny(string unicodeName)
////        {
////            return _idn.GetAscii(unicodeName);
////        }

////        public static DnsName ParsePuny(string unicodeName, int index)
////        {
////            return _idn.GetAscii(unicodeName, index);
////        }

////        public static DnsName ParsePuny(string unicodeName, int index, int count)
////        {
////            return _idn.GetAscii(unicodeName, index, count);
////        }

////        public int CompareTo(object other)
////        {
////            if (other == null) return 1;
////            return CompareTo(other as string);
////        }

////        public int CompareTo(DnsName other)
////        {
////            if (other == null)
////            {
////                return 1;
////            }

////            return ToString().CompareTo(other.ToString());
////        }

////        public int CompareTo(string other)
////        {
////            if (other == null) return 1;
////            return CompareTo((DnsName)other);
////        }

////        public DnsName Concat(DnsName other)
////        {
////            if (other == null)
////            {
////                throw new ArgumentNullException(nameof(other));
////            }

////            var result = new DnsNameLabel[_labels.Length - 1 + other._labels.Length];
////            Array.Copy(_labels, 0, result, 0, _labels.Length - 1);
////            Array.Copy(other._labels, 0, result, _labels.Length - 1, other._labels.Length);

////            return new DnsName(result);
////        }

////        public override string ToString() => ToString(false);

////        public string ToString(bool unescaped)
////        {
////            if (unescaped)
////            {
////                if (_stringUnescaped == null)
////                {
////                    _stringUnescaped = string.Join(DotStr, _labels.Select(p => p.ToUnescapedString()).ToArray(), 0, _labels.Length - 1) + DotStr;
////                }

////                return _stringUnescaped;
////            }

////            if (_string == null)
////            {
////                _string = string.Join(DotStr, _labels.Select(p => p.ToString()).ToArray(), 0, _labels.Length - 1) + DotStr;
////            }

////            return _string;
////        }

////        public static DnsName Parse(string name)
////        {
////            return new DnsName(name);
////        }

////        private static ICollection<DnsNameLabel> ParseInternal(string name)
////        {
////            if (name == null)
////            {
////                throw new ArgumentNullException(nameof(name));
////            }
////            if (name.Length > 1 && name[0] == Dot)
////            {
////                throw new ArgumentException($"'{name}' is not a legal name, found empty label.", nameof(name));
////            }

////            if (name.Length == 0 || (name.Length == 1 && name[0] == Dot))
////            {
////                return new DnsNameLabel[] { DnsNameLabel.Root };
////            }

////            var actualLength = Encoding.UTF8.GetByteCount(name);

////            var result = new List<DnsNameLabel>();
////            using (var bytes = new PooledBytes(actualLength))
////            {
////                int offset = 0;
////                int count = 0;
////                byte lastByte = 0;
////                Encoding.UTF8.GetBytes(name, 0, name.Length, bytes.Buffer, 0);

////                for (int index = 0; index < actualLength; index++)
////                {
////                    byte b = bytes.Buffer[index];
////                    count++;

////                    if (b == DotByte && lastByte != BackslashByte)
////                    {
////                        result.Add(ParseLabel(new ArraySegment<byte>(bytes.Buffer, offset, count - 1)));
////                        offset += count;
////                        count = 0;
////                    }

////                    lastByte = b;
////                }
////                if (count > 0)
////                {
////                    result.Add(ParseLabel(new ArraySegment<byte>(bytes.Buffer, offset, count)));
////                }
////            }

////            return result;
////        }

////        internal static DnsName FromBytes(ICollection<ArraySegment<byte>> labels)
////        {
////            if (labels.Count == 0)
////            {
////                return Root;
////            }

////            var result = new List<DnsNameLabel>();

////            foreach (var label in labels)
////            {
////                result.Add(ParseLabel(label));
////            }

////            return new DnsName(result);
////        }

////        private static DnsNameLabel ParseLabel(ArraySegment<byte> bytes)
////        {
////            if (bytes.Count == 0)
////            {
////                return DnsNameLabel.Root;
////            }

////            byte current = bytes.Array[bytes.Offset];

////            if (bytes.Count >= 4)
////            {
////                // check ACE prefix
////                byte a = bytes.Array[bytes.Offset + 1], b = bytes.Array[bytes.Offset + 2], c = bytes.Array[bytes.Offset + 3];

////                if (current == ACEPrefixBytes[0] && a == ACEPrefixBytes[1] && b == ACEPrefixBytes[2] && c == ACEPrefixBytes[3])
////                {
////                    var stringRep = Encoding.ASCII.GetString(bytes.ToArray());
////                    try
////                    {
////                        var unicode = _idn.GetUnicode(stringRep);
////                        return new DnsNameLabel(stringRep, unicode, bytes.Count);
////                    }
////                    catch
////                    {
////                        throw new InvalidOperationException("Found lable with ACE-prefix, but the name is invalid and cannot be parsed.");
////                    }
////                }
////            }

////            var unescapedBytes = new List<byte>(66);
////            var escapedBytes = new List<byte>(66);
////            for (var index = 0; index < bytes.Count; index++)
////            {
////                current = bytes.Array[bytes.Offset + index];

////                if (current == BackslashByte)
////                {
////                    // eof check
////                    if (index == bytes.Count - 1)
////                    {
////                        // escape backslash
////                        unescapedBytes.Add(BackslashByte);

////                        // escape the escape
////                        escapedBytes.Add(BackslashByte);
////                        escapedBytes.Add(BackslashByte);
////                        continue;
////                    }

////                    // escape sequence started
////                    // continue
////                    current = bytes.Array[bytes.Offset + ++index];

////                    // determine if we found escaped digitsin \DDD format
////                    if (current.IsDigit() && bytes.Count > index + 2)
////                    {
////                        var a = bytes.Array[bytes.Offset + index + 1];
////                        var b = bytes.Array[bytes.Offset + index + 2];
////                        if (a.IsDigit() && b.IsDigit())
////                        {
////                            unescapedBytes.Add(Convert.ToByte("" + (char)current + (char)a + (char)b));

////                            escapedBytes.Add(BackslashByte);
////                            escapedBytes.Add(current);
////                            escapedBytes.Add(a);
////                            escapedBytes.Add(b);

////                            index += 2;
////                        }
////                        else
////                        {
////                            // escape backslash
////                            unescapedBytes.Add(BackslashByte);

////                            // escape the escape
////                            escapedBytes.Add(BackslashByte);
////                            escapedBytes.Add(BackslashByte);
////                            // and parse current again
////                            index--;
////                            continue;
////                        }
////                    }
////                    else if (current == (byte)'.' || current == (byte)';' || current == BackslashByte)
////                    {
////                        // calid escape

////                        unescapedBytes.Add(BackslashByte);
////                        escapedBytes.Add(BackslashByte);
////                        unescapedBytes.Add(current);
////                        escapedBytes.Add(current);
////                    }
////                    else
////                    {
////                        // escape backslash
////                        unescapedBytes.Add(BackslashByte);

////                        // escape the escape
////                        escapedBytes.Add(BackslashByte);
////                        escapedBytes.Add(BackslashByte);
////                        // and parse current again
////                        index--;
////                        continue;
////                    }
////                }
////                else if (current <= 33 || current >= 126)
////                {
////                    unescapedBytes.Add(current);

////                    // non supported ASCI char
////                    // escape it \000
////                    escapedBytes.Add(BackslashByte);
////                    var byteOfByte = ExctractBytesToEscape(current);
////                    escapedBytes.Add(byteOfByte[0]);
////                    escapedBytes.Add(byteOfByte[1]);
////                    escapedBytes.Add(byteOfByte[2]);
////                }
////                else
////                {
////                    if (current == (byte)';')
////                    {
////                        unescapedBytes.Add(BackslashByte);
////                        escapedBytes.Add(BackslashByte);
////                    }

////                    unescapedBytes.Add(current);
////                    escapedBytes.Add(current);
////                }
////            }

////            return new DnsNameLabel(escapedBytes.ToArray(), unescapedBytes.ToArray());
////        }

////        private static byte[] ExctractBytesToEscape(int current)
////        {
////            int a = 0, b = 0, c = 0;
////            int p;
////            a = current / 100;
////            p = current % 100;
////            b = a > 0 ? p / 10 : current / 10;
////            c = current % 10;

////            //  ASCI number 0 baseline => 48
////            return new byte[] { (byte)(a + 48), (byte)(b + 48), (byte)(c + 48) };
////        }

////        internal void WriteBytes(DnsDatagramWriter writer)
////        {
////            foreach (var label in _labels)
////            {
////                if (label.IsRoot)
////                {
////                    writer.WriteByte(0);
////                    break;
////                }

////                // should never cause issues as each label's length is limited to 64 chars.
////                var len = checked((byte)label.OctetLength);

////                // set the label length byte
////                writer.WriteByte(len);

////                writer.WriteBytes((byte[])label.GetBytes(), len);
////            }
////        }

////        public byte[] GetBytes()
////        {
////            using (var writer = new DnsDatagramWriter())
////            {
////                WriteBytes(writer);
////                return writer.Data.ToArray();
////            }
////        }

////        public override bool Equals(object obj)
////        {
////            if (obj == null)
////            {
////                return false;
////            }

////            return obj.ToString().Equals(Value);
////        }

////        public override int GetHashCode()
////        {
////            return ToString().GetHashCode();
////        }

////        // stays as it is
////        private DnsNameLabel[] ValidateLabels(ICollection<DnsNameLabel> labels, out int octets)
////        {
////            octets = 0;
////            var result = new List<DnsNameLabel>();

////            for (var index = 0; index < labels.Count; index++)
////            {
////                var label = labels.ElementAt(index);
////                if (label == null)
////                {
////                    throw new ArgumentNullException(nameof(label));
////                }

////                // http://www.freesoft.org/CIE/RFC/1035/9.htm
////                // dns name limits are 63octets per label
////                // (63 letters).(63 letters).(63 letters).(62 letters)
////                if (label.OctetLength > 63)
////                {
////                    throw new InvalidOperationException($"Label exceeds 63 octets: '{label}'.");
////                }

////                // Check for empty labels: we want to have only one, and only at end.
////                int len = label.OctetLength;
////                if (len == 0)
////                {
////                    if (index != labels.Count - 1)
////                    {
////                        // not the end
////                        throw new InvalidOperationException("Only one root label is allowed at the end.");
////                    }

////                    // adding root at the end anyways
////                    break;
////                }

////                // Total length must not be larger than 255 characters (including the ending zero).
////                if (len > 0)
////                {
////                    if (octets + len + 1 >= 256)
////                    {
////                        throw new InvalidOperationException("Name too long");
////                    }

////                    octets += (short)(len + 1);
////                }

////                result.Add(label);
////            }

////            result.Add(DnsNameLabel.Root);
////            octets++;

////            return result.ToArray();
////        }

////        internal class DnsNameLabel
////        {
////            private const byte Dash = 45;
////            private const byte a = 97;
////            private const byte z = 122;
////            private const byte A = 65;
////            private const byte Z = 90;
////            private const byte Zero = 48;
////            private const byte Nine = 57;
////            public static readonly DnsNameLabel Root = new DnsNameLabel(new byte[0], new byte[0]);

////            private readonly ICollection<byte> _escapedBytes;
////            private readonly ICollection<byte> _unescapedBytes;
////            private string _toString = null;
////            private string _toStringUnescaped = null;

////            public DnsNameLabel(ICollection<byte> escapedBytes, ICollection<byte> unescapedBytes)
////            {
////                OctetLength = escapedBytes.Count;
////                _escapedBytes = escapedBytes;
////                _unescapedBytes = unescapedBytes;
////                IsRoot = OctetLength == 0;
////            }

////            public DnsNameLabel(string asci, string unicode, int byteLength)
////            {
////                if (string.IsNullOrWhiteSpace(asci)) throw new ArgumentNullException(nameof(asci));
////                if (string.IsNullOrWhiteSpace(unicode)) throw new ArgumentNullException(nameof(unicode));
////                _toString = asci;
////                _toStringUnescaped = unicode;
////                _escapedBytes = Encoding.ASCII.GetBytes(_toString);
////                OctetLength = byteLength;
////            }

////            /// <summary>
////            /// The actual number of bytes. Can be used to validate the length of the label.
////            /// </summary>
////            public int OctetLength { get; }

////            public bool IsRoot { get; }

////            public ICollection<byte> GetBytes()
////            {
////                return _escapedBytes;
////            }

////            public string ToUnescapedString()
////            {
////                if (_toStringUnescaped == null)
////                {
////                    _toStringUnescaped = Encoding.UTF8.GetString(_unescapedBytes.ToArray(), 0, _unescapedBytes.Count);
////                }

////                return _toStringUnescaped;
////            }

////            public override string ToString()
////            {
////                if (_toString == null)
////                {
////                    _toString = Encoding.ASCII.GetString(_escapedBytes.ToArray(), 0, _escapedBytes.Count);
////                }

////                return _toString;
////            }

////            public bool IsHostNameLabel()
////            {
////                if (IsRoot) return true;
////                var bytes = (byte[])_escapedBytes;
////                for (int i = 0; i < bytes.Length; i++)
////                {
////                    byte b = bytes[i];
////                    if (!IsHostNameByte(b))
////                    {
////                        return false;
////                    }
////                }

////                return bytes[0] != Dash && bytes[bytes.Length - 1] != Dash;
////            }

////            public static bool IsHostNameByte(byte b)
////            {
////                return (b == Dash ||
////                        b >= a && b <= z ||
////                        b >= A && b <= Z ||
////                        b >= Zero && b <= Nine);
////            }
////        }
////    }

////    internal static class DnsNameExtentions
////    {
////        public static bool IsDigit(this char c)
////        {
////            return (c >= '0' && c <= '9');
////        }

////        public static bool IsDigit(this byte c)
////        {
////            return (c >= 48 && c <= 57);
////        }
////    }
////}