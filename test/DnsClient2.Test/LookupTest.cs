using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DnsClient2.Protocol.Record;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DnsClient2.Tests
{
    public class LookupTest
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger logger;

        public LookupTest()
        {
            loggerFactory = new LoggerFactory()
                .AddConsole(LogLevel.Trace)
                .AddDebug(LogLevel.Trace);
            
            logger = loggerFactory.CreateLogger("testing");
        }

        private async Task<IPAddress> GetDnsEntryAsync()
        {
            // retries the normal host name (without domain)
            var hostname = Dns.GetHostName();
            var hostIp = await Dns.GetHostAddressesAsync(hostname);

            // find the actual IP of the adapter used for inter networking
            var ip = hostIp.Where(p => p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).First();
            return ip;
        }

        [Fact]
        public async Task Lookup_GetHostAddresses_Local()
        {
            var client = new LookupClient();
            var result = await client.QueryAsync("localhost", 1);

            var answer = result.Answers.OfType<ARecord>().First();
            Assert.Equal("127.0.0.1", answer.Address.ToString());
            Assert.Equal(1, result.Questions.First().QuestionClass);
            Assert.Equal(1, result.Questions.First().QuestionType);
            Assert.True(result.Header.AnswerCount > 0);
        }

        [Fact]
        public async Task Lookup_GetHostAddresses_LocalReverse_NoResult()
        {
            // expecting no result as reverse lookup must be explicit
            var client = new LookupClient();
            var result = await client.QueryAsync("127.0.0.1", 1);
            
            Assert.Equal(1, result.Questions.First().QuestionClass);
            Assert.Equal(1, result.Questions.First().QuestionType);
            Assert.True(result.Header.AnswerCount == 0);
        }

        //[Theory]
        //[InlineData(TransportType.Tcp)]
        //[InlineData(TransportType.Udp)]
        //public async Task Lookup_GetHostAddresses_ActualHost(TransportType transport)
        //{
        //    var entry = await GetDnsEntryAsync();

        //    var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { TransportType = transport });
        //    var result = await client.GetHostAddressesAsync(entry.HostName);

        //    Assert.True(entry.AddressList.Contains(result.First()));
        //}

        //[Theory]
        //[InlineData(TransportType.Tcp)]
        //[InlineData(TransportType.Udp)]
        //public async Task Lookup_GetHostEntryAsync_ByIp(TransportType transport)
        //{
        //    var entry = await GetDnsEntryAsync();
        //    var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { TransportType = transport });
        //    var result = await client.GetHostEntryAsync(entry.AddressList.First());

        //    Assert.True(entry.AddressList.Contains(result.AddressList.First()));
        //    Assert.Equal(entry.HostName, result.HostName);
        //}

        //[Theory]
        //[InlineData(TransportType.Tcp)]
        //[InlineData(TransportType.Udp)]
        //public async Task Lookup_GetHostEntryAsync_ByName(TransportType transport)
        //{
        //    var entry = await GetDnsEntryAsync();
        //    var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { TransportType = transport });
        //    var result = await client.GetHostEntryAsync(entry.HostName);

        //    Assert.True(entry.AddressList.Contains(result.AddressList.First()));
        //    Assert.Equal(entry.HostName, result.HostName);
        //}

        //[Theory]
        //[InlineData(TransportType.Tcp)]
        //[InlineData(TransportType.Udp)]
        //public async Task Lookup_Query_A(TransportType transport)
        //{
        //    var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { TransportType = transport });
        //    var result = await client.QueryAsync("google.com", QType.A);

        //    Assert.True(result.Answers.Count > 0);
        //    Assert.True(result.RecordsA.Count > 0);
        //}

        //[Theory]
        //[InlineData(TransportType.Tcp)]
        //[InlineData(TransportType.Udp)]
        //public async Task Lookup_Query_AAAA(TransportType transport)
        //{
        //    var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { TransportType = transport });
        //    var result = await client.QueryAsync("google.com", QType.AAAA);

        //    Assert.True(result.Answers.Count > 0);
        //    Assert.True(result.RecordsAAAA.Count > 0);
        //}

        //[Theory]
        //[InlineData(TransportType.Tcp)]
        //[InlineData(TransportType.Udp)]
        //public async Task Lookup_Query_Any(TransportType transport)
        //{
        //    var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { TransportType = transport });
        //    var result = await client.QueryAsync("google.com", QType.ANY);

        //    Assert.True(result.Answers.Count > 5);
        //    Assert.True(result.RecordsA.Count > 0);
        //    Assert.True(result.RecordsAAAA.Count > 0);
        //    Assert.True(result.RecordsMX.Count > 0);
        //}

        //[Theory]
        //[InlineData(TransportType.Tcp)]
        //[InlineData(TransportType.Udp)]
        //public async Task Lookup_Query_Mx(TransportType transport)
        //{
        //    var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { TransportType = transport });
        //    var result = await client.QueryAsync("google.com", QType.MX);

        //    Assert.True(result.Answers.Count > 0);
        //    Assert.True(result.RecordsMX.Count > 0);
        //}

        //[Theory]
        //[InlineData(TransportType.Tcp)]
        //[InlineData(TransportType.Udp)]
        //public async Task Lookup_Query_NS(TransportType transport)
        //{
        //    var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { TransportType = transport });
        //    var result = await client.QueryAsync("google.com", QType.NS);

        //    Assert.True(result.Answers.Count > 0);
        //    Assert.True(result.RecordsNS.Count > 0);
        //}

        //[Theory]
        //[InlineData(TransportType.Tcp)]
        //[InlineData(TransportType.Udp)]
        //public async Task Lookup_Query_TXT(TransportType transport)
        //{
        //    var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { TransportType = transport });
        //    var result = await client.QueryAsync("google.com", QType.TXT);

        //    Assert.True(result.Answers.Count > 0);
        //    Assert.True(result.RecordsTXT.Count > 0);
        //}

        //[Theory]
        //[InlineData(TransportType.Tcp)]
        //[InlineData(TransportType.Udp)]
        //public async Task Lookup_Query_SOA(TransportType transport)
        //{
        //    var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { TransportType = transport });
        //    var result = await client.QueryAsync("google.com", QType.SOA);

        //    Assert.True(result.Answers.Count > 0);
        //    Assert.True(result.RecordsSOA.Count > 0);
        //}

        //[Theory]
        //[InlineData(TransportType.Tcp)]
        //[InlineData(TransportType.Udp)]
        //public async Task Lookup_Query_ForceTimeout(TransportType transport)
        //{
        //    // basically testing we don't throw an error but return information
        //    var client = new DnsLookup(LoggerFactory, new DnsLookupOptions() { Timeout = 0, TransportType = transport });
        //    var result = await client.QueryAsync("google.com", QType.ANY);
        //    Assert.True(!string.IsNullOrWhiteSpace(result.Error));
        //    Assert.True(result.Answers.Count == 0);
        //}
    }
}