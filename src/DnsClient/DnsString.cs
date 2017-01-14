using System;
using System.Globalization;
using System.Linq;

namespace DnsClient
{
    public class DnsString
    {
        public const string ACEPrefix = "xn--";
        public const int LabelMaxLength = 63;
        public const int QueryMaxLength = 255;
        public static readonly DnsString RootLabel = new DnsString(".", ".");
        internal static readonly IdnMapping IDN = new IdnMapping() { UseStd3AsciiRules = true };
        private const char Dot = '.';
        private const string DotStr = ".";

        private string[] _labels = new string[0];

        public string Original { get; }

        public string Value { get; }

        internal DnsString(string original, string value)
        {
            Original = original;
            Value = value;
        }

        public static implicit operator string(DnsString name) => name.Value;

        public override string ToString()
        {
            return Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            return obj.ToString().Equals(Value);
        }

        public static DnsString ParseQueryString(string query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            int charCount = 0;
            int labelCharCount = 0;
            int labelsCount = 0;

            if (query.Length > 1 && query[0] == Dot)
            {
                throw new ArgumentException($"'{query}' is not a legal name, found leading root label.", nameof(query));
            }

            if (query.Length == 0 || (query.Length == 1 && query.Equals(DotStr)))
            {
                return RootLabel;
            }

            for (int index = 0; index < query.Length; index++)
            {
                var c = query[index];
                if (c == Dot)
                {
                    if (labelCharCount > LabelMaxLength)
                    {
                        throw new ArgumentException($"Label '{labelsCount + 1}' is longer than {LabelMaxLength} bytes.", nameof(query));
                    }

                    labelsCount++;
                    labelCharCount = 0;
                }
                else
                {
                    labelCharCount++;
                    charCount++;
                    if (!(c == '-' || c == '_' ||
                        c >= 'a' && c <= 'z' ||
                        c >= 'A' && c <= 'Z' ||
                        c >= '0' && c <= '9'))
                    {
                        try
                        {
                            var result = IDN.GetAscii(query);
                            if (result[result.Length - 1] != Dot)
                            {
                                result += Dot;
                            }

                            return new DnsString(query, result);
                        }
                        catch (Exception ex)
                        {
                            throw new ArgumentException($"'{query}' is not a valid host name.", nameof(query), ex);
                        }
                    }
                }
            }

            // check rest
            if (labelCharCount > 0)
            {
                labelsCount++;

                // check again label max length
                if (labelCharCount > LabelMaxLength)
                {
                    throw new ArgumentException($"Label '{labelsCount}' is longer than {LabelMaxLength} bytes.", nameof(query));
                }
            }

            // octets length length bit per label + 2(start +end)
            if (charCount + labelsCount + 1 > QueryMaxLength)
            {
                throw new ArgumentException($"Octet length of '{query}' exceeds maximum of {QueryMaxLength} bytes.", nameof(query));
            }

            if (query[query.Length - 1] != Dot)
            {
                return new DnsString(query, query + Dot);
            }

            return new DnsString(query, query);
        }

        public static DnsString FromResponseQueryString(string query)
        {
            if (query.Length == 0 || query[query.Length - 1] != Dot)
            {
                query += DotStr;
            }

            if (query.Contains(ACEPrefix))
            {
                var unicode = IDN.GetUnicode(query);
                return new DnsString(unicode, query);
            }

            return new DnsString(query, query);
        }
    }
}