#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace DnsClient.Internal
{
    using System;

    public interface ILogger
    {
        void Log(LogLevel logLevel, int eventId, Exception exception, string message, params object[] args);

        bool IsEnabled(LogLevel logLevel);
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member