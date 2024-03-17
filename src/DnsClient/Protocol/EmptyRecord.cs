﻿// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

namespace DnsClient.Protocol
{
    /// <summary>
    /// A <see cref="DnsResourceRecord"/> not representing any specific resource record.
    /// Used if unsupported <see cref="ResourceRecordType"/>s are found in the result.
    /// </summary>
    /// <seealso cref="DnsResourceRecord" />
    public class EmptyRecord : DnsResourceRecord
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmptyRecord"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <exception cref="System.ArgumentNullException">If <paramref name="info"/> is null.</exception>
        public EmptyRecord(ResourceRecordInfo info) : base(info)
        {
        }

        private protected override string RecordToString()
        {
            return string.Empty;
        }
    }
}