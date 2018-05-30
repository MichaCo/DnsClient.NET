using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DnsClient.Protocol;
using Xunit;

namespace DnsClient.Tests
{
    public class ResponseCacheTest
    {
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
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0)
            {
                Audit = new LookupClientAudit()
            };
            response.AddAnswer(record);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any)));
            var item = cache.Get("key", out double? effectiveTtl);

            // should not be null although TTL is zero, mimimum timeout is set to infinite
            Assert.Equal(item, response.AsQueryResponse(new NameServer(IPAddress.Any)));
            Assert.Equal(int.MaxValue, effectiveTtl);
        }

        [Fact]
        public async Task Cache_DoesCacheWithMinimumDefined()
        {
            var minTtl = 2000;
            var cache = new ResponseCache(true, TimeSpan.FromMilliseconds(minTtl));
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 0, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0)
            {
                Audit = new LookupClientAudit()
            };
            response.AddAnswer(record);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any)));

            await Task.Delay(1000);
            var item = cache.Get("key", out double? effectiveTtl);

            // should not be null although TTL is zero, mimimum timeout is set to 2000ms
            // TTL of the record should be negative because the initial TTL is 0
            Assert.True(item.Answers.First().TimeToLive < 0);
            Assert.Equal(minTtl, effectiveTtl);
        }

        [Fact]
        public void Cache_DoesNotCacheIfDisabled()
        {
            var cache = new ResponseCache(false);
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 100, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0)
            {
                Audit = new LookupClientAudit()
            };
            response.AddAnswer(record);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any)));
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
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0)
            {
                Audit = new LookupClientAudit()
            };
            response.AddAnswer(record);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any)));
            var item = cache.Get("key", out double? effectiveTtl);

            // should be null because ttl of the record is 0 which disables caching for this record
            Assert.Null(item);
            Assert.Null(effectiveTtl);
        }

        [Fact]
        public void Cache_DoesNotCacheIfZeroTTLWithZeroMinTtl()
        {
            var cache = new ResponseCache(true, TimeSpan.Zero);
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 0, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0)
            {
                Audit = new LookupClientAudit()
            };
            response.AddAnswer(record);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any)));
            var item = cache.Get("key", out double? effectiveTtl);

            // should be null because ttl of the record is 0 which disables caching for this record
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
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0)
            {
                Audit = new LookupClientAudit()
            };
            response.AddAnswer(record);
            response.AddAdditional(recordB);
            response.AddAuthority(recordC);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any)));

            await Task.Delay(200);
            var item = cache.Get("key", out double? effectiveTtl);

            Assert.NotNull(item);
            Assert.Equal(1 * 1000, effectiveTtl);

            await Task.Delay(1100);
            var item3 = cache.Get("key", out double? effectiveTtl2);

            Assert.Null(item3);
        }

        [Fact]
        public void Cache_EntriesLowestTTLWins()
        {
            var cache = new ResponseCache(true);
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 1000, 100));
            var recordB = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 100, 100));
            var recordC = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 0, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0)
            {
                Audit = new LookupClientAudit()
            };
            response.AddAnswer(record);
            response.AddAdditional(recordB);
            response.AddAuthority(recordC);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any)));
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
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0)
            {
                Audit = new LookupClientAudit()
            };
            response.AddAnswer(record);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any)));
            var item = cache.Get("key", out double? effectiveTtl);

            Assert.Equal(item, response.AsQueryResponse(new NameServer(IPAddress.Any)));
            Assert.Equal(100 * 1000, effectiveTtl);
        }

        [Fact]
        public void Cache_GetOrAddExists()
        {
            var cache = new ResponseCache(true);
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 100, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0)
            {
                Audit = new LookupClientAudit()
            };
            response.AddAnswer(record);

            var success = cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any)));
            Assert.True(success);

            response.AddAnswer(new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 100, 100)));
            var fail = cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any)));

            Assert.False(fail);
        }
    }
}