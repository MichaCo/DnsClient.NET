#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace DnsClient.Internal
{
    public interface ILoggerFactory
    {
        ILogger CreateLogger(string categoryName);
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
