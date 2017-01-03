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
        private string _string = null;

        /// <summary>
        /// Creates an empty <see cref="DnsName"/> instance.
        /// </summary>
        public DnsName()
        {
            AddRootLabel();
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

            if (name.Equals(".", StringComparison.Ordinal))
            {
                AddRootLabel();
            }
            else if (name.Length > 0)
            {
                Parse(name.ToCharArray());
            }

            if (!HasRootLabel) AddRootLabel();
        }

        private void AddRootLabel()
        {
            AddLabel(0, "");
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
                var labelBytes = Encoding.UTF8.GetBytes(label);
                Array.ConstrainedCopy(labelBytes, 0, bytes, offset, len);

                offset += len;
            }

            return bytes;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public string ToString(bool utf8)
        {
            if (!utf8)
            {
                return ToString();
            }

            return string.Join(".", _labels);
        }

        public override string ToString()
        {
            if (_string != null)
            {
                return _string;
            }

            var buf = new StringBuilder();
            foreach (var label in _labels)
            {
                if (buf.Length > 0 || label.Length == 0)
                {
                    buf.Append('.');
                }

                Escaped(buf, label);
            }

            return _string = buf.ToString();
        }

        private void Escaped(StringBuilder buf, string label)
        {
            var bytes = Encoding.UTF8.GetBytes(label);
            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                char c = (char)b;
                if ((b < 32) || (b > 126))
                {
                    buf.Append("\\" + ((int)b).ToString("000"));
                    continue;
                }
                else if (c == '.' || c == '"')
                {
                    if ((char)bytes[i - 1] != '\\') buf.Append('\\');
                }
                else if (c == '\\')
                {
                    var next = (char)bytes[i + 1];
                    if (next != '.' && next != '"') buf.Append('\\');
                }

                buf.Append(c);
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

                var label = Encoding.UTF8.GetString(data, offset, length);
                result.AddLabel(1, UnescapeBytes(label.ToArray()));
                offset += length;
            }

            return result;
        }

        private static string UnescapeBytes(char[] label)
        {
            var buf = new StringBuilder();
            for (int i = 0; i < label.Length; i++)
            {
                char c = label[i];
                if (c == '\\')
                {
                    buf.Append(c);
                    c = (char)ReadByte(label, ref i);
                }

                buf.Append(c);
            }

            return buf.ToString();
        }

        private void Parse(char[] domainName)
        {
            var label = new List<byte>();

            for (int index = 0; index < domainName.Length; index++)
            {
                byte b;
                var c = domainName[index];

                if (c == '\\')
                {
                    label.Add((byte)c);
                    //index++;
                    b = ReadByte(domainName, ref index);
                    label.Add(b);
                }
                else if (c != '.')
                {
                    label.Add((byte)c);
                }
                else
                {
                    AddLabel(0, Encoding.UTF8.GetString(label.ToArray()));
                    label.Clear();
                }
            }

            if (label.Count > 0)
            {
                AddLabel(0, Encoding.UTF8.GetString(label.ToArray()));
            }
        }

        public static byte ReadByte(char[] value, ref int index)
        {
            char c1 = value[++index];
            if (c1.IsDigit())
            {
                // sequence is `\DDD'
                char c2 = value[++index];
                char c3 = value[++index];
                if (c2.IsDigit() && c3.IsDigit())
                {
                    if (value[index + 1].IsDigit())
                    {
                        throw new InvalidOperationException("Double byte unicode characters are not supported.");
                    }

                    int val = ((c1 - '0') * 100 + (c2 - '0') * 10 + (c3 - '0'));
                    if (val > byte.MaxValue)
                    {
                        throw new InvalidOperationException("Double byte unicode characters are not supported");
                    }

                    return checked((byte)val);
                }
                else
                {
                    throw new ArgumentException("Invalid escape sequence.", nameof(value));
                }
            }
            else
            {
                return (byte)c1;
            }
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

            // http://www.freesoft.org/CIE/RFC/1035/9.htm
            // dns name limits are 63octets per label
            // (63 letters).(63 letters).(63 letters).(62 letters)
            if (label.Length > 63)
            {
                throw new InvalidOperationException($"Label exceeds 63 octets: '{label}'.");
            }

            _labels.Insert(i, label);
            _string = null;
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
    }

    internal static class CharExtentions
    {
        public static bool IsDigit(this char c)
        {
            return (c >= '0' && c <= '9');
        }
    }
}