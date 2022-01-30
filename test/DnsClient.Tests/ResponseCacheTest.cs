using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DnsClient.Protocol;
using Xunit;

namespace DnsClient.Tests
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ResponseCacheTest
    {
        static ResponseCacheTest()
        {
            Tracing.Source.Switch.Level = System.Diagnostics.SourceLevels.All;
        }

        [Fact]
        public void Cache_InvalidLessThanZero()
        {
            var ex = Assert.ThrowsAny<ArgumentOutOfRangeException>(
                () => new ResponseCache(true, TimeSpan.FromMilliseconds(-2)));
        }

        [Fact]
        public void Cache_InvalidMAx()
        {
            var ex = Assert.ThrowsAny<ArgumentOutOfRangeException>(
                () => new ResponseCache(true, TimeSpan.MaxValue));
        }

        [Fact]
        public void Cache_SupportsInfinite()
        {
            var cache = new ResponseCache(true, Timeout.InfiniteTimeSpan);
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 0, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0);
            response.AddAnswer(record);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any), null));
            var item = cache.Get("key", out double? effectiveTtl);

            // should not be null although TTL is zero, minimum timeout is set to infinite
            Assert.Equal(item, response.AsQueryResponse(new NameServer(IPAddress.Any), null));
            Assert.Equal(int.MaxValue, effectiveTtl);
        }

        [Fact]
        public void Cache_DoesCacheWithMinimumDefined()
        {
            var minTtl = 2000;
            var cache = new ResponseCache(true, TimeSpan.FromMilliseconds(minTtl));
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 0, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0);
            response.AddAnswer(record);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any), null));

            var item = cache.Get("key", out double? effectiveTtl);

            Assert.NotNull(item);
            Assert.Equal(0, item.Answers[0].TimeToLive);
            Assert.Equal(minTtl, effectiveTtl);
        }

        [Fact]
        public void Cache_RespectsMaximumTtl()
        {
            var maxTtl = 2000;
            var cache = new ResponseCache(true, maximumTimeout: TimeSpan.FromMilliseconds(maxTtl));
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 60 * 60 * 24, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0);
            response.AddAnswer(record);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any), null));

            var item = cache.Get("key", out double? effectiveTtl);

            Assert.NotNull(item);
            Assert.Equal(1, cache.Count);
            Assert.Equal(60 * 60 * 24, item.Answers[0].TimeToLive);
            Assert.Equal(maxTtl, effectiveTtl);
        }

        [Fact]
        public void Cache_DoesNotCacheIfDisabled()
        {
            var cache = new ResponseCache(false);
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 100, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0);
            response.AddAnswer(record);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any), null));
            var item = cache.Get("key", out double? effectiveTtl);

            // should be null because cache is disabled and we create null in the second call of GetOrAdd
            Assert.Null(item);
            Assert.Null(effectiveTtl);
        }

        [Fact]
        public void Cache_DoesNotCacheIfZeroTTL()
        {
            var cache = new ResponseCache(true);
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 0, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0);
            response.AddAnswer(record);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any), null));
            var item = cache.Get("key", out double? effectiveTtl);

            // should be null because TTL of the record is 0 which disables caching for this record
            Assert.Null(item);
            Assert.Null(effectiveTtl);
        }

        [Fact]
        public void Cache_DoesNotCacheIfZeroTTLWithZeroMinTtl()
        {
            var cache = new ResponseCache(true, TimeSpan.Zero);
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 0, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0);
            response.AddAnswer(record);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any), null));
            var item = cache.Get("key", out double? effectiveTtl);

            // should be null because TTL of the record is 0 which disables caching for this record
            Assert.Null(item);
            Assert.Null(effectiveTtl);
        }

        [Fact]
        public async Task Cache_EntriesLowestTT_Expires()
        {
            var cache = new ResponseCache(true);
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 1000, 100));
            var recordB = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 100, 100));
            var recordC = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 1, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0);
            response.AddAnswer(record);
            response.AddAdditional(recordB);
            response.AddAuthority(recordC);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any), null));

            await Task.Delay(200).ConfigureAwait(false);
            var item = cache.Get("key", out double? effectiveTtl);

            Assert.NotNull(item);
            Assert.Equal(1 * 1000, effectiveTtl);

            await Task.Delay(1100).ConfigureAwait(false);
            var item3 = cache.Get("key", out _);

            Assert.Null(item3);
        }

        [Fact]
        public void Cache_EntriesLowestTTLWins()
        {
            var cache = new ResponseCache(true);
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 1000, 100));
            var recordB = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 100, 100));
            var recordC = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 0, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0);
            response.AddAnswer(record);
            response.AddAdditional(recordB);
            response.AddAuthority(recordC);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any), null));
            var item = cache.Get("key", out double? effectiveTtl);

            // should be null because recordC has zero TTL which wins
            Assert.Null(item);
            Assert.Null(effectiveTtl);
        }

        [Fact]
        public void Cache_GetOrAdd()
        {
            var cache = new ResponseCache(true);
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 100, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0);
            response.AddAnswer(record);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any), null));
            var item = cache.Get("key", out double? effectiveTtl);

            Assert.Equal(item, response.AsQueryResponse(new NameServer(IPAddress.Any), null));
            Assert.Equal(100 * 1000, effectiveTtl);
        }

        [Fact]
        public void Cache_GetOrAddExists()
        {
            var cache = new ResponseCache(true);
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 100, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0);
            response.AddAnswer(record);

            var success = cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any), null));
            Assert.True(success);

            response.AddAnswer(new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 100, 100)));
            var fail = cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any), null));

            Assert.False(fail);
        }

        [Fact]
        public void Cache_DoesNotCacheFailureIfDisabled()
        {
            var cache = new ResponseCache(true);
            var failureStatus = DnsResponseCode.NotExistentDomain;
            var response = new DnsResponseMessage(new DnsResponseHeader(1, (ushort)failureStatus, 0, 0, 0, 0), 0);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any), null));
            var item = cache.Get("key", out _);

            // Should be null because cache does not accept failure responses by default.
            Assert.Null(item);
        }

        [Fact]
        public void Cache_DoesCacheFailureIfEnabled()
        {
            var cache = new ResponseCache(true);
            var failureStatus = DnsResponseCode.NotExistentDomain;
            var response = new DnsResponseMessage(new DnsResponseHeader(1, (ushort)failureStatus, 0, 0, 0, 0), 0);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any), null), true);
            var item = cache.Get("key", out _);

            Assert.NotNull(item);
        }

        [Fact]
        public async Task Cache_DoesCacheFailureExpire()
        {
            var cache = new ResponseCache(true, null, null, TimeSpan.FromMilliseconds(1));
            var failureStatus = DnsResponseCode.NotExistentDomain;
            var response = new DnsResponseMessage(new DnsResponseHeader(1, (ushort)failureStatus, 0, 0, 0, 0), 0);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any), null), true);

            await Task.Delay(10).ConfigureAwait(false);

            var item = cache.Get("key", out _);

            // Should be null because failed response expires after 1 millisecond.
            Assert.Null(item);
        }
    }
}
