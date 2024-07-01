// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Globalization;

namespace DnsClient
{
    /// <summary>
    /// The <see cref="DnsString"/> type is used to normalize and validate domain names and labels.
    /// </summary>
    public class DnsString
    {
        /// <summary>
        /// The ACE prefix indicates that the domain name label contains not normally supported characters and that the label has been encoded.
        /// </summary>
        public const string ACEPrefix = "xn--";

        /// <summary>
        /// The maximum length in bytes for one label.
        /// </summary>
        public const int LabelMaxLength = 63;

        /// <summary>
        /// The maximum supported total length in bytes for a domain name. The calculation of the actual
        /// bytes this <see cref="DnsString"/> consumes includes all bytes used for to encode it as octet string.
        /// </summary>
        public const int QueryMaxLength = 255;

        /// <summary>
        /// The root label ".".
        /// </summary>
        public static readonly DnsString RootLabel = new DnsString(".", ".");

        internal static readonly IdnMapping s_idn = new IdnMapping();

        internal const char Dot = '.';
        internal const string DotStr = ".";

        /// <summary>
        /// Gets the original value.
        /// </summary>
        public string Original { get; }

        /// <summary>
        /// Gets the validated and eventually modified value.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets the number of labels of this <see cref="DnsString"/> or null if not applicable.
        /// This property is only set if the <see cref="Parse(string)"/> method was used to create this instance.
        /// </summary>
        public int? NumberOfLabels { get; }

        internal DnsString(string original, string value, int? numLabels = null)
        {
            Original = original;
            Value = value;
            NumberOfLabels = numLabels;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="DnsString"/> to <see cref="string"/>.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator string(DnsString name) => name?.Value;

        /// <summary>
        ///
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static DnsString operator +(DnsString a, DnsString b)
        {
            if (a is null)
            {
                throw new ArgumentNullException(nameof(a));
            }

            if (b is null)
            {
                throw new ArgumentNullException(nameof(b));
            }

            var result = a.Value + (b.Value.Length > 1 ? b.Value : string.Empty);
            return new DnsString(result, result);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static DnsString operator +(DnsString a, string b)
        {
            if (a is null)
            {
                throw new ArgumentNullException(nameof(a));
            }

            if (string.IsNullOrWhiteSpace(b))
            {
                throw new ArgumentException($"'{nameof(b)}' cannot be null or empty.", nameof(b));
            }

            b = b[0] == Dot ? b.Substring(1) : b;

            var parsed = Parse(b);
            return a + parsed;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
#if NET6_0_OR_GREATER || NETSTANDARD2_1
            return Value.GetHashCode(StringComparison.Ordinal);
#else
            return Value.GetHashCode();
#endif
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            return obj.ToString().Equals(Value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Parses the given <paramref name="query"/> and validates all labels.
        /// </summary>
        /// <remarks>
        /// An empty string will be interpreted as root label.
        /// </remarks>
        /// <param name="query">A domain name.</param>
        /// <returns>The <see cref="DnsString"/> representing the given <paramref name="query"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        public static DnsString Parse(string query)
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

            if (query.Length == 0 || (query.Length == 1 && query.Equals(DotStr, StringComparison.Ordinal)))
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
                            var result = s_idn.GetAscii(query);
                            if (result[result.Length - 1] != Dot)
                            {
                                result += Dot;
                            }

                            var labels = result.Split(new[] { Dot }, StringSplitOptions.RemoveEmptyEntries);

                            return new DnsString(query, result, labels.Length);
                        }
                        catch (Exception ex)
                        {
                            throw new ArgumentException($"'{query}' is not a valid hostname.", nameof(query), ex);
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

            // octets length bit per label + 2(start +end)
            if (charCount + labelsCount + 1 > QueryMaxLength)
            {
                throw new ArgumentException($"Octet length of '{query}' exceeds maximum of {QueryMaxLength} bytes.", nameof(query));
            }

            if (query[query.Length - 1] != Dot)
            {
                return new DnsString(query, query + Dot, labelsCount);
            }

            return new DnsString(query, query, labelsCount);
        }

        /// <summary>
        /// Transforms names with the <see cref="ACEPrefix"/> to the Unicode variant and adds a trailing '.' at the end if not present.
        /// The original value will be kept in this instance in case it is needed.
        /// </summary>
        /// <remarks>
        /// The method does not parse the domain name unless it contains a <see cref="ACEPrefix"/>.
        /// </remarks>
        /// <param name="query">The value to check.</param>
        /// <returns>The <see cref="DnsString"/> representation.</returns>
        public static DnsString FromResponseQueryString(string query)
        {
            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var data = query;
            if (query.Length == 0 || query[query.Length - 1] != Dot)
            {
                data += DotStr;
            }

#if NET6_0_OR_GREATER || NETSTANDARD2_1
            if (data.Contains(ACEPrefix, StringComparison.Ordinal))
#else
            if (data.Contains(ACEPrefix))
#endif
            {
                var unicode = s_idn.GetUnicode(data);
                return new DnsString(query, unicode);
            }

            return new DnsString(query, data);
        }
    }
}
