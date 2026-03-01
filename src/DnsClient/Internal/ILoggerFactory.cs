// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace DnsClient.Internal
{
    public interface ILoggerFactory
    {
        ILogger CreateLogger(string categoryName);
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
