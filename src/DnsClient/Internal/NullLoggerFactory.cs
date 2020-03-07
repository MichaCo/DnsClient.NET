namespace DnsClient.Internal
{
    using System;

    internal class NullLoggerFactory : ILoggerFactory
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new NullLogger();
        }

        private class NullLogger : ILogger
        {
            public bool IsEnabled(LogLevel logLevel)
            {
                return false;
            }

            public void Log(LogLevel logLevel, int eventId, Exception exception, string message, params object[] args)
            {
            }
        }
    }
}