// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Diagnostics;
using System.Globalization;
using DnsClient.Internal;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace DnsClient
{
#pragma warning disable CA1724
    // TODO: re-name in 2.0, or maybe get rid of?
    public static class Tracing
#pragma warning restore CA1724
    {
        public static TraceSource Source { get; } = new TraceSource("DnsClient", SourceLevels.Error);

        // Logger factory which creates a logger writing to the TraceSource above.
        internal class TraceLoggerFactory : ILoggerFactory
        {
            public ILogger CreateLogger(string categoryName)
            {
                return new TraceLogger(categoryName);
            }

            private class TraceLogger : ILogger
            {
                private readonly string _name;

                public TraceLogger(string name)
                {
                    _name = name ?? throw new ArgumentNullException(nameof(name));
                }

                public bool IsEnabled(LogLevel logLevel)
                {
                    return Source.Switch.ShouldTrace(GetTraceEventType(logLevel));
                }

                public void Log(LogLevel logLevel, int eventId, Exception exception, string message, params object[] args)
                {
                    var result = $"[{_name}] ";
                    if (message != null)
                    {
                        result += string.Format(CultureInfo.InvariantCulture, message, args);
                    }

                    if (exception != null)
                    {
                        result += Environment.NewLine + exception;
                    }

                    Source.TraceEvent(GetTraceEventType(logLevel), eventId, result);
                }

                private static TraceEventType GetTraceEventType(LogLevel logLevel)
                {
                    switch (logLevel)
                    {
                        case LogLevel.Critical:
                            return TraceEventType.Critical;

                        case LogLevel.Error:
                            return TraceEventType.Error;

                        case LogLevel.Warning:
                            return TraceEventType.Warning;

                        case LogLevel.Information:
                            return TraceEventType.Information;

                        case LogLevel.Debug:
                        case LogLevel.Trace:
                            return TraceEventType.Verbose;
                    }

                    return 0;
                }
            }
        }
    }

    public static class Logging
    {
        /// <summary>
        /// Gets or sets the <see cref="ILoggerFactory"/> DnsClient should use.
        /// Per default it will log to <see cref="Tracing.Source"/>.
        /// </summary>
        public static ILoggerFactory LoggerFactory { get; set; } = new Tracing.TraceLoggerFactory();
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
