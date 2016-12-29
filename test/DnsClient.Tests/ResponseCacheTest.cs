using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DnsClient.Protocol;
using Xunit;

namespace DnsClient.Tests
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

    public class ResponseCacheTest
    {
        [Fact]
        public async Task Cache_DoesCacheWithMinimumDefined()
        {
            var cache = new ResponseCache(true, TimeSpan.FromMilliseconds(100));
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 0, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0);
            response.AddAnswer(record);

            var item = await cache.GetOrAdd("key", async () => response.AsQueryResponse(new NameServer(IPAddress.Any)));
            var item2 = await cache.GetOrAdd("key", async () => null);

            Assert.Equal(item, response.AsQueryResponse(new NameServer(IPAddress.Any)));

            // should not be null although TTL is zero, mimimum timeout is set to 100ms
            Assert.NotNull(item2);
        }

        [Fact]
        public async Task Cache_DoesNotCacheIfDisabled()
        {
            var cache = new ResponseCache(false);
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 100, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0);
            response.AddAnswer(record);

            var item = await cache.GetOrAdd("key", async () => response.AsQueryResponse(new NameServer(IPAddress.Any)));
            var item2 = await cache.GetOrAdd("key", async () => null);

            Assert.Equal(item, response.AsQueryResponse(new NameServer(IPAddress.Any)));

            // should be null because cache is disabled and we create null in the second call of GetOrAdd
            Assert.Null(item2);
        }

        [Fact]
        public async Task Cache_DoesNotCacheIfZeroTTL()
        {
            var cache = new ResponseCache(true);
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 0, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0);
            response.AddAnswer(record);

            var item = await cache.GetOrAdd("key", async () => response.AsQueryResponse(new NameServer(IPAddress.Any)));
            var item2 = await cache.GetOrAdd("key", async () => null);

            Assert.Equal(item, response.AsQueryResponse(new NameServer(IPAddress.Any)));

            // should be null because ttl of the record is 0 which disables caching for this record
            Assert.Null(item2);
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

            var item = await cache.GetOrAdd("key", async () => response.AsQueryResponse(new NameServer(IPAddress.Any)));
            var item2 = await cache.GetOrAdd("key", async () => null);

            Assert.Equal(item, response.AsQueryResponse(new NameServer(IPAddress.Any)));
            Assert.NotNull(item2);

            await Task.Delay(1100);
            var item3 = await cache.GetOrAdd("key", async () => null);

            // should be null because recordC has zero TTL which wins
            Assert.Null(item3);
        }

        [Fact]
        public async Task Cache_EntriesLowestTTLWins()
        {
            var cache = new ResponseCache(true);
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 1000, 100));
            var recordB = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 100, 100));
            var recordC = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 0, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0);
            response.AddAnswer(record);
            response.AddAdditional(recordB);
            response.AddAuthority(recordC);

            var item = await cache.GetOrAdd("key", async () => response.AsQueryResponse(new NameServer(IPAddress.Any)));
            var item2 = await cache.GetOrAdd("key", async () => null);

            Assert.Equal(item, response.AsQueryResponse(new NameServer(IPAddress.Any)));

            // should be null because recordC has zero TTL which wins
            Assert.Null(item2);
        }

        [Fact]
        public async Task Cache_GetOrAdd()
        {
            var cache = new ResponseCache(true);
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 100, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0);
            response.AddAnswer(record);

            var item = await cache.GetOrAdd("key", async () => response.AsQueryResponse(new NameServer(IPAddress.Any)));

            Assert.Equal(item, response.AsQueryResponse(new NameServer(IPAddress.Any)));
        }

        [Fact]
        public async Task Cache_GetOrAddExists()
        {
            var cache = new ResponseCache(true);
            var record = new EmptyRecord(new ResourceRecordInfo("a", ResourceRecordType.A, QueryClass.IN, 100, 100));
            var response = new DnsResponseMessage(new DnsResponseHeader(1, 256, 1, 1, 0, 0), 0);
            response.AddAnswer(record);

            var item = await cache.GetOrAdd("key", async () => response.AsQueryResponse(new NameServer(IPAddress.Any)));
            var item2 = await cache.GetOrAdd("key", async () => null);

            Assert.Equal(item, response.AsQueryResponse(new NameServer(IPAddress.Any)));
            Assert.NotNull(item2);
        }
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}