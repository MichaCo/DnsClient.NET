// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace DnsClient
{
    [Serializable]
    public class DnsXidMismatchException : Exception
    {
        public int RequestXid { get; }

        public int ResponseXid { get; }

        public DnsXidMismatchException(int requestXid, int responseXid)
            : base()
        {
            RequestXid = requestXid;
            ResponseXid = responseXid;
        }

        public DnsXidMismatchException()
        {
        }

        public DnsXidMismatchException(string message) : base(message)
        {
        }

        public DnsXidMismatchException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
