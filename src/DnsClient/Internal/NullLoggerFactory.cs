// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;

namespace DnsClient.Internal
{
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
