#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace DnsClient
{
    using System;
    using System.Globalization;

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
            : this(DefaultMessage(data.Length, index, length, message), innerException)
        {
            ResponseData = data ?? throw new ArgumentNullException(nameof(data));
            Index = index;
            ReadLength = length;
        }

        private static readonly Func<int, int, int, string, string> DefaultMessage = (dataLength, index, length, message)
            => string.Format(CultureInfo.InvariantCulture, "Response parser error, {0} bytes available, tried to read {1} bytes at index {2}. {3}", dataLength, length, index, message);
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member