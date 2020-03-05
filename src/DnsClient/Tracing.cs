#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace DnsClient
{
    using System;
    using System.Diagnostics;
    using DnsClient.Internal;

    public static class Tracing
    {
#if !NETSTANDARD1_3
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
                    this._name = name ?? throw new ArgumentNullException(nameof(name));
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
                        result += string.Format(message, args);
                    }

                    if (exception != null)
                    {
                        result += Environment.NewLine + exception;
                    }

                    Source.TraceEvent(GetTraceEventType(logLevel), eventId, result);
                }

                private LogLevel GetLogLevel(TraceEventType type)
                {
                    switch (type)
                    {
                        case TraceEventType.Critical:
                            return LogLevel.Critical;

                        case TraceEventType.Error:
                            return LogLevel.Error;

                        case TraceEventType.Warning:
                            return LogLevel.Warning;

                        case TraceEventType.Information:
                            return LogLevel.Information;

                        case TraceEventType.Verbose:
                            return LogLevel.Debug;
                    }

                    return LogLevel.None;
                }

                private TraceEventType GetTraceEventType(LogLevel logLevel)
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
#endif
    }

    public static class Logging
    {
#if !NETSTANDARD1_3
        /// <summary>
        /// Gets or sets the <see cref="ILoggerFactory"/> DnsClient should use.
        /// Per default it will log to <see cref="Tracing.Source"/>.
        /// </summary>
        public static ILoggerFactory LoggerFactory { get; set; } = new Tracing.TraceLoggerFactory();
#else

        /// <summary>
        /// Gets or sets the <see cref="ILoggerFactory"/> DnsClient should use.
        /// Per default it will not do anything in netstandard 1.3 targeted projects.
        /// </summary>
        public static ILoggerFactory LoggerFactory { get; set; } = new NullLoggerFactory();

#endif
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member