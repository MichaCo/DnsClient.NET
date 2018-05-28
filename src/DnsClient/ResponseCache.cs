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
        private static readonly int CleanupInterval = (int)TimeSpan.FromMinutes(10).TotalMilliseconds;
        private readonly ConcurrentDictionary<string, ResponseEntry> _cache = new ConcurrentDictionary<string, ResponseEntry>();
        private readonly object _cleanupLock = new object();
        private bool _cleanupRunning = false;
        private int _lastCleanup = 0;

        public int Count => _cache.Count;

        public bool Enabled { get; set; } = true;

        public TimeSpan? MinimumTimout { get; set; }

        public ResponseCache(bool enabled = true, TimeSpan? minimumTimout = null)
        {
            Enabled = enabled;
            MinimumTimout = minimumTimout;
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
            if (key == null) throw new ArgumentNullException(key);
            if (!Enabled) return null;

            if (_cache.TryGetValue(key, out ResponseEntry entry))
            {
                if (entry.IsExpiredFor(DateTimeOffset.UtcNow))
                {
                    _cache.TryRemove(key, out entry);
                }
                else
                {
                    StartCleanup();
                    return entry.Response;
                }
            }

            return null;
        }

        public bool Add(string key, IDnsQueryResponse response)
        {
            if (key == null) throw new ArgumentNullException(key);
            if (Enabled && response != null && !response.HasError)
            {
                var all = response.AllRecords;
                if (all.Any())
                {
                    double minTtl = all.Min(p => p.TimeToLive) * 1000d;

                    if (MinimumTimout.HasValue && minTtl < MinimumTimout.Value.TotalMilliseconds)
                    {
                        minTtl = (long)MinimumTimout.Value.TotalMilliseconds;
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
                    cache._cache.TryRemove(entry.Key, out ResponseEntry o);
                }
            }

            cache._cleanupRunning = false;
        }

        private void StartCleanup()
        {
            // TickCount jump every 25days to int.MinValue, adjusting...
            var currentTicks = Environment.TickCount & int.MaxValue;
            if (_lastCleanup + CleanupInterval < 0 || currentTicks + CleanupInterval < 0) _lastCleanup = 0;
            if (!_cleanupRunning && _lastCleanup + CleanupInterval < currentTicks)
            {
                lock (_cleanupLock)
                {
                    if (!_cleanupRunning && _lastCleanup + CleanupInterval < currentTicks)
                    {
                        _lastCleanup = currentTicks;

                        Task.Factory.StartNew(
                            state => DoCleanup((ResponseCache)state),
                            this,
                            CancellationToken.None,
                            TaskCreationOptions.DenyChildAttach,
                            TaskScheduler.Default);
                    }
                }
            }
        }

        private class ResponseEntry
        {
            public bool IsExpiredFor(DateTimeOffset forDate) => forDate >= ExpiresAt;

            public IDnsQueryResponse Response { get; set; }

            public DateTimeOffset ExpiresAt { get; }

            public double TTL { get; set; }

            public ResponseEntry(IDnsQueryResponse response, double ttlInMS)
            {
                Debug.Assert(response != null);
                Debug.Assert(ttlInMS >= 0);

                Response = response;
                TTL = ttlInMS;
                ExpiresAt = DateTimeOffset.UtcNow.AddMilliseconds(TTL);
            }
        }
    }
}