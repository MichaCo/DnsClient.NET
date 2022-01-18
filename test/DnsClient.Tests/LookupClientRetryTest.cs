using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DnsClient.Protocol;
using Xunit;

namespace DnsClient.Tests
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class LookupClientRetryTest
    {
        // https://github.com/MichaCo/DnsClient.NET/issues/52
        private static readonly byte[] s_issue52data = new byte[] { 31, 169, 129, 128, 0, 1, 0, 18, 0, 0, 0, 1, 20, 95, 97, 99, 109, 101, 45, 99, 104, 97, 108, 108, 101, 110, 103, 101, 45, 116, 101, 115, 116, 8, 97, 122, 117, 114, 101, 100, 110, 115, 4, 115, 105, 116, 101, 0, 0, 16, 0, 1, 192, 12, 0, 16, 0, 1, 0, 0, 0, 60, 0, 42, 41, 87, 84, 98, 112, 74, 121, 122, 81, 87, 112, 85, 112, 82, 97, 45, 108, 73, 95, 118, 73, 95, 121, 51, 95, 68, 101, 75, 80, 48, 77, 114, 109, 121, 87, 107, 55, 86, 85, 76, 82, 51, 192, 12, 0, 16, 0, 1, 0, 0, 0, 60, 0, 42, 41, 87, 84, 98, 112, 74, 121, 122, 81, 87, 112, 85, 112, 82, 97, 45, 108, 73, 95, 118, 73, 95, 121, 51, 95, 68, 101, 75, 80, 48, 77, 114, 109, 121, 87, 107, 55, 86, 85, 76, 82, 52, 192, 12, 0, 16, 0, 1, 0, 0, 0, 60, 0, 43, 42, 87, 84, 98, 112, 74, 121, 122, 81, 87, 112, 85, 100, 45, 108, 73, 95, 118, 73, 95, 121, 51, 95, 68, 101, 75, 80, 48, 77, 114, 109, 121, 87, 107, 55, 86, 85, 76, 82, 118, 66, 73, 57, 192, 12, 0, 16, 0, 1, 0, 0, 0, 60, 0, 43, 42, 87, 84, 98, 112, 74, 121, 122, 81, 87, 112, 85, 112, 82, 97, 45, 108, 73, 95, 118, 73, 95, 121, 51, 95, 68, 101, 75, 80, 48, 77, 114, 109, 121, 87, 107, 55, 86, 85, 76, 82, 118, 97, 192, 12, 0, 16, 0, 1, 0, 0, 0, 60, 0, 43, 42, 87, 84, 98, 112, 74, 121, 122, 81, 87, 112, 85, 112, 82, 97, 45, 108, 73, 95, 118, 73, 95, 121, 51, 95, 68, 101, 75, 80, 48, 77, 114, 109, 121, 87, 107, 55, 86, 85, 76, 82, 118, 115, 192, 12, 0, 16, 0, 1, 0, 0, 0, 60, 0, 43, 42, 87, 84, 98, 112, 74, 121, 122, 81, 87, 112, 85, 112, 82, 97, 45, 108, 73, 95, 118, 73, 95, 121, 51, 95, 68, 101, 75, 80, 48, 77, 114, 109, 121, 87, 107, 55, 86, 85, 76, 82, 118, 119, 192, 12, 0, 16, 0, 1, 0, 0, 0, 60, 0, 43, 42, 87, 84, 98, 112, 74, 121, 122, 81, 87, 112, 85, 112, 82, 97, 45, 108, 73, 95, 118, 73, 95, 121, 51, 95, 68, 101, 75, 80, 48, 77, 114, 109, 121, 87, 107, 55, 86, 85, 76, 82, 119, 101, 192, 12, 0, 16, 0, 1, 0, 0, 0, 60, 0, 44, 43, 87, 84, 98, 112, 74, 121, 122, 81, 87, 112, 85, 112, 82, 97, 45, 108, 73, 95, 118, 73, 95, 121, 51, 95, 68, 101, 75, 80, 48, 77, 114, 109, 121, 87, 52, 55, 86, 85, 76, 82, 118, 66, 73, 192, 12, 0, 16, 0, 1, 0, 0, 0, 60, 0, 44, 43, 87, 84, 98, 112, 74, 121, 122, 81 };

        static LookupClientRetryTest()
        {
            Tracing.Source.Switch.Level = System.Diagnostics.SourceLevels.All;
        }

        [Fact]
        public void PreserveNameServerOrder()
        {
            var options = new LookupClientOptions(
                   new NameServer(IPAddress.Parse("127.0.10.1")),
                   new NameServer(IPAddress.Parse("127.0.10.2")),
                   new NameServer(IPAddress.Parse("127.0.10.3")))
            {
                UseRandomNameServer = false,
                UseCache = false
            };

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(
                    new DnsResponseHeader(req.Header.Id, (int)DnsResponseCode.NoError, 0, 0, 0, 0),
                    0);
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);

            // calling multiple times will always use the first server when it returns NoError.
            // No other servers should be called
            for (var i = 0; i < 3; i++)
            {
                var result = lookup.Query("test.com", QueryType.A);
                Assert.False(result.HasError);
            }

            Assert.Equal(9, calledIps.Count);
            Assert.Equal(IPAddress.Parse("127.0.10.1"), calledIps[0]);
            Assert.Equal(IPAddress.Parse("127.0.10.1"), calledIps[3]);
            Assert.Equal(IPAddress.Parse("127.0.10.1"), calledIps[6]);
        }

        [Fact]
        public async Task PreserveNameServerOrderAsync()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")))
            {
                UseRandomNameServer = false,
                UseCache = false
            };

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(
                    new DnsResponseHeader(req.Header.Id, (int)DnsResponseCode.NoError, 0, 0, 0, 0),
                    0);
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);

            for (var i = 0; i < 3; i++)
            {
                var result = await lookup.QueryAsync(new DnsQuestion("test.com", QueryType.A, QueryClass.IN));
                Assert.False(result.HasError);
            }

            // 3 servers get called 3 times because none returns a response. Order should be the same
            // so every first server of each loop should be our first one.
            Assert.Equal(9, calledIps.Count);
            Assert.Equal(IPAddress.Parse("127.0.10.1"), calledIps[0]);
            Assert.Equal(IPAddress.Parse("127.0.10.1"), calledIps[3]);
            Assert.Equal(IPAddress.Parse("127.0.10.1"), calledIps[6]);
        }

        [Fact]
        public void ShuffleNameServerOrder()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")))
            {
                UseRandomNameServer = true,
                UseCache = false
            };

            var calledIps = new List<IPAddress>();
            var uniqueIps = new HashSet<IPAddress>();
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                uniqueIps.Add(ip.Address);
                return new DnsResponseMessage(
                    new DnsResponseHeader(req.Header.Id, (int)DnsResponseCode.BadAlgorithm, 0, 0, 0, 0),
                    0);
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);

            // consecutive calls should cycle through the servers (not always using the first one)
            // because this is only one thread, the order is always the same. But this can be very
            // different with multiple tasks)
            for (var i = 0; i < 6; i++)
            {
                var result = lookup.Query("test.com", QueryType.A);
                Assert.True(result.HasError);
            }

            Assert.Equal(18, calledIps.Count);
            Assert.Equal(3, uniqueIps.Count);
        }

        [Fact]
        public async Task ShuffleNameServerOrderAsync()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")))
            {
                UseRandomNameServer = true,
                UseCache = false
            };

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(
                    new DnsResponseHeader(req.Header.Id, (int)DnsResponseCode.BadAlgorithm, 0, 0, 0, 0),
                    0);
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);

            // consecutive calls should cycle through the servers (not always using the first one)
            // because this is only one thread, the order is always the same. But this can be very
            // different with multiple tasks)
            for (var i = 0; i < 6; i++)
            {
                var result = await lookup.QueryAsync(new DnsQuestion("test.com", QueryType.A, QueryClass.IN));
                Assert.True(result.HasError);
            }

            Assert.Equal(18, calledIps.Count);
        }

        [Fact]
        public void DnsResponseException_Continue_DontThrow()
        {
            var ip1 = IPAddress.Parse("127.0.10.1");
            var ip2 = IPAddress.Parse("127.0.10.2");
            var options = new LookupClientOptions(
                new NameServer(ip1),
                new NameServer(ip2),
                new NameServer(IPAddress.Parse("127.0.10.4")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = true,
                ThrowDnsErrors = false,
                UseCache = true,
                Retries = 3,
                UseRandomNameServer = false,
                UseTcpFallback = false
            };

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                var status = DnsHeaderResponseCode.NotExistentDomain;

                // response from ip1 & 2 should be retried with same server.
                if (ip.Address == ip1)
                {
                    status = DnsHeaderResponseCode.FormatError;
                }
                if (ip.Address == ip2)
                {
                    status = DnsHeaderResponseCode.ServerFailure;
                }

                calledIps.Add(ip.Address);
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (ushort)status, 0, 0, 0, 0), 0);
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var result = lookup.Query(new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

            // no exception but error in the response after calling all endpoints!
            Assert.True(result.HasError);
            Assert.Equal(DnsHeaderResponseCode.NotExistentDomain, result.Header.ResponseCode);

            // 1x 1 try, 2x 4 tries
            Assert.Equal(9, calledIps.Count);
        }

        [Fact]
        public async Task DnsResponseException_Continue_DontThrow_Async()
        {
            var ip1 = IPAddress.Parse("127.0.10.1");
            var ip2 = IPAddress.Parse("127.0.10.2");
            var options = new LookupClientOptions(
                new NameServer(ip1),
                new NameServer(ip2),
                new NameServer(IPAddress.Parse("127.0.10.4")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = true,
                ThrowDnsErrors = false,
                UseCache = true,
                Retries = 3,
                UseRandomNameServer = false,
                UseTcpFallback = false
            };

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                var status = DnsHeaderResponseCode.NotExistentDomain;

                // response from ip1 & 2 should be retried with same server.
                if (ip.Address == ip1)
                {
                    status = DnsHeaderResponseCode.FormatError;
                }
                if (ip.Address == ip2)
                {
                    status = DnsHeaderResponseCode.ServerFailure;
                }

                calledIps.Add(ip.Address);
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (ushort)status, 0, 0, 0, 0), 0);
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var result = await lookup.QueryAsync(new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

            // no exception but error in the response after calling all 3 endpoints!
            Assert.True(result.HasError);
            Assert.Equal(DnsHeaderResponseCode.NotExistentDomain, result.Header.ResponseCode);

            // 1x 1 try, 2x 4 tries
            Assert.Equal(9, calledIps.Count);
        }

        [Fact]
        public void DnsResponseException_Continue_Throw()
        {
            var ip1 = IPAddress.Parse("127.0.10.1");
            var ip2 = IPAddress.Parse("127.0.10.2");
            var options = new LookupClientOptions(
                new NameServer(ip1),
                new NameServer(ip2),
                new NameServer(IPAddress.Parse("127.0.10.4")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = true,
                ThrowDnsErrors = true,
                UseCache = true,
                Retries = 3,
                UseRandomNameServer = false,
                UseTcpFallback = false
            };

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                var status = DnsHeaderResponseCode.NotExistentDomain;

                // response from ip1 & 2 should be retried with same server.
                if (ip.Address == ip1)
                {
                    status = DnsHeaderResponseCode.FormatError;
                }
                if (ip.Address == ip2)
                {
                    status = DnsHeaderResponseCode.ServerFailure;
                }

                calledIps.Add(ip.Address);
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (ushort)status, 0, 0, 0, 0), 0);
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var request = new DnsQuestion("test.com", QueryType.A, QueryClass.IN);

            // all three servers have been called and we get the last exception thrown
            var result = Assert.ThrowsAny<DnsResponseException>(() => lookup.Query(request));

            // ensure the error is the one from the last call
            Assert.Equal(DnsResponseCode.NotExistentDomain, result.Code);

            Assert.Equal(9, calledIps.Count);
        }

        [Fact]
        public async Task DnsResponseException_Continue_Throw_Async()
        {
            var ip1 = IPAddress.Parse("127.0.10.1");
            var ip2 = IPAddress.Parse("127.0.10.2");
            var options = new LookupClientOptions(
                new NameServer(ip1),
                new NameServer(ip2),
                new NameServer(IPAddress.Parse("127.0.10.4")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = true,
                ThrowDnsErrors = true,
                UseCache = true,
                Retries = 3,
                UseRandomNameServer = false,
                UseTcpFallback = false
            };

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                var status = DnsHeaderResponseCode.NotExistentDomain;

                // response from ip1 & 2 should be retried with same server.
                if (ip.Address == ip1)
                {
                    status = DnsHeaderResponseCode.FormatError;
                }
                if (ip.Address == ip2)
                {
                    status = DnsHeaderResponseCode.ServerFailure;
                }

                calledIps.Add(ip.Address);
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (ushort)status, 0, 0, 0, 0), 0);
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var request = new DnsQuestion("test.com", QueryType.A, QueryClass.IN);
            var result = await Assert.ThrowsAnyAsync<DnsResponseException>(() => lookup.QueryAsync(request));

            // ensure the error is the one from the last call
            Assert.Equal(DnsResponseCode.NotExistentDomain, result.Code);

            Assert.Equal(9, calledIps.Count);
        }

        [Fact]
        public void DnsResponseException_DontContinue_DontThrow()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false,
                Retries = 5,
                UseCache = true,
                UseRandomNameServer = false,
                UseTcpFallback = false
            };

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderResponseCode.NotExistentDomain, 0, 0, 0, 0), 0);
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var result = lookup.Query(new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

            Assert.True(result.HasError);
            Assert.Equal(DnsHeaderResponseCode.NotExistentDomain, result.Header.ResponseCode);

            // ensure we got the error right from the first server call
            Assert.Single(calledIps);
        }

        [Fact]
        public async Task DnsResponseException_DontContinue_DontThrow_Async()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false,
                Retries = 5,
                UseCache = true,
                UseRandomNameServer = false,
                UseTcpFallback = false
            };

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderResponseCode.NotExistentDomain, 0, 0, 0, 0), 0);
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var result = await lookup.QueryAsync(new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

            Assert.True(result.HasError);
            Assert.Equal(DnsHeaderResponseCode.NotExistentDomain, result.Header.ResponseCode);

            // ensure we got the error right from the first server call
            Assert.Single(calledIps);
        }

        [Fact]
        public void DnsResponseException_DontContinue_Throw()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = true,
                Retries = 5,
                UseCache = true,
                UseRandomNameServer = false,
                UseTcpFallback = false
            };

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderResponseCode.NotExistentDomain, 0, 0, 0, 0), 0);
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var request = new DnsQuestion("test.com", QueryType.A, QueryClass.IN);
            var result = Assert.ThrowsAny<DnsResponseException>(() => lookup.Query(request));

            Assert.Equal(DnsResponseCode.NotExistentDomain, result.Code);

            Assert.Single(calledIps);
        }

        [Fact]
        public async Task DnsResponseException_DontContinue_Throw_Async()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = true,
                Retries = 5,
                UseCache = true,
                UseRandomNameServer = false,
                UseTcpFallback = false
            };

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderResponseCode.NotExistentDomain, 0, 0, 0, 0), 0);
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var request = new DnsQuestion("test.com", QueryType.A, QueryClass.IN);
            var result = await Assert.ThrowsAnyAsync<DnsResponseException>(() => lookup.QueryAsync(request));

            Assert.Equal(DnsResponseCode.NotExistentDomain, result.Code);
            Assert.Single(calledIps);
        }

        /* ContinueOnEmptyResponse */

        [Fact]
        public void ContinueOnEmptyResponse_ShouldTryNextServer_OnEmptyResponse()
        {
            var ip1 = IPAddress.Parse("127.0.10.1");
            var ip2 = IPAddress.Parse("127.0.10.2");
            var ip3 = IPAddress.Parse("127.0.10.3");
            var options = new LookupClientOptions(
                new NameServer(ip1),
                new NameServer(ip2),
                new NameServer(ip3))
            {
                ContinueOnEmptyResponse = true,
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = true,
                Retries = 5,
                UseCache = false,
                UseRandomNameServer = false,
                UseTcpFallback = false
            };

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderResponseCode.NoError, 0, 0, 0, 0), 0);
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var result = lookup.Query("test.com", QueryType.A);

            Assert.Equal(3, calledIps.Count);
        }

        [Fact]
        public async Task ContinueOnEmptyResponse_ShouldTryNextServer_OnEmptyResponse_Async()
        {
            var ip1 = IPAddress.Parse("127.0.10.1");
            var ip2 = IPAddress.Parse("127.0.10.2");
            var ip3 = IPAddress.Parse("127.0.10.3");
            var options = new LookupClientOptions(
                new NameServer(ip1),
                new NameServer(ip2),
                new NameServer(ip3))
            {
                ContinueOnEmptyResponse = true,
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = true,
                Retries = 5,
                UseCache = false,
                UseRandomNameServer = false,
                UseTcpFallback = false
            };

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderResponseCode.NoError, 0, 0, 0, 0), 0);
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var result = await lookup.QueryAsync("test.com", QueryType.A);

            Assert.Equal(3, calledIps.Count);
        }

        [Fact]
        public void ContinueOnEmptyResponse_ShouldNotTryNextServer_OnOkResponse()
        {
            var ip1 = IPAddress.Parse("127.0.10.1");
            var ip2 = IPAddress.Parse("127.0.10.2");
            var ip3 = IPAddress.Parse("127.0.10.3");
            var options = new LookupClientOptions(
                new NameServer(ip1),
                new NameServer(ip2),
                new NameServer(ip3))
            {
                ContinueOnEmptyResponse = true,
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = true,
                Retries = 5,
                UseCache = false,
                UseRandomNameServer = false,
                UseTcpFallback = false
            };

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                var response = new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderResponseCode.NoError, 0, 1, 0, 0), 0);
                response.AddAnswer(new ARecord(new ResourceRecordInfo("google.com", ResourceRecordType.A, QueryClass.IN, 100, 50), IPAddress.Parse("172.217.22.238")));
                return response;
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var result = lookup.Query("test.com", QueryType.A);

            Assert.Single(calledIps);
        }

        [Fact]
        public async Task ContinueOnEmptyResponse_ShouldNotTryNextServer_OnOkResponse_Async()
        {
            var ip1 = IPAddress.Parse("127.0.10.1");
            var ip2 = IPAddress.Parse("127.0.10.2");
            var ip3 = IPAddress.Parse("127.0.10.3");
            var options = new LookupClientOptions(
                new NameServer(ip1),
                new NameServer(ip2),
                new NameServer(ip3))
            {
                ContinueOnEmptyResponse = true,
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = true,
                Retries = 5,
                UseCache = false,
                UseRandomNameServer = false,
                UseTcpFallback = false
            };

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                var response = new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderResponseCode.NoError, 0, 1, 0, 0), 0);
                response.AddAnswer(new ARecord(new ResourceRecordInfo("google.com", ResourceRecordType.A, QueryClass.IN, 100, 50), IPAddress.Parse("172.217.22.238")));
                return response;
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var result = await lookup.QueryAsync("test.com", QueryType.A);

            Assert.Single(calledIps);
        }

        /* See https://github.com/MichaCo/DnsClient.NET/issues/93
         * Test should proof that 
         * First server has DNS error
         * Second returns empty result
         * => Depending on settings (don't throw DNS, continue on DNS and don't continue on empty)
         *      The result should be an empty result and no error.
         */

        [Fact]
        public void AcceptEmptyResponse_ContinueOnDnsErrors()
        {
            var ip1 = IPAddress.Parse("127.0.10.1");
            var ip2 = IPAddress.Parse("127.0.10.2");
            var ip3 = IPAddress.Parse("127.0.10.3");
            var options = new LookupClientOptions(
                new NameServer(ip1),
                new NameServer(ip2),
                new NameServer(ip3))
            {
                ContinueOnEmptyResponse = false,
                ContinueOnDnsError = true,
                ThrowDnsErrors = false,
                Retries = 5,
                UseCache = false,
                EnableAuditTrail = true,
                UseRandomNameServer = false,
                UseTcpFallback = false
            };

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                var status = DnsHeaderResponseCode.NoError;

                // response from ip1 & 2 should be retried with same server.
                if (ip.Address == ip1)
                {
                    status = DnsHeaderResponseCode.NotExistentDomain;
                }
                if (ip.Address == ip2)
                {
                    status = DnsHeaderResponseCode.NoError;
                }
                if (ip.Address == ip3)
                {
                    status = DnsHeaderResponseCode.NoError;
                }

                calledIps.Add(ip.Address);
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (ushort)status, 0, 0, 0, 0), 0);
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var result = lookup.Query("test.com", QueryType.A);

            // 2nd server already returns an empty result, expecting not to hit the 3rd one
            Assert.Equal(2, calledIps.Count);

            // Empty result expected
            Assert.Equal(0, result.Answers.Count);

            // Response wasn't an error.
            Assert.False(result.HasError);
        }

        /* DNS response parse error handling and retries */

        // https://github.com/MichaCo/DnsClient.NET/issues/52
        [Fact]
        public void DnsResponseParseException_ShouldTryTcp_Issue52()
        {
            var ip1 = IPAddress.Parse("127.0.10.1");
            var ip2 = IPAddress.Parse("127.0.10.2");
            var ip3 = IPAddress.Parse("127.0.10.3");
            var options = new LookupClientOptions(
                new NameServer(ip1),
                new NameServer(ip2),
                new NameServer(ip3))
            {
                ContinueOnEmptyResponse = false,
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false,
                UseCache = true,
                Retries = 3,
                UseRandomNameServer = false,
                UseTcpFallback = true
            };

            var calledIps = new List<IPAddress>();
            bool calledUdp = false, calledTcp = false;
            var udpMessageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledUdp = true;

                // A result which didn't fit into 512 bytes and which was not marked via TC as truncated.
                // The server returned 512 bytes because the response was truncated by a firewall or so.
                // This should retry the full request via TCP
                var handle = new DnsUdpMessageHandler(true);
                return handle.GetResponseMessage(new ArraySegment<byte>(s_issue52data));
            });

            var tcpMessageHandler = new TestMessageHandler(DnsMessageHandleType.TCP, (ip, req) =>
         {
             calledIps.Add(ip.Address);
             calledTcp = true;
             return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderResponseCode.NoError, 0, 0, 0, 0), 0);
         });

            var lookup = new LookupClient(options, udpMessageHandler, tcpMessageHandler);
            var result = lookup.Query(new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

            Assert.False(result.HasError);

            Assert.True(calledUdp);
            Assert.True(calledTcp);

            // It should fallback to TCP right away and call the first server twice, and then try all other servers because we don't return any answers
            Assert.Equal(2, calledIps.Count);
            Assert.Equal(new[] { ip1, ip1 }, calledIps);
        }

        [Fact]
        public async Task DnsResponseParseException_ShouldTryTcp_Issue52_Async()
        {
            var ip1 = IPAddress.Parse("127.0.10.1");
            var ip2 = IPAddress.Parse("127.0.10.2");
            var ip3 = IPAddress.Parse("127.0.10.3");
            var options = new LookupClientOptions(
                new NameServer(ip1),
                new NameServer(ip2),
                new NameServer(ip3))
            {
                ContinueOnEmptyResponse = false,
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false,
                UseCache = true,
                Retries = 3,
                UseRandomNameServer = false,
                UseTcpFallback = true
            };

            var calledIps = new List<IPAddress>();
            bool calledUdp = false, calledTcp = false;
            var udpMessageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledUdp = true;

                // A result which didn't fit into 512 bytes and which was not marked via TC as truncated.
                // The server returned 512 bytes because the response was truncated by a firewall or so.
                // This should retry the full request via TCP
                var handle = new DnsUdpMessageHandler(true);
                return handle.GetResponseMessage(new ArraySegment<byte>(s_issue52data));
            });

            var tcpMessageHandler = new TestMessageHandler(DnsMessageHandleType.TCP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledTcp = true;
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderResponseCode.NoError, 0, 0, 0, 0), 0);
            });

            var lookup = new LookupClient(options, udpMessageHandler, tcpMessageHandler);
            var result = await lookup.QueryAsync(new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

            Assert.False(result.HasError);

            Assert.True(calledUdp);
            Assert.True(calledTcp);

            // It should fallback to TCP right away and call the first server twice.
            Assert.Equal(2, calledIps.Count);
            Assert.Equal(new[] { ip1, ip1 }, calledIps);
        }

        [Fact]
        public void DnsResponseParseException_ShouldTryTcp_LargerResponse()
        {
            var ip1 = IPAddress.Parse("127.0.10.1");
            var ip2 = IPAddress.Parse("127.0.10.2");
            var ip3 = IPAddress.Parse("127.0.10.3");
            var options = new LookupClientOptions(
                new NameServer(ip1),
                new NameServer(ip2),
                new NameServer(ip3))
            {
                ContinueOnEmptyResponse = true,
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false,
                UseCache = true,
                Retries = 3,
                UseRandomNameServer = false,
                UseTcpFallback = true
            };

            var calledIps = new List<IPAddress>();
            bool calledUdp = false, calledTcp = false;
            var udpMessageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledUdp = true;

                // This should retry the full request via TCP.
                throw new DnsResponseParseException("Some error at the end", new byte[1000], 995, 10);
            });

            var tcpMessageHandler = new TestMessageHandler(DnsMessageHandleType.TCP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledTcp = true;
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderResponseCode.NoError, 0, 0, 0, 0), 0);
            });

            var lookup = new LookupClient(options, udpMessageHandler, tcpMessageHandler);
            var result = lookup.Query(new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

            Assert.False(result.HasError);
            Assert.True(calledUdp);
            Assert.True(calledTcp);

            Assert.Equal(4, calledIps.Count);
            Assert.Equal(new[] { ip1, ip1, ip2, ip3 }, calledIps);
        }

        [Fact]
        public async Task DnsResponseParseException_ShouldTryTcp_LargerResponse_Async()
        {
            var ip1 = IPAddress.Parse("127.0.10.1");
            var ip2 = IPAddress.Parse("127.0.10.2");
            var ip3 = IPAddress.Parse("127.0.10.3");
            var options = new LookupClientOptions(
                new NameServer(ip1),
                new NameServer(ip2),
                new NameServer(ip3))
            {
                ContinueOnEmptyResponse = true,
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false,
                UseCache = true,
                Retries = 3,
                UseRandomNameServer = false,
                UseTcpFallback = true
            };

            var calledIps = new List<IPAddress>();
            bool calledUdp = false, calledTcp = false;
            var udpMessageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledUdp = true;

                // This should retry the full request via TCP.
                throw new DnsResponseParseException("Some error at the end", new byte[1000], 995, 10);
            });

            var tcpMessageHandler = new TestMessageHandler(DnsMessageHandleType.TCP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledTcp = true;
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderResponseCode.NoError, 0, 0, 0, 0), 0);
            });

            var lookup = new LookupClient(options, udpMessageHandler, tcpMessageHandler);
            var result = await lookup.QueryAsync(new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

            Assert.False(result.HasError);
            Assert.True(calledUdp);
            Assert.True(calledTcp);

            // It should fallback to TCP right away and call the first server twice,
            // and then try all other servers because we don't return any answers because ContinueOnEmptyResponse = true,
            Assert.Equal(4, calledIps.Count);
            Assert.Equal(new[] { ip1, ip1, ip2, ip3 }, calledIps);
        }

        [Fact]
        public void DnsResponseParseException_FailWithoutTcpFallback()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false,
                UseCache = true,
                Retries = 3,
                UseRandomNameServer = false,
                UseTcpFallback = false  // Disabled => will throw on fallback.
            };

            var calledIps = new List<IPAddress>();
            var udpMessageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);

                var handle = new DnsUdpMessageHandler(true);
                return handle.GetResponseMessage(new ArraySegment<byte>(s_issue52data));
            });

            var lookup = new LookupClient(options, udpMessageHandler, TestMessageHandler.Tcp);
            var result = Assert.ThrowsAny<DnsResponseException>(() => lookup.Query(new DnsQuestion("test.com", QueryType.A, QueryClass.IN)));

            Assert.Single(calledIps);
            Assert.Contains("truncated and UseTcpFallback is disabled", result.Message);
        }

        [Fact]
        public async Task DnsResponseParseException_FailWithoutTcpFallback_Async()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false,
                UseCache = true,
                Retries = 3,
                UseRandomNameServer = false,
                UseTcpFallback = false
            };

            var calledIps = new List<IPAddress>();
            var udpMessageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);

                var handle = new DnsUdpMessageHandler(true);
                return handle.GetResponseMessage(new ArraySegment<byte>(s_issue52data));
            });

            var lookup = new LookupClient(options, udpMessageHandler, TestMessageHandler.Tcp);
            var result = await Assert.ThrowsAnyAsync<DnsResponseException>(() => lookup.QueryAsync(new DnsQuestion("test.com", QueryType.A, QueryClass.IN)));

            Assert.Single(calledIps);
            Assert.Contains("truncated and UseTcpFallback is disabled", result.Message);
        }

        [Fact]
        public void DnsResponseParseException_ShouldTryNextServer_ThenThrow()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false,
                UseCache = true,
                Retries = 3,
                UseRandomNameServer = false,
                UseTcpFallback = true // although this is set, the error shoudld be thrown.
            };

            var calledIps = new List<IPAddress>();
            bool calledUdp = false, calledTcp = false;
            var udpMessageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledUdp = true;

                // If the result is a parser error which isn't network related (as to our best guess),
                // The error handling should retry the next server before it falls back to TCP.
                // Any result with a small byte result (<= 512) is treated as network issue. Let's pass in a bigger array...
                throw new DnsResponseParseException("Some error in the middle", new byte[1000], 20, 10);
            });

            var tcpMessageHandler = new TestMessageHandler(DnsMessageHandleType.TCP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledTcp = true;
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderResponseCode.NoError, 0, 0, 0, 0), 0);
            });

            var lookup = new LookupClient(options, udpMessageHandler, tcpMessageHandler);
            var result = Assert.ThrowsAny<DnsResponseParseException>(() => lookup.Query(new DnsQuestion("test.com", QueryType.A, QueryClass.IN)));

            Assert.Contains("1000 bytes available", result.Message);

            Assert.True(calledUdp);
            Assert.False(calledTcp);

            // 3 servers are configured, it should not fallback to TCP after trying each one of them once.
            Assert.Equal(3, calledIps.Count);
        }

        [Fact]
        public async Task DnsResponseParseException_ShouldTryNextServer_ThenThrow_Async()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false,
                UseCache = true,
                Retries = 3,
                UseRandomNameServer = false,
                UseTcpFallback = true // although this is set, the error shoudld be thrown.
            };

            var calledIps = new List<IPAddress>();
            bool calledUdp = false, calledTcp = false;
            var udpMessageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledUdp = true;

                // If the result is a parser error which isn't network related (as to our best guess),
                // The error handling should retry the next server before it falls back to TCP.
                // Any result with a small byte result (<= 512) is treated as network issue. Let's pass in a bigger array...
                throw new DnsResponseParseException("Some error in the middle", new byte[1000], 20, 10);
            });

            var tcpMessageHandler = new TestMessageHandler(DnsMessageHandleType.TCP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledTcp = true;
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderResponseCode.NoError, 0, 0, 0, 0), 0);
            });

            var lookup = new LookupClient(options, udpMessageHandler, tcpMessageHandler);
            var result = await Assert.ThrowsAnyAsync<DnsResponseParseException>(() => lookup.QueryAsync(new DnsQuestion("test.com", QueryType.A, QueryClass.IN)));

            Assert.Contains("1000 bytes available", result.Message);

            Assert.True(calledUdp);
            Assert.False(calledTcp);

            // 3 servers are configured, it should not fallback to TCP after trying each one of them once.
            Assert.Equal(3, calledIps.Count);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 3)]
        [InlineData(2, 1)]
        [InlineData(3, 1)]
        [InlineData(4, 4)]
        public void DnsDnsXidMismatchException_ShouldRetry_ThenThrow(int serversCount, int retriesCount)
        {
            var nameServers = Enumerable.Range(1, serversCount)
                .Select(i => new NameServer(IPAddress.Parse($"127.0.10.{i}")))
                .ToArray();

            var options = new LookupClientOptions(nameServers)
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false,
                UseCache = true,
                Retries = retriesCount,
                UseRandomNameServer = false,
                UseTcpFallback = false
            };

            var calledIps = new List<IPAddress>();
            var udpMessageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);

                throw new DnsXidMismatchException(req.Header.Id, req.Header.Id + 1);
            });

            var lookup = new LookupClient(options, udpHandler: udpMessageHandler);
            var result = Assert.ThrowsAny<DnsXidMismatchException>(() => lookup.Query(new DnsQuestion("test.com", QueryType.SRV, QueryClass.IN)));

            var expectedIps = nameServers
                .SelectMany(ns => Enumerable.Repeat(ns.IPEndPoint.Address, retriesCount + 1))
                .ToArray();

            Assert.Equal(expectedIps, calledIps);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 3)]
        [InlineData(2, 1)]
        [InlineData(3, 1)]
        [InlineData(4, 4)]
        public async Task DnsDnsXidMismatchException_ShouldRetry_ThenThrow_Async(int serversCount, int retriesCount)
        {
            var nameServers = Enumerable.Range(1, serversCount)
                .Select(i => new NameServer(IPAddress.Parse($"127.0.10.{i}")))
                .ToArray();

            var options = new LookupClientOptions(nameServers)
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false,
                UseCache = true,
                Retries = retriesCount,
                UseRandomNameServer = false,
                UseTcpFallback = false
            };

            var calledIps = new List<IPAddress>();
            var udpMessageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);

                throw new DnsXidMismatchException(req.Header.Id, req.Header.Id + 1);
            });

            var lookup = new LookupClient(options, udpHandler: udpMessageHandler);
            var result = await Assert.ThrowsAnyAsync<DnsXidMismatchException>(() => lookup.QueryAsync(new DnsQuestion("test.com", QueryType.SRV, QueryClass.IN)));

            var expectedIps = nameServers
                .SelectMany(ns => Enumerable.Repeat(ns.IPEndPoint.Address, retriesCount + 1))
                .ToArray();

            Assert.Equal(expectedIps, calledIps);
        }

        /* Normal truncated response (TC flag) */

        [Fact]
        public void TruncatedResponse_ShouldTryTcp()
        {
            var ip1 = IPAddress.Parse("127.0.10.1");
            var ip2 = IPAddress.Parse("127.0.10.2");
            var ip3 = IPAddress.Parse("127.0.10.3");
            var options = new LookupClientOptions(
                new NameServer(ip1),
                new NameServer(ip2),
                new NameServer(ip3))
            {
                ContinueOnEmptyResponse = false,
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false,
                UseCache = true,
                Retries = 3,
                UseRandomNameServer = false,
                UseTcpFallback = true
            };

            var calledIps = new List<IPAddress>();
            bool calledUdp = false, calledTcp = false;
            var udpMessageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledUdp = true;
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderFlag.ResultTruncated, 0, 0, 0, 0), 0);
            });

            var tcpMessageHandler = new TestMessageHandler(DnsMessageHandleType.TCP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledTcp = true;
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderResponseCode.NoError, 0, 0, 0, 0), 0);
            });

            var lookup = new LookupClient(options, udpMessageHandler, tcpMessageHandler);
            var result = lookup.Query(new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

            Assert.False(result.HasError);

            Assert.True(calledUdp);
            Assert.True(calledTcp);

            // The TCP handler doesn't return an asnwer, but ContinueOnEmptyResponse is false, => should only call it once.
            Assert.Equal(2, calledIps.Count);
            Assert.Equal(new[] { ip1, ip1 }, calledIps);
        }

        [Fact]
        public async Task TruncatedResponse_ShouldTryTcp_Async()
        {
            var ip1 = IPAddress.Parse("127.0.10.1");
            var ip2 = IPAddress.Parse("127.0.10.2");
            var ip3 = IPAddress.Parse("127.0.10.3");
            var options = new LookupClientOptions(
                new NameServer(ip1),
                new NameServer(ip2),
                new NameServer(ip3))
            {
                ContinueOnEmptyResponse = false,
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false,
                UseCache = true,
                Retries = 3,
                UseRandomNameServer = false,
                UseTcpFallback = true
            };

            var calledIps = new List<IPAddress>();
            bool calledUdp = false, calledTcp = false;
            var udpMessageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledUdp = true;
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderFlag.ResultTruncated, 0, 0, 0, 0), 0);
            });

            var tcpMessageHandler = new TestMessageHandler(DnsMessageHandleType.TCP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledTcp = true;
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderResponseCode.NoError, 0, 0, 0, 0), 0);
            });

            var lookup = new LookupClient(options, udpMessageHandler, tcpMessageHandler);
            var result = await lookup.QueryAsync(new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

            Assert.False(result.HasError);

            Assert.True(calledUdp);
            Assert.True(calledTcp);

            Assert.Equal(2, calledIps.Count);
            Assert.Equal(new[] { ip1, ip1 }, calledIps);
        }

        [Fact]
        public void TruncatedResponse_ShouldTryTcp_AndFail()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false,
                UseCache = true,
                Retries = 3,
                UseRandomNameServer = false,
                UseTcpFallback = false // Disabling this will produce an error when we have to fall back to TCP => expected
            };

            var calledIps = new List<IPAddress>();
            var udpMessageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderFlag.ResultTruncated, 0, 0, 0, 0), 0);
            });

            var tcpMessageHandler = new TestMessageHandler(DnsMessageHandleType.TCP, (ip, req) =>
            {
                throw new NotImplementedException("This shouldn't get called");
            });

            var lookup = new LookupClient(options, udpMessageHandler, tcpMessageHandler);
            var result = Assert.ThrowsAny<DnsResponseException>(() => lookup.Query(new DnsQuestion("test.com", QueryType.A, QueryClass.IN)));

            // This should fail right away because there is no need to ask other servers for the same truncated response
            Assert.Single(calledIps);
            Assert.Contains("Response was truncated and UseTcpFallback is disabled", result.Message);
        }

        [Fact]
        public async Task TruncatedResponse_ShouldTryTcp_AndFail_Async()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false,
                UseCache = true,
                Retries = 3,
                UseRandomNameServer = false,
                UseTcpFallback = false // Disabling this will produce an error when we have to fall back to TCP => expected
            };

            var calledIps = new List<IPAddress>();
            var udpMessageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderFlag.ResultTruncated, 0, 0, 0, 0), 0);
            });

            var tcpMessageHandler = new TestMessageHandler(DnsMessageHandleType.TCP, (ip, req) =>
            {
                throw new NotImplementedException("This shouldn't get called");
            });

            var lookup = new LookupClient(options, udpMessageHandler, tcpMessageHandler);
            var result = await Assert.ThrowsAnyAsync<DnsResponseException>(() => lookup.QueryAsync(new DnsQuestion("test.com", QueryType.A, QueryClass.IN)));

            // This should fail right away because there is no need to ask other servers for the same truncated response
            Assert.Single(calledIps);
            Assert.Contains("Response was truncated and UseTcpFallback is disabled", result.Message);
        }

        /* transient timeout errors */

        [Fact]
        public void TransientTimeoutExceptions_ShouldRetry()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false,
                UseCache = true,
                Retries = 3,
                UseRandomNameServer = false,
                UseTcpFallback = true
            };

            var calledIps = new List<IPAddress>();
            var count = 0;
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                count++;
                if (count == 1)
                {
                    throw new TimeoutException();
                }

                throw new OperationCanceledException();
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var result = Assert.ThrowsAny<DnsResponseException>(() => lookup.Query(new DnsQuestion("test.com", QueryType.A, QueryClass.IN)));

            Assert.NotNull(result.InnerException);
            Assert.IsType<OperationCanceledException>(result.InnerException);
            Assert.Equal(12, calledIps.Count);
        }

        [Fact]
        public async Task TransientTimeoutExceptions_ShouldRetry_Async()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false,
                UseCache = true,
                Retries = 3,
                UseRandomNameServer = false,
                UseTcpFallback = true
            };

            var calledIps = new List<IPAddress>();
            var count = 0;
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                count++;
                if (count == 1)
                {
                    throw new TimeoutException();
                }

                throw new OperationCanceledException();
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var result = await Assert.ThrowsAnyAsync<DnsResponseException>(() => lookup.QueryAsync(new DnsQuestion("test.com", QueryType.A, QueryClass.IN)));

            Assert.NotNull(result.InnerException);
            Assert.IsType<OperationCanceledException>(result.InnerException);

            // 3 servers x (1 + 3 re-tries)
            Assert.Equal(12, calledIps.Count);
        }

        /* transient SocketExceptions

            case SocketError.TimedOut:
            case SocketError.ConnectionAborted:
            case SocketError.ConnectionReset:
            case SocketError.OperationAborted:
            case SocketError.TryAgain:
         * */

        [Fact]
        public void TransientSocketExceptions_ShouldRetry()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")),
                new NameServer(IPAddress.Parse("127.0.10.4")),
                new NameServer(IPAddress.Parse("127.0.10.5")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false,
                UseCache = true,
                Retries = 3,
                UseRandomNameServer = false,
                UseTcpFallback = true
            };

            var calledIps = new List<IPAddress>();
            var count = 0;
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                count++;
                if (count == 1)
                {
                    throw new SocketException((int)SocketError.TimedOut);
                }
                if (count == 2)
                {
                    throw new SocketException((int)SocketError.ConnectionAborted);
                }
                if (count == 3)
                {
                    throw new SocketException((int)SocketError.ConnectionReset);
                }
                if (count == 4)
                {
                    throw new SocketException((int)SocketError.OperationAborted);
                }

                throw new SocketException((int)SocketError.TryAgain);
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var result = Assert.ThrowsAny<DnsResponseException>(() => lookup.Query(new DnsQuestion("test.com", QueryType.A, QueryClass.IN)));

            Assert.Equal(SocketError.TryAgain, ((SocketException)result.InnerException).SocketErrorCode);

            Assert.Equal(20, calledIps.Count);
        }

        [Fact]
        public async Task TransientSocketExceptions_ShouldRetry_Async()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")),
                new NameServer(IPAddress.Parse("127.0.10.4")),
                new NameServer(IPAddress.Parse("127.0.10.5")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false,
                UseCache = true,
                Retries = 3,
                UseRandomNameServer = false,
                UseTcpFallback = true
            };

            var calledIps = new List<IPAddress>();
            var count = 0;
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                count++;
                if (count == 1)
                {
                    throw new SocketException((int)SocketError.TimedOut);
                }
                if (count == 2)
                {
                    throw new SocketException((int)SocketError.ConnectionAborted);
                }
                if (count == 3)
                {
                    throw new SocketException((int)SocketError.ConnectionReset);
                }
                if (count == 4)
                {
                    throw new SocketException((int)SocketError.OperationAborted);
                }

                throw new SocketException((int)SocketError.TryAgain);
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var result = await Assert.ThrowsAnyAsync<DnsResponseException>(() => lookup.QueryAsync(new DnsQuestion("test.com", QueryType.A, QueryClass.IN)));

            Assert.Equal(SocketError.TryAgain, ((SocketException)result.InnerException).SocketErrorCode);

            // 5 servers x (1 + 3 re-tries)
            Assert.Equal(20, calledIps.Count);
        }

        /* Other exceptions like SocketExceptions which we should re-try on the next server... */

        [Fact]
        public void OtherExceptions_ShouldTryNextServer()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")),
                new NameServer(IPAddress.Parse("127.0.10.4")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false,
                UseCache = true,
                Retries = 3,
                UseRandomNameServer = false,
                UseTcpFallback = true
            };

            var calledIps = new List<IPAddress>();
            var count = 0;
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                count++;
                if (count == 1)
                {
                    // Trying some non-transient socket exceptions to make sure those do not get retried...
                    throw new SocketException((int)SocketError.AddressFamilyNotSupported);
                }
                if (count == 2)
                {
                    throw new SocketException((int)SocketError.ConnectionRefused);
                }
                if (count == 3)
                {
                    throw new FormatException("Some random exception we didn't expect.");
                }

                throw new SocketException((int)SocketError.SocketError);
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var result = Assert.ThrowsAny<DnsResponseException>(() => lookup.Query(new DnsQuestion("test.com", QueryType.A, QueryClass.IN)));

            Assert.Equal(SocketError.SocketError, ((SocketException)result.InnerException).SocketErrorCode);

            Assert.Equal(4, calledIps.Count);
        }

        [Fact]
        public async Task OtherExceptions_ShouldTryNextServer_Async()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")),
                new NameServer(IPAddress.Parse("127.0.10.4")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = false,
                UseCache = true,
                Retries = 3,
                UseRandomNameServer = false,
                UseTcpFallback = true
            };

            var calledIps = new List<IPAddress>();
            var count = 0;
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                count++;
                if (count == 1)
                {
                    // Trying some non-transient socket exceptions to make sure those do not get retried...
                    throw new SocketException((int)SocketError.AddressFamilyNotSupported);
                }
                if (count == 2)
                {
                    throw new SocketException((int)SocketError.ConnectionRefused);
                }
                if (count == 3)
                {
                    throw new FormatException("Some random exception we didn't expect.");
                }

                throw new SocketException((int)SocketError.SocketError);
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var result = await Assert.ThrowsAnyAsync<DnsResponseException>(() => lookup.QueryAsync(new DnsQuestion("test.com", QueryType.A, QueryClass.IN)));

            Assert.Equal(SocketError.SocketError, ((SocketException)result.InnerException).SocketErrorCode);

            Assert.Equal(4, calledIps.Count);
        }

        /* Not handled exceptions. Should be thrown */

        [Fact]
        public void ArgumentExceptions_ShouldNotBeHandled()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")));

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                throw new ArgumentNullException("Some random argument exception");
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var result = Assert.ThrowsAny<ArgumentException>(() => lookup.Query(new DnsQuestion("test.com", QueryType.A, QueryClass.IN)));

            Assert.Single(calledIps);
        }

        [Fact]
        public async Task ArgumentExceptions_ShouldNotBeHandled_Async()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")));

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                throw new ArgumentNullException("Some random argument exception");
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var result = await Assert.ThrowsAnyAsync<ArgumentException>(() => lookup.QueryAsync(new DnsQuestion("test.com", QueryType.A, QueryClass.IN)));

            Assert.Single(calledIps);
        }

        [Fact]
        public void InvalidOperationExceptions_ShouldNotBeHandled()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")));

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                throw new InvalidOperationException("Some random argument exception");
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var result = Assert.ThrowsAny<InvalidOperationException>(() => lookup.Query(new DnsQuestion("test.com", QueryType.A, QueryClass.IN)));

            Assert.Single(calledIps);
        }

        [Fact]
        public async Task InvalidOperationExceptions_ShouldNotBeHandled_Async()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.10.1")),
                new NameServer(IPAddress.Parse("127.0.10.2")),
                new NameServer(IPAddress.Parse("127.0.10.3")));

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler(DnsMessageHandleType.UDP, (ip, req) =>
            {
                calledIps.Add(ip.Address);
                throw new InvalidOperationException("Some random argument exception");
            });

            var lookup = new LookupClient(options, messageHandler, TestMessageHandler.Tcp);
            var result = await Assert.ThrowsAnyAsync<InvalidOperationException>(() => lookup.QueryAsync(new DnsQuestion("test.com", QueryType.A, QueryClass.IN)));

            Assert.Single(calledIps);
        }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal class TestMessageHandler : DnsMessageHandler
    {
        public static readonly TestMessageHandler Udp = new TestMessageHandler(DnsMessageHandleType.UDP, (a, b) => null);
        public static readonly TestMessageHandler Tcp = new TestMessageHandler(DnsMessageHandleType.TCP, (a, b) => null);

        private readonly Func<IPEndPoint, DnsRequestMessage, DnsResponseMessage> _onQuery;

        public override DnsMessageHandleType Type { get; }

        public TestMessageHandler(DnsMessageHandleType type, Func<IPEndPoint, DnsRequestMessage, DnsResponseMessage> onQuery)
        {
            Type = type;
            _onQuery = onQuery ?? throw new ArgumentNullException(nameof(onQuery));
        }

        public override DnsResponseMessage Query(IPEndPoint endpoint, DnsRequestMessage request, TimeSpan timeout)
        {
            return _onQuery(endpoint, request);
        }

        public override Task<DnsResponseMessage> QueryAsync(IPEndPoint endpoint, DnsRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_onQuery(endpoint, request));
        }
    }
}
