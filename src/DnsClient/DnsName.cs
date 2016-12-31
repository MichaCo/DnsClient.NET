using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DnsClient
{
    public class DnsName : IComparable
    {
        public static readonly DnsName Root = new DnsName();

        private const byte ReferenceByte = 0xc0;
        private List<string> _labels = new List<string>();
        private short _octets = 1;

        /// <summary>
        /// Creates an empty <see cref="DnsName"/> instance.
        /// </summary>
        public DnsName()
        {
            AddLabel(0, "");
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

            if (!HasRootLabel) AddLabel(0, "");
        }

        public bool IsEmpty => Size == 0;

        public bool IsHostName => !_labels.Any(p => !IsHostNameLabel(p));

        public int Octets => _octets;

        public int Size => _labels.Count - 1;

        private bool HasRootLabel => (_labels.Count > 0 && GetLabel(0).Equals(""));

        public string this[int index]
        {
            get
            {
                // exclude ""
                if (index < 0 || index >= Size)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return _labels[index];
            }
        }

        public static DnsName FromBytes(byte[] data, ref int offset)
        {
            if (data == null || data.Length == 0 || data.Length < offset + 1)
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
                    var subName = FromBytes(data, ref subset);
                    result.Concat(subName);
                    return result;
                }

                if (offset + length > data.Length - 1)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(data),
                        $"Found invalid label position {offset - 1} or length {length} in the source data.");
                }

                var label = Encoding.ASCII.GetString(data, offset, length);
                result.AddLabel(1, label);
                offset += length;
            }

            return result;
        }

        public static implicit operator DnsName(string name) => new DnsName(name);

        public static implicit operator string(DnsName name) => name.ToString();

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }

            if ((obj as DnsName) == null)
            {
                return 1;
            }

            return ToString().CompareTo(obj.ToString());
        }

        public void Concat(DnsName other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            foreach (var label in other._labels.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                AddLabel(1, label);
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

        public byte[] GetBytes()
        {
            var bytes = new byte[_octets];
            var offset = 0;
            for (int i = _labels.Count - 1; i >= 0; i--)
            {
                var label = GetLabel(i);

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

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
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

            return buf.ToString();
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

        private void AddLabel(int pos, string label)
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

        private string GetLabel(int label)
        {
            if (label < 0 || label >= _labels.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(label));
            }

            return _labels[_labels.Count - label - 1];
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
                    AddLabel(0, label.ToString());
                    label.Clear();
                }
            }

            if (label.Length > 0)
            {
                AddLabel(0, label.ToString());
            }
        }
    }
}