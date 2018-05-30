using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DnsClient.Protocol;
using Xunit;

namespace DnsClient.Tests
{
    public class ResponseCacheTest
    {
        [Fact]
        public void Cache_DoesCacheWithMinimumDefined()
        {
            var cache = new ResponseCache(true, TimeSpan.FromMilliseconds(100));
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 0, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0);
            response.AddAnswer(record);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any)));
            var item = cache.Get("key");

            // should not be null although TTL is zero, mimimum timeout is set to 100ms
            Assert.Equal(item, response.AsQueryResponse(new NameServer(IPAddress.Any)));
        }

        [Fact]
        public void Cache_DoesNotCacheIfDisabled()
        {
            var cache = new ResponseCache(false);
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 100, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0);
            response.AddAnswer(record);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any)));
            var item = cache.Get("key");

            // should be null because cache is disabled and we create null in the second call of GetOrAdd
            Assert.Null(item);
        }

        [Fact]
        public void Cache_DoesNotCacheIfZeroTTL()
        {
            var cache = new ResponseCache(true);
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 0, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0);
            response.AddAnswer(record);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any)));
            var item = cache.Get("key");

            // should be null because ttl of the record is 0 which disables caching for this record
            Assert.Null(item);
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

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any)));
            var item = cache.Get("key");

            Assert.NotNull(item);

            await Task.Delay(1100);
            var item3 = cache.Get("key");

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

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any)));
            var item = cache.Get("key");

            // should be null because recordC has zero TTL which wins
            Assert.Null(item);
        }

        [Fact]
        public void Cache_GetOrAdd()
        {
            var cache = new ResponseCache(true);
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 100, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0);
            response.AddAnswer(record);

            cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any)));
            var item = cache.Get("key");

            Assert.Equal(item, response.AsQueryResponse(new NameServer(IPAddress.Any)));
        }

        [Fact]
        public void Cache_GetOrAddExists()
        {
            var cache = new ResponseCache(true);
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 100, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0);
            response.AddAnswer(record);

            var success = cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any)));
            Assert.True(success);

            response.AddAnswer(new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 100, 100)));
            var fail = cache.Add("key", response.AsQueryResponse(new NameServer(IPAddress.Any)));

            Assert.False(fail);
        }
    }
}