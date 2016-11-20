using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DnsClient2
{
    /* Todo: write tests
            var offset = 0;

            var txtName = new DnsName("afk;asddda");

            var nullName = new DnsName();
            var nullStr = nullName.ToString();
            var nullbytes = nullName.ToBytes();

            var dnsNameA = new DnsName("www.google.com");
            var gName = dnsNameA.ToString();
            var gBytes = dnsNameA.ToBytes();

            var longName = new DnsName("12341234123412341234123412341234123412341234.123123123123123123123123123123123123123123123123.123123123123123123");
            var longString = longName.ToString();
            var longBytes = longName.ToBytes();

            var longRevers = DnsName.FromBytes(longBytes, ref offset);

            try
            {
                // invalid length attr
                offset = 0;
                DnsName.FromBytes(new byte[] { 22, 3, 1, 0 }, ref offset);
            }
            catch (Exception e) { }

            try
            {
                // index oor
                offset = 20;
                DnsName.FromBytes(new byte[] { 22, 3, 1, 0 }, ref offset);
            }
            catch (Exception e) { }

            var nameWithEnd = new DnsName("www.gooogle.com.");
            var wName = nameWithEnd.ToString();

            var nameB = new DnsName("lala\\u0\u106fun.fu.com.12344.fun");

            var s = nameB.ToString();

            var isName = nameB.IsHostName;
     * */

    public class DnsName : IComparable
    {
        private const byte ReferenceByte = 0xc0;
        private List<string> _labels = new List<string>();
        private short _octets = 1;

        public bool HasRootLabel => (_labels.Count > 0 && Get(0).Equals(""));

        public bool IsEmpty => Size == 0;

        public bool IsHostName => !_labels.Any(p => !IsHostNameLabel(p));

        public int Size => _labels.Where(p => p != "").Count();

        public int Octets => _octets;

        /// <summary>
        /// Creates an empty <see cref="DnsName"/> instance.
        /// </summary>
        public DnsName()
        {
            Add(0, "");
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

            if (name.Length > 0)
            {
                Parse(name);
            }

            Add(0, "");
        }

        public static DnsName FromBytes(byte[] data, ref int offset)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (offset > data.Length - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            var result = new DnsName();

            // read the length byte for the label, then get the content from offset+1 to length
            // proceed till we reach zero length byte.
            byte length;
            while ((length = data[offset++]) != 0)
            {
                // respect the reference bit and lookup the name at the given position
                // the reader will advance only for the 2 bytes read.
                if ((length & ReferenceByte) != 0)
                {
                    var subset = (length & 0x3f) << 8 | data[offset++];
                    return FromBytes(data, ref subset);
                }

                if (offset + length > data.Length - 1)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(data),
                        $"Found invalid label position {offset - 1} or length {length} in the source data.");
                }

                var label = Encoding.ASCII.GetString(data, offset, length);
                result.Add(1, label);
                offset += length;
            }

            return result;
        }

        public void Add(int pos, string label)
        {
            if (label == null)
            {
                throw new ArgumentNullException(nameof(label));
            }
            if (pos < 0 || pos > _labels.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(pos));
            }
            // Check for empty labels:  may have only one, and only at end.
            int len = label.Length;
            if ((pos > 0 && len == 0) ||
                (pos == 0 && HasRootLabel))
            {
                throw new InvalidOperationException("Empty label must be the last label in a domain name");
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

            int i = _labels.Count - pos;
            VerifyLabel(label);
            _labels.Insert(i, label);
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }

            return ToString().CompareTo(obj.ToString());
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

        public string Get(int pos)
        {
            if (pos < 0 || pos > _labels.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(pos));
            }

            return _labels[_labels.Count - pos - 1];
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public byte[] AsBytes()
        {
            var bytes = new byte[_octets];
            var offset = 0;
            for (int i = _labels.Count - 1; i >= 0; i--)
            {
                var label = Get(i);

                // should never cause issues as each label's length is limited to 64 chars.
                var len = checked((byte)label.Length);

                // set the label length byte
                bytes[offset++] = len;

                // set the label's content
                var labelBytes = Encoding.ASCII.GetBytes(label);
                Array.ConstrainedCopy(labelBytes, 0, bytes, offset, len);

                offset += len;
            }

            return bytes;
        }

        public override string ToString()
        {
            var buf = new StringBuilder();
            foreach (var label in _labels)
            {
                if (buf.Length > 0 || label.Length == 0)
                {
                    buf.Append('.');
                }

                Escaped(buf, label);
            }

            var name = buf.ToString();

            return name;
        }

        private static bool IsHostNameChar(char c)
        {
            return (c == '-' ||
                    c >= 'a' && c <= 'z' ||
                    c >= 'A' && c <= 'Z' ||
                    c >= '0' && c <= '9');
        }

        private static bool IsHostNameLabel(string label)
        {
            for (int i = 0; i < label.Length; i++)
            {
                char c = label.ElementAt(i);
                if (!IsHostNameChar(c))
                {
                    return false;
                }
            }
            return !(label.StartsWith("-") || label.EndsWith("-"));
        }

        private static void VerifyLabel(string label)
        {
            // http://www.freesoft.org/CIE/RFC/1035/9.htm
            // dns name limits are 63octets per label
            // (63 letters).(63 letters).(63 letters).(62 letters)
            if (label.Length > 63)
            {
                throw new InvalidOperationException("Label exceeds 63 octets: " + label);
            }

            // Check for two-byte characters.
            for (int i = 0; i < label.Length; i++)
            {
                char c = label.ElementAt(i);
                if ((c & 0xFF00) != 0)
                {
                    throw new InvalidOperationException("Label has two-byte char: " + label);
                }
            }
        }

        private void Escaped(StringBuilder buf, string label)
        {
            for (int i = 0; i < label.Length; i++)
            {
                char c = label.ElementAt(i);
                if (c == '.' || c == '\\')
                {
                    buf.Append('\\');
                }

                buf.Append(c);
            }
        }

        private char GetEscaped(string domainName, int pos)
        {
            try
            {
                // assert (name.charAt(pos) == '\\');
                char c1 = domainName.ElementAt(++pos);
                if (IsDigit(c1))
                {
                    // sequence is `\DDD'
                    char c2 = domainName.ElementAt(++pos);
                    char c3 = domainName.ElementAt(++pos);
                    if (IsDigit(c2) && IsDigit(c3))
                    {
                        return (char)((c1 - '0') * 100 + (c2 - '0') * 10 + (c3 - '0'));
                    }
                    else
                    {
                        throw new ArgumentException("Invalid escape sequence.", nameof(domainName));
                    }
                }
                else
                {
                    return c1;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ArgumentException("Invalid escape sequence.", nameof(domainName));
            }
        }

        private bool IsDigit(char c)
        {
            return (c >= '0' && c <= '9');
        }

        private void Parse(string domainName)
        {
            var label = new StringBuilder();

            for (int index = 0; index < domainName.Length; index++)
            {
                var c = domainName[index];

                if (c == '\\')
                {
                    c = GetEscaped(domainName, index++);
                    if (IsDigit(domainName[index]))
                    {
                        index += 2;
                    }

                    label.Append(c);
                }
                else if (c != '.')
                {
                    label.Append(c);
                }
                else
                {
                    Add(0, label.ToString());
                    label.Clear();
                }
            }

            if (label.Length > 0)
            {
                Add(0, label.ToString());
            }
        }
    }
}