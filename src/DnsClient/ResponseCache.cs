using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DnsClient
{
    internal class ResponseCache
    {
        private static readonly long CleanupInterval = (long)TimeSpan.FromMinutes(10).TotalMilliseconds;
        private ConcurrentDictionary<string, ResponseEntry> _cache = new ConcurrentDictionary<string, ResponseEntry>();
        private object _cleanupLock = new object();
        private bool _cleanupRunning = false;
        private int _lastCleanup = 0;

        public int Count => _cache.Count;

        public bool Enabled { get; set; }

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

            return question.QueryName + ":" + question.QuestionClass + ":" + question.QuestionType;
        }

        public async Task<DnsQueryResponse> GetOrAdd(string key, Func<Task<DnsQueryResponse>> create)
        {
            if (create == null)
            {
                throw new ArgumentNullException(nameof(create));
            }

            if (!Enabled)
            {
                return await create();
            }

            ResponseEntry entry;
            if (_cache.TryGetValue(key, out entry))
            {
                if (entry.IsExpired)
                {
                    _cache.TryRemove(key, out entry);
                }
                else
                {
                    return entry.Response;
                }
            }

            var record = await create();

            // only cache in case the result is valid and does need caching
            if (record != null)
            {
                var newEntry = CreatedEntry(record);

                // respecting minimum expiration value which gets evaluated in CreateEntry
                if (newEntry.TTL > 0)
                {
                    _cache.TryAdd(key, newEntry);

                    StartCleanup();
                }
            }

            return record;
        }

        private static void DoCleanup(ResponseCache cache)
        {
            cache._cleanupRunning = true;

            foreach (var entry in cache._cache)
            {
                if (entry.Value.IsExpired)
                {
                    ResponseEntry o;
                    cache._cache.TryRemove(entry.Key, out o);
                }
            }

            cache._cleanupRunning = false;
        }

        private ResponseEntry CreatedEntry(DnsQueryResponse response)
        {
            var entry = new ResponseEntry(response);

            // minimum timeout
            if (MinimumTimout.HasValue && entry.TTL < MinimumTimout.Value.TotalMilliseconds)
            {
                entry.TTL = (long)MinimumTimout.Value.TotalMilliseconds;
            }

            return entry;
        }

        private void StartCleanup()
        {
            var currentTicks = Environment.TickCount;
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
            public bool IsExpired => Environment.TickCount > TicksCreated + TTL;

            public DnsQueryResponse Response { get; set; }

            public int TicksCreated { get; }

            public long TTL { get; set; }

            public ResponseEntry(DnsQueryResponse response)
            {
                var minTtl = response.AllRecords.Min(p => p?.TimeToLive);

                Response = response;
                TTL = response.HasError || !minTtl.HasValue ? 0 : (int)minTtl.Value * 1000;    // ttl is in second, we calculate in millis
                TicksCreated = Environment.TickCount;
            }
        }
    }
}