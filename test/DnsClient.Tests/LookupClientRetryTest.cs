using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
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

            var lookup = new LookupClient(options);

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(
                    new DnsResponseHeader(req.Header.Id, (int)DnsResponseCode.NoError, 0, 0, 0, 0),
                    0);
            });

            // calling multiple times will always use the first server when it returns NoError.
            // No other servers should be called
            for (var i = 0; i < 3; i++)
            {
                var request = new DnsRequestMessage(
                   new DnsRequestHeader(DnsOpCode.Query),
                   new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

                var servers = lookup.Settings.ShuffleNameServers();

                var result = lookup.ResolveQuery(servers, lookup.Settings, messageHandler, request);
                Assert.False(result.HasError);
            }

            Assert.Equal(3, calledIps.Count);
            Assert.Equal(IPAddress.Parse("127.0.10.1"), calledIps[0]);
            Assert.Equal(IPAddress.Parse("127.0.10.1"), calledIps[1]);
            Assert.Equal(IPAddress.Parse("127.0.10.1"), calledIps[2]);
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
            var messageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(
                    new DnsResponseHeader(req.Header.Id, (int)DnsResponseCode.NoError, 0, 0, 0, 0),
                    0);
            });

            var lookup = new LookupClient(options, messageHandler, messageHandler);

            // calling multiple times will always use the first server when it returns NoError.
            // No other servers should be called
            for (var i = 0; i < 3; i++)
            {
                var servers = lookup.Settings.ShuffleNameServers();

                var result = await lookup.QueryAsync(new DnsQuestion("test.com", QueryType.A, QueryClass.IN));
                Assert.False(result.HasError);
            }

            Assert.Equal(3, calledIps.Count);
            Assert.Equal(IPAddress.Parse("127.0.10.1"), calledIps[0]);
            Assert.Equal(IPAddress.Parse("127.0.10.1"), calledIps[1]);
            Assert.Equal(IPAddress.Parse("127.0.10.1"), calledIps[2]);
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

            var lookup = new LookupClient(options);

            var calledIps = new List<IPAddress>();
            var uniqueIps = new HashSet<IPAddress>();
            var messageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);
                uniqueIps.Add(ip.Address);
                return new DnsResponseMessage(
                    new DnsResponseHeader(req.Header.Id, (int)DnsResponseCode.BadAlgorithm, 0, 0, 0, 0),
                    0);
            });

            // consecutive calls should cycle through the servers (not always using the first one)
            // because this is only one thread, the order is always the same. But this can be very
            // different with multiple tasks)
            for (var i = 0; i < 6; i++)
            {
                var request = new DnsRequestMessage(
                   new DnsRequestHeader(DnsOpCode.Query),
                   new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

                var servers = lookup.Settings.ShuffleNameServers();

                var result = lookup.ResolveQuery(servers, lookup.Settings, messageHandler, request);
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

            var lookup = new LookupClient(options);

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(
                    new DnsResponseHeader(req.Header.Id, (int)DnsResponseCode.BadAlgorithm, 0, 0, 0, 0),
                    0);
            });

            // consecutive calls should cycle through the servers (not always using the first one)
            // because this is only one thread, the order is always the same. But this can be very
            // different with multiple tasks)
            for (var i = 0; i < 6; i++)
            {
                var request = new DnsRequestMessage(
                   new DnsRequestHeader(DnsOpCode.Query),
                   new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

                var servers = lookup.Settings.ShuffleNameServers();

                var result = await lookup.ResolveQueryAsync(servers, lookup.Settings, request, messageHandler);
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
            var messageHandler = new TestMessageHandler((ip, req) =>
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

            var lookup = new LookupClient(options, messageHandler, messageHandler);
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
            var messageHandler = new TestMessageHandler((ip, req) =>
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

            var lookup = new LookupClient(options, messageHandler, messageHandler);
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
            var messageHandler = new TestMessageHandler((ip, req) =>
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

            var lookup = new LookupClient(options, messageHandler, messageHandler);
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
            var messageHandler = new TestMessageHandler((ip, req) =>
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

            var lookup = new LookupClient(options, messageHandler, messageHandler);
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
            var messageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderResponseCode.NotExistentDomain, 0, 0, 0, 0), 0);
            });

            var lookup = new LookupClient(options, messageHandler, messageHandler);
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
            var messageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderResponseCode.NotExistentDomain, 0, 0, 0, 0), 0);
            });

            var lookup = new LookupClient(options, messageHandler, messageHandler);
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
            var messageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderResponseCode.NotExistentDomain, 0, 0, 0, 0), 0);
            });

            var lookup = new LookupClient(options, messageHandler, messageHandler);
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
            var messageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderResponseCode.NotExistentDomain, 0, 0, 0, 0), 0);
            });

            var lookup = new LookupClient(options, messageHandler, messageHandler);
            var request = new DnsQuestion("test.com", QueryType.A, QueryClass.IN);
            var result = await Assert.ThrowsAnyAsync<DnsResponseException>(() => lookup.QueryAsync(request));

            Assert.Equal(DnsResponseCode.NotExistentDomain, result.Code);
            Assert.Single(calledIps);
        }

        /* DNS response parse error handling and retries */

        // https://github.com/MichaCo/DnsClient.NET/issues/52
        [Fact]
        public void DnsResponseParseException_ShouldTryTcp_Issue52()
        {
            var ip1 = IPAddress.Parse("127.0.10.1");
            var options = new LookupClientOptions(
                new NameServer(ip1),
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
            bool calledUdp = false, calledTcp = false;
            var udpMessageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledUdp = true;

                // A result which didn't fit into 512 bytes and which was not marked via TC as truncated.
                // The server returned 512 bytes because the response was truncated by a firewall or so.
                // This should retry the full request via TCP
                var handle = new DnsUdpMessageHandler(true);
                return handle.GetResponseMessage(new System.ArraySegment<byte>(s_issue52data));
            });

            var tcpMessageHandler = new TestMessageHandler((ip, req) =>
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

            // Although 3 servers are configured, it should fallback to TCP right away and call the first server twice.
            Assert.Equal(2, calledIps.Count);
            Assert.Equal(new[] { ip1, ip1 }, calledIps);
        }

        [Fact]
        public async Task DnsResponseParseException_ShouldTryTcp_Issue52_Async()
        {
            var ip1 = IPAddress.Parse("127.0.10.1");
            var options = new LookupClientOptions(
                new NameServer(ip1),
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
            bool calledUdp = false, calledTcp = false;
            var udpMessageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledUdp = true;

                // A result which didn't fit into 512 bytes and which was not marked via TC as truncated.
                // The server returned 512 bytes because the response was truncated by a firewall or so.
                // This should retry the full request via TCP
                var handle = new DnsUdpMessageHandler(true);
                return handle.GetResponseMessage(new System.ArraySegment<byte>(s_issue52data));
            });

            var tcpMessageHandler = new TestMessageHandler((ip, req) =>
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

            // Although 3 servers are configured, it should fallback to TCP right away and call the first server twice.
            Assert.Equal(2, calledIps.Count);
            Assert.Equal(new[] { ip1, ip1 }, calledIps);
        }

        [Fact]
        public void DnsResponseParseException_ShouldTryTcp_LargerResponse()
        {
            var ip1 = IPAddress.Parse("127.0.10.1");
            var options = new LookupClientOptions(
                new NameServer(ip1),
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
            bool calledUdp = false, calledTcp = false;
            var udpMessageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledUdp = true;

                // This should retry the full request via TCP.
                throw new DnsResponseParseException("Some error at the end", new byte[1000], 995, 10);
            });

            var tcpMessageHandler = new TestMessageHandler((ip, req) =>
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

            // Although 3 servers are configured, it should fallback to TCP right away and call the first server twice.
            Assert.Equal(2, calledIps.Count);
            Assert.Equal(new[] { ip1, ip1 }, calledIps);
        }

        [Fact]
        public async Task DnsResponseParseException_ShouldTryTcp_LargerResponse_Async()
        {
            var ip1 = IPAddress.Parse("127.0.10.1");
            var options = new LookupClientOptions(
                new NameServer(ip1),
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
            bool calledUdp = false, calledTcp = false;
            var udpMessageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledUdp = true;

                // This should retry the full request via TCP.
                throw new DnsResponseParseException("Some error at the end", new byte[1000], 995, 10);
            });

            var tcpMessageHandler = new TestMessageHandler((ip, req) =>
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

            // Although 3 servers are configured, it should fallback to TCP right away and call the first server twice.
            Assert.Equal(2, calledIps.Count);
            Assert.Equal(new[] { ip1, ip1 }, calledIps);
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
            var udpMessageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);

                var handle = new DnsUdpMessageHandler(true);
                return handle.GetResponseMessage(new System.ArraySegment<byte>(s_issue52data));
            });

            var lookup = new LookupClient(options, udpMessageHandler, udpMessageHandler);
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
            var udpMessageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);

                var handle = new DnsUdpMessageHandler(true);
                return handle.GetResponseMessage(new System.ArraySegment<byte>(s_issue52data));
            });

            var lookup = new LookupClient(options, udpMessageHandler, udpMessageHandler);
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
            var udpMessageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledUdp = true;

                // If the result is a parser error which isn't network related (as to our best guess),
                // The error handling should retry the next server before it falls back to TCP.
                // Any result with a small byte result (<= 512) is treated as network issue. Let's pass in a bigger array...
                throw new DnsResponseParseException("Some error in the middle", new byte[1000], 20, 10);
            });

            var tcpMessageHandler = new TestMessageHandler((ip, req) =>
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
            var udpMessageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledUdp = true;

                // If the result is a parser error which isn't network related (as to our best guess),
                // The error handling should retry the next server before it falls back to TCP.
                // Any result with a small byte result (<= 512) is treated as network issue. Let's pass in a bigger array...
                throw new DnsResponseParseException("Some error in the middle", new byte[1000], 20, 10);
            });

            var tcpMessageHandler = new TestMessageHandler((ip, req) =>
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

        /* Normal truncated response (TC flag) */

        [Fact]
        public void TruncatedResponse_ShouldTryTcp()
        {
            var ip1 = IPAddress.Parse("127.0.10.1");
            var options = new LookupClientOptions(
                new NameServer(ip1),
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
            bool calledUdp = false, calledTcp = false;
            var udpMessageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledUdp = true;
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderFlag.ResultTruncated, 0, 0, 0, 0), 0);
            });

            var tcpMessageHandler = new TestMessageHandler((ip, req) =>
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

            // Although 3 servers are configured, it should fallback to TCP right away and call the first server twice.
            Assert.Equal(2, calledIps.Count);
            Assert.Equal(new[] { ip1, ip1 }, calledIps);
        }

        [Fact]
        public async Task TruncatedResponse_ShouldTryTcp_Async()
        {
            var ip1 = IPAddress.Parse("127.0.10.1");
            var options = new LookupClientOptions(
                new NameServer(ip1),
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
            bool calledUdp = false, calledTcp = false;
            var udpMessageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);
                calledUdp = true;
                return new DnsResponseMessage(new DnsResponseHeader(req.Header.Id, (int)DnsHeaderFlag.ResultTruncated, 0, 0, 0, 0), 0);
            });

            var tcpMessageHandler = new TestMessageHandler((ip, req) =>
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

            // Although 3 servers are configured, it should fallback to TCP right away and call the first server twice.
            Assert.Equal(2, calledIps.Count);
            Assert.Equal(new[] { ip1, ip1 }, calledIps);
        }
    }

    [ExcludeFromCodeCoverage]
    internal class TestMessageHandler : DnsMessageHandler
    {
        private readonly Func<IPEndPoint, DnsRequestMessage, DnsResponseMessage> _onQuery;

        public TestMessageHandler(Func<IPEndPoint, DnsRequestMessage, DnsResponseMessage> onQuery)
        {
            _onQuery = onQuery ?? throw new ArgumentNullException(nameof(onQuery));
        }

        public override bool IsTransientException<T>(T exception)
        {
            return false;
        }

        public override DnsResponseMessage Query(IPEndPoint endpoint, DnsRequestMessage request, TimeSpan timeout)
        {
            return _onQuery(endpoint, request);
        }

        public override Task<DnsResponseMessage> QueryAsync(IPEndPoint endpoint, DnsRequestMessage request, CancellationToken cancellationToken, Action<Action> cancelationCallback)
        {
            return Task.FromResult(_onQuery(endpoint, request));
        }
    }
}