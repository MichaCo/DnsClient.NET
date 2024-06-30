// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DnsClient
{
    internal class ResponseCache
    {
        private static readonly TimeSpan s_infiniteTimeout = Timeout.InfiniteTimeSpan;

        // max is 24 days
        private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

        private static readonly TimeSpan s_defaultFailureTimeout = TimeSpan.FromSeconds(5);

        private static readonly int s_cleanupInterval = (int)TimeSpan.FromMinutes(10).TotalMilliseconds;
        private readonly ConcurrentDictionary<string, ResponseEntry> _cache = new ConcurrentDictionary<string, ResponseEntry>();
        private readonly object _cleanupLock = new object();
        private bool _cleanupRunning;
        private int _lastCleanup;
        private TimeSpan? _minimumTimeout;
        private TimeSpan? _maximumTimeout;
        private TimeSpan _failureEntryTimeout = s_defaultFailureTimeout;

        public int Count => _cache.Count;

        public bool Enabled { get; set; } = true;

        public TimeSpan? MinimumTimout
        {
            get { return _minimumTimeout; }
            set
            {
                if (value.HasValue &&
                    (value < TimeSpan.Zero || value > s_maxTimeout) && value != s_infiniteTimeout)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _minimumTimeout = value;
            }
        }

        public TimeSpan? MaximumTimeout
        {
            get { return _maximumTimeout; }
            set
            {
                if (value.HasValue &&
                    (value < TimeSpan.Zero || value > s_maxTimeout) && value != s_infiniteTimeout)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _maximumTimeout = value;
            }
        }

        public TimeSpan FailureEntryTimeout
        {
            get { return _failureEntryTimeout; }
            set
            {
                if ((value < TimeSpan.Zero || value > s_maxTimeout) && value != s_infiniteTimeout)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _failureEntryTimeout = value;
            }
        }

        public ResponseCache(bool enabled = true, TimeSpan? minimumTimout = null, TimeSpan? maximumTimeout = null, TimeSpan? failureEntryTimeout = null)
        {
            Enabled = enabled;
            MinimumTimout = minimumTimout;
            MaximumTimeout = maximumTimeout;

            if (failureEntryTimeout.HasValue)
            {
                FailureEntryTimeout = failureEntryTimeout.Value;
            }
        }

        public static string GetCacheKey(DnsQuestion question)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            return string.Concat(question.QueryName.Value, ":", (short)question.QuestionClass, ":", (short)question.QuestionType);
        }

        public IDnsQueryResponse Get(string key)
        {
            return Get(key, out _);
        }

        public IDnsQueryResponse Get(string key, out double? effectiveTtl)
        {
            effectiveTtl = null;
            if (key == null)
            {
                throw new ArgumentNullException(key);
            }

            if (!Enabled)
            {
                return null;
            }

            if (_cache.TryGetValue(key, out ResponseEntry entry))
            {
                effectiveTtl = entry.TTL;
                if (entry.IsExpiredFor(DateTimeOffset.UtcNow))
                {
                    _cache.TryRemove(key, out _);
                }
                else
                {
                    StartCleanup();
                    return entry.Response;
                }
            }

            return null;
        }

        public bool Add(string key, IDnsQueryResponse response, bool cacheFailures = false)
        {
            if (key == null)
            {
                throw new ArgumentNullException(key);
            }

            if (Enabled && response != null && (cacheFailures || (!response.HasError && response.Answers.Count > 0)))
            {
                if (response.Answers.Count == 0)
                {
                    // Cache entry for a failure response.
                    var newEntry = new ResponseEntry(response, FailureEntryTimeout.TotalMilliseconds);

                    StartCleanup();
                    return _cache.TryAdd(key, newEntry);
                }
                else
                {
                    var all = response.AllRecords.Where(p => !(p is Protocol.Options.OptRecord)).ToList();
                    if (all.Count != 0)
                    {
                        // in millis
                        double minTtl = all.Min(p => p.InitialTimeToLive) * 1000d;

                        if (MinimumTimout == Timeout.InfiniteTimeSpan)
                        {
                            // TODO: Log warning once?
                            minTtl = s_maxTimeout.TotalMilliseconds;
                        }
                        else if (MinimumTimout.HasValue && minTtl < MinimumTimout.Value.TotalMilliseconds)
                        {
                            minTtl = MinimumTimout.Value.TotalMilliseconds;
                        }

                        // max TTL check which can limit the upper boundary
                        if (MaximumTimeout.HasValue && MaximumTimeout != Timeout.InfiniteTimeSpan && minTtl > MaximumTimeout.Value.TotalMilliseconds)
                        {
                            minTtl = MaximumTimeout.Value.TotalMilliseconds;
                        }

                        if (minTtl < 1d)
                        {
                            return false;
                        }

                        var newEntry = new ResponseEntry(response, minTtl);

                        StartCleanup();
                        return _cache.TryAdd(key, newEntry);
                    }
                }
            }

            StartCleanup();
            return false;
        }

        private static void DoCleanup(ResponseCache cache)
        {
            cache._cleanupRunning = true;

            var now = DateTimeOffset.UtcNow;
            foreach (var entry in cache._cache)
            {
                if (entry.Value.IsExpiredFor(now))
                {
                    cache._cache.TryRemove(entry.Key, out _);
                }
            }

            cache._cleanupRunning = false;
        }

        private void StartCleanup()
        {
            if (!Enabled)
            {
                return;
            }

            // TickCount jump every 25days to int.MinValue, adjusting...
            var currentTicks = Environment.TickCount & int.MaxValue;
            if (_lastCleanup + s_cleanupInterval < 0 || currentTicks + s_cleanupInterval < 0)
            {
                _lastCleanup = 0;
            }

            if (!_cleanupRunning && _lastCleanup + s_cleanupInterval < currentTicks)
            {
                lock (_cleanupLock)
                {
                    if (!_cleanupRunning && _lastCleanup + s_cleanupInterval < currentTicks)
                    {
                        _lastCleanup = currentTicks;

                        Task.Factory.StartNew(
                            action: state => DoCleanup((ResponseCache)state),
                            state: this,
                            cancellationToken: CancellationToken.None,
                            creationOptions: TaskCreationOptions.DenyChildAttach,
                            scheduler: TaskScheduler.Default)
                            .ContinueWith(t =>
                            {
                                if (t.IsFaulted)
                                {
                                    /* Ignoring but handling background errors. */
                                }
                            },
                            scheduler: TaskScheduler.Default);
                    }
                }
            }
        }

        private class ResponseEntry
        {
            public bool IsExpiredFor(DateTimeOffset forDate) => forDate >= ExpiresAt;

            public DateTimeOffset ExpiresAt { get; }

            public DateTimeOffset Created { get; }

            public double TTL { get; set; }

            public IDnsQueryResponse Response { get; }

            public ResponseEntry(IDnsQueryResponse response, double ttlInMS)
            {
                Debug.Assert(response != null);
                Debug.Assert(ttlInMS >= 0);

                Response = response;
                TTL = ttlInMS;
                Created = DateTimeOffset.UtcNow;
                ExpiresAt = Created.AddMilliseconds(TTL);
            }
        }
    }
}
