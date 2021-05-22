using System;
using System.Globalization;
using System.Linq;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace DnsClient
{
#if !NETSTANDARD1_3
    [Serializable]
#endif

    public class DnsResponseParseException : Exception

    {
        public byte[] ResponseData { get; }

        public int Index { get; }

        public int ReadLength { get; }

        public DnsResponseParseException()
        {
        }

        public DnsResponseParseException(string message)
            : base(message)
        {
        }

        public DnsResponseParseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public DnsResponseParseException(string message, byte[] data, int index = 0, int length = 0, Exception innerException = null)
            : this(s_defaultMessage(data.Length, index, length, message, FormatData(data, index, length)), innerException)
        {
            ResponseData = data ?? throw new ArgumentNullException(nameof(data));
            Index = index;
            ReadLength = length;
        }

        // Formats the data array to not spam too much data into the exception but
        // at least have all bytes from the beginning to the position around where it failed.
        private static string FormatData(byte[] data, int index, int length)
        {
            if (data == null || data.Length == 0)
            {
                return string.Empty;
            }

            return Convert.ToBase64String(data.Take(index + length + 100).ToArray());
        }

        private static readonly Func<int, int, int, string, string, string> s_defaultMessage = (dataLength, index, length, message, dataDump)
            => string.Format(
                CultureInfo.InvariantCulture,
                "Response parser error, {1} bytes available, tried to read {2} bytes at index {3}.{0}{4}{0}[{5}].",
                Environment.NewLine,
                dataLength,
                length,
                index,
                message,
                dataDump);
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
