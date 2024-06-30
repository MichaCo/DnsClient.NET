﻿// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Globalization;

namespace DnsClient.Protocol
{
    /// <summary>
    /// Base class for all resource records.
    /// </summary>
    /// <seealso cref="ResourceRecordInfo" />
    public abstract class DnsResourceRecord : ResourceRecordInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DnsResourceRecord" /> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="info"/> is null.</exception>
        protected DnsResourceRecord(ResourceRecordInfo info)
            : base(
                  info?.DomainName ?? throw new ArgumentNullException(nameof(info)),
                  info?.RecordType ?? throw new ArgumentNullException(nameof(info)),
                  info?.RecordClass ?? throw new ArgumentNullException(nameof(info)),
                  info?.InitialTimeToLive ?? throw new ArgumentNullException(nameof(info)),
                  info?.RawDataLength ?? throw new ArgumentNullException(nameof(info)))
        {
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return ToString(offset: 0);
        }

        /// <summary>
        /// Same as <c>ToString</c> but offsets the <see cref="ResourceRecordInfo.DomainName"/>
        /// by <paramref name="offset"/>.
        /// Set the offset to -32 for example to make it print nicely in consoles.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns>A string representing this instance.</returns>
        public virtual string ToString(int offset = 0)
        {
            var printTab = offset == 0 ? string.Empty : "\t";
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0," + offset.ToString(CultureInfo.InvariantCulture) + "} {1}{2} {1}{3} {1}{4} {1}{5}",
                DomainName,
                printTab,
                TimeToLive,
                RecordClass,
                RecordType,
                RecordToString());
        }

        /// <summary>
        /// Returns a string representation of the record's value only.
        /// <see cref="ToString(int)"/> uses this to compose the full string value of this instance.
        /// </summary>
        /// <returns>A string representing this record.</returns>
        private protected abstract string RecordToString();
    }

    /// <summary>
    /// The type represents a <see cref="DnsResourceRecord"/>.
    /// </summary>
    public class ResourceRecordInfo
    {
        private readonly int _ticks;

        /// <summary>
        /// The domain name used to query.
        /// </summary>
        public DnsString DomainName { get; }

        /// <summary>
        /// Specifies type of resource record.
        /// </summary>
        public ResourceRecordType RecordType { get; }

        /// <summary>
        /// Specifies type class of resource record, mostly IN but can be CS, CH or HS .
        /// </summary>
        public QueryClass RecordClass { get; }

        /// <summary>
        /// Gets the current time to live value for the record.
        /// </summary>
        public int TimeToLive
        {
            get
            {
                var curTicks = (int)((Environment.TickCount & int.MaxValue) / 1000d);
                if (curTicks < _ticks)
                {
                    return 0;
                }

                var ttl = InitialTimeToLive - (curTicks - _ticks);
                return ttl < 0 ? 0 : ttl;
            }
        }

        /// <summary>
        /// Gets or sets the original time to live returned from the server.
        /// </summary>
        public int InitialTimeToLive { get; internal set; }

        /// <summary>
        /// Gets the number of bytes for this resource record stored in RDATA
        /// </summary>
        public int RawDataLength { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceRecordInfo" /> class.
        /// </summary>
        /// <param name="domainName">The domain name used by the query.</param>
        /// <param name="recordType">Type of the record.</param>
        /// <param name="recordClass">The record class.</param>
        /// <param name="timeToLive">The time to live.</param>
        /// <param name="rawDataLength">Length of the raw data.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="domainName"/> is null.</exception>
        public ResourceRecordInfo(string domainName, ResourceRecordType recordType, QueryClass recordClass, int timeToLive, int rawDataLength)
            : this(DnsString.Parse(domainName), recordType, recordClass, timeToLive, rawDataLength)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceRecordInfo" /> class.
        /// </summary>
        /// <param name="domainName">The <see cref="DnsString" /> used by the query.</param>
        /// <param name="recordType">Type of the record.</param>
        /// <param name="recordClass">The record class.</param>
        /// <param name="timeToLive">The time to live.</param>
        /// <param name="rawDataLength">Length of the raw data.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="domainName" /> is null or empty.</exception>
        public ResourceRecordInfo(DnsString domainName, ResourceRecordType recordType, QueryClass recordClass, int timeToLive, int rawDataLength)
        {
            DomainName = domainName ?? throw new ArgumentNullException(nameof(domainName));
            RecordType = recordType;
            RecordClass = recordClass;
            RawDataLength = rawDataLength;
            InitialTimeToLive = timeToLive;
            _ticks = (int)((Environment.TickCount & int.MaxValue) / 1000d);
        }
    }
}
