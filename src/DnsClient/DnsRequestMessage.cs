﻿// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;

namespace DnsClient
{
    /// <summary>
    /// Represents a simple request message which can be send through <see cref="DnsMessageHandler"/>.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Request:{Header} => {Question}")]
    internal class DnsRequestMessage
    {
        public DnsRequestHeader Header { get; }

        public DnsQuestion Question { get; }

        public DnsQuerySettings QuerySettings { get; }

        public DnsRequestMessage(DnsRequestHeader header, DnsQuestion question, DnsQuerySettings dnsQuerySettings = null)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            Question = question ?? throw new ArgumentNullException(nameof(question));
            QuerySettings = dnsQuerySettings ?? new DnsQuerySettings(new DnsQueryOptions());
        }

        public override string ToString()
        {
            return $"{Header} => {Question}";
        }
    }
}