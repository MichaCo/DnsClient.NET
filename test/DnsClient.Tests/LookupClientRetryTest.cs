﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DnsClient.Tests
{
    public class LookupClientRetryTest
    {
        [Fact]
        public void Lookup_PreserveNameServerOrder()
        {
            var options = new LookupClientOptions(
                   new NameServer(IPAddress.Parse("127.0.0.1")),
                   new NameServer(IPAddress.Parse("127.0.0.2")),
                   new NameServer(IPAddress.Parse("127.0.0.3")))
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
                    new DnsResponseHeader(1, (int)DnsResponseCode.NoError, 0, 0, 0, 0),
                    0);
            });

            // calling multiple times will always use the first server when it returns NoError.
            // No other servers should be called
            for (var i = 0; i < 3; i++)
            {
                var request = new DnsRequestMessage(
                   new DnsRequestHeader(i, DnsOpCode.Query),
                   new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

                var servers = lookup.Settings.ShuffleNameServers();

                var result = lookup.ResolveQuery(servers, lookup.Settings, messageHandler, request);
                Assert.False(result.HasError);
            }

            Assert.Equal(3, calledIps.Count);
            Assert.Equal(IPAddress.Parse("127.0.0.1"), calledIps[0]);
            Assert.Equal(IPAddress.Parse("127.0.0.1"), calledIps[1]);
            Assert.Equal(IPAddress.Parse("127.0.0.1"), calledIps[2]);
        }

        [Fact]
        public async Task Lookup_PreserveNameServerOrderAsync()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.0.1")),
                new NameServer(IPAddress.Parse("127.0.0.2")),
                new NameServer(IPAddress.Parse("127.0.0.3")))
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
                    new DnsResponseHeader(1, (int)DnsResponseCode.NoError, 0, 0, 0, 0),
                    0);
            });

            // calling multiple times will always use the first server when it returns NoError.
            // No other servers should be called
            for (var i = 0; i < 3; i++)
            {
                var request = new DnsRequestMessage(
                   new DnsRequestHeader(i, DnsOpCode.Query),
                   new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

                var servers = lookup.Settings.ShuffleNameServers();

                var result = await lookup.ResolveQueryAsync(servers, lookup.Settings, messageHandler, request);
                Assert.False(result.HasError);
            }

            Assert.Equal(3, calledIps.Count);
            Assert.Equal(IPAddress.Parse("127.0.0.1"), calledIps[0]);
            Assert.Equal(IPAddress.Parse("127.0.0.1"), calledIps[1]);
            Assert.Equal(IPAddress.Parse("127.0.0.1"), calledIps[2]);
        }

        [Fact]
        public void Lookup_ShuffleNameServerOrder()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.0.1")),
                new NameServer(IPAddress.Parse("127.0.0.2")),
                new NameServer(IPAddress.Parse("127.0.0.3")))
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
                    new DnsResponseHeader(1, (int)DnsResponseCode.BadAlgorithm, 0, 0, 0, 0),
                    0);
            });

            // consecutive calls should cycle through the servers (not always using the first one)
            // because this is only one thread, the order is always the same. But this can be very
            // different with multiple tasks)
            for (var i = 0; i < 6; i++)
            {
                var request = new DnsRequestMessage(
                   new DnsRequestHeader(i, DnsOpCode.Query),
                   new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

                var servers = lookup.Settings.ShuffleNameServers();

                var result = lookup.ResolveQuery(servers, lookup.Settings, messageHandler, request);
                Assert.True(result.HasError);
            }

            Assert.Equal(18, calledIps.Count);
            Assert.Equal(3, uniqueIps.Count);
        }

        [Fact]
        public async Task Lookup_ShuffleNameServerOrderAsync()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.0.1")),
                new NameServer(IPAddress.Parse("127.0.0.2")),
                new NameServer(IPAddress.Parse("127.0.0.3")))
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
                    new DnsResponseHeader(1, (int)DnsResponseCode.BadAlgorithm, 0, 0, 0, 0),
                    0);
            });

            // consecutive calls should cycle through the servers (not always using the first one)
            // because this is only one thread, the order is always the same. But this can be very
            // different with multiple tasks)
            for (var i = 0; i < 6; i++)
            {
                var request = new DnsRequestMessage(
                   new DnsRequestHeader(i, DnsOpCode.Query),
                   new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

                var servers = lookup.Settings.ShuffleNameServers();

                var result = await lookup.ResolveQueryAsync(servers, lookup.Settings, messageHandler, request);
                Assert.True(result.HasError);
            }

            Assert.Equal(18, calledIps.Count);
        }

        [Fact]
        public void Lookup_RetryOnDnsError()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.0.1")),
                new NameServer(IPAddress.Parse("127.0.0.2")),
                new NameServer(IPAddress.Parse("127.0.0.3")),
                new NameServer(IPAddress.Parse("127.0.0.4")),
                new NameServer(IPAddress.Parse("127.0.0.5")),
                new NameServer(IPAddress.Parse("127.0.0.6")),
                new NameServer(IPAddress.Parse("127.0.0.7")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = true,
                UseCache = false
            };

            var lookup = new LookupClient(options);

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(
                    new DnsResponseHeader(1, (int)DnsResponseCode.NotExistentDomain, 0, 0, 0, 0),
                    0);
            });

            var request = new DnsRequestMessage(
                new DnsRequestHeader(0, DnsOpCode.Query),
                new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

            var servers = lookup.Settings.ShuffleNameServers();
            var result = lookup.ResolveQuery(servers, lookup.Settings, messageHandler, request);

            // no exception but error in the response after calling all endpoints!
            Assert.True(result.HasError);
            Assert.Equal(DnsResponseCode.NotExistentDomain, result.Header.ResponseCode);

            // ensure the client tried all the configured endpoints
            Assert.Equal(7, calledIps.Count);
        }

        [Fact]
        public async Task Lookup_RetryOnDnsErrorAsync()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.0.1")),
                new NameServer(IPAddress.Parse("127.0.0.2")),
                new NameServer(IPAddress.Parse("127.0.0.3")),
                new NameServer(IPAddress.Parse("127.0.0.4")),
                new NameServer(IPAddress.Parse("127.0.0.5")),
                new NameServer(IPAddress.Parse("127.0.0.6")),
                new NameServer(IPAddress.Parse("127.0.0.7")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = true,
                UseCache = false
            };

            var lookup = new LookupClient(options);

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(
                    new DnsResponseHeader(1, (int)DnsResponseCode.NotExistentDomain, 0, 0, 0, 0),
                    0);
            });

            var request = new DnsRequestMessage(
                new DnsRequestHeader(0, DnsOpCode.Query),
                new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

            var servers = lookup.Settings.ShuffleNameServers();
            var result = await lookup.ResolveQueryAsync(servers, lookup.Settings, messageHandler, request);

            // no exception but error in the response after calling all 3 endpoints!
            Assert.True(result.HasError);
            Assert.Equal(DnsResponseCode.NotExistentDomain, result.Header.ResponseCode);

            // ensure the client tried all the configured endpoints
            Assert.Equal(7, calledIps.Count);
        }

        [Fact]
        public void Lookup_RetryOnDnsErrorAndThrow()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.0.1")),
                new NameServer(IPAddress.Parse("127.0.0.2")),
                new NameServer(IPAddress.Parse("127.0.0.3")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = true,
                ThrowDnsErrors = true,
                UseCache = false,
                UseRandomNameServer = false
            };

            var lookup = new LookupClient(options);

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);

                if (ip.Address.Equals(IPAddress.Parse("127.0.0.3")))
                {
                    // last one returns different result
                    return new DnsResponseMessage(
                        new DnsResponseHeader(1, (int)DnsResponseCode.FormatError, 0, 0, 0, 0),
                        0);
                }

                return new DnsResponseMessage(
                    new DnsResponseHeader(1, (int)DnsResponseCode.NotExistentDomain, 0, 0, 0, 0),
                    0);
            });

            var request = new DnsRequestMessage(
                new DnsRequestHeader(0, DnsOpCode.Query),
                new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

            // all three servers have been called and we get the last exception thrown
            var result = Assert.ThrowsAny<DnsResponseException>(() => lookup.ResolveQuery(lookup.Settings.NameServers, lookup.Settings, messageHandler, request));

            // ensure the error is the one from the last call
            Assert.Equal(DnsResponseCode.FormatError, result.Code);

            // ensuer all 3 configured servers were called before throwing the exception
            Assert.Equal(3, calledIps.Count);
        }

        [Fact]
        public async Task Lookup_RetryOnDnsErrorAndThrowAsync()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.0.1")),
                new NameServer(IPAddress.Parse("127.0.0.2")),
                new NameServer(IPAddress.Parse("127.0.0.3")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = true,
                ThrowDnsErrors = true,
                UseRandomNameServer = false
            };

            var lookup = new LookupClient(options);

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);

                if (ip.Address.Equals(IPAddress.Parse("127.0.0.3")))
                {
                    // last one returns different result
                    return new DnsResponseMessage(
                        new DnsResponseHeader(1, (int)DnsResponseCode.FormatError, 0, 0, 0, 0),
                        0);
                }

                return new DnsResponseMessage(
                    new DnsResponseHeader(1, (int)DnsResponseCode.NotExistentDomain, 0, 0, 0, 0),
                    0);
            });

            var request = new DnsRequestMessage(
                new DnsRequestHeader(0, DnsOpCode.Query),
                new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

            // all three servers have been called and we get the last exception thrown
            var servers = lookup.Settings.ShuffleNameServers();
            var result = await Assert.ThrowsAnyAsync<DnsResponseException>(() => lookup.ResolveQueryAsync(servers, lookup.Settings, messageHandler, request));

            // ensure the error is the one from the last call
            Assert.Equal(DnsResponseCode.FormatError, result.Code);

            // ensuer all 3 configured servers were called before throwing the exception
            Assert.Equal(3, calledIps.Count);
        }

        [Fact]
        public void Lookup_NoRetryOnDnsError()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.0.1")),
                new NameServer(IPAddress.Parse("127.0.0.2")),
                new NameServer(IPAddress.Parse("127.0.0.3")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = false // disable retry/continue on dns error
            };

            var lookup = new LookupClient(options);

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(
                    new DnsResponseHeader(1, (int)DnsResponseCode.NotExistentDomain, 0, 0, 0, 0),
                    0);
            });

            var request = new DnsRequestMessage(
                new DnsRequestHeader(0, DnsOpCode.Query),
                new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

            var servers = lookup.Settings.ShuffleNameServers();
            var result = lookup.ResolveQuery(servers, lookup.Settings, messageHandler, request);

            Assert.True(result.HasError);
            Assert.Equal(DnsResponseCode.NotExistentDomain, result.Header.ResponseCode);

            // ensure we got the error right from the first server call
            Assert.Single(calledIps);
        }

        [Fact]
        public async Task Lookup_NoRetryOnDnsErrorAsync()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.0.1")),
                new NameServer(IPAddress.Parse("127.0.0.2")),
                new NameServer(IPAddress.Parse("127.0.0.3")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = false // disable retry/continue on dns error
            };

            var lookup = new LookupClient(options);

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(
                    new DnsResponseHeader(1, (int)DnsResponseCode.NotExistentDomain, 0, 0, 0, 0),
                    0);
            });

            var request = new DnsRequestMessage(
                new DnsRequestHeader(0, DnsOpCode.Query),
                new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

            var servers = lookup.Settings.ShuffleNameServers();
            var result = await lookup.ResolveQueryAsync(servers, lookup.Settings, messageHandler, request);

            Assert.True(result.HasError);
            Assert.Equal(DnsResponseCode.NotExistentDomain, result.Header.ResponseCode);

            // ensure we got the error right from the first server call
            Assert.Single(calledIps);
        }

        [Fact]
        public void Lookup_NoRetryOnDnsErrorAndThrow()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.0.1")),
                new NameServer(IPAddress.Parse("127.0.0.2")),
                new NameServer(IPAddress.Parse("127.0.0.3")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = false, // disable retry/continue on dns error
                ThrowDnsErrors = true // enable throw dns error (should throw the error of the last one
            };

            var lookup = new LookupClient(options);

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);

                return new DnsResponseMessage(
                    new DnsResponseHeader(1, (int)DnsResponseCode.NotExistentDomain, 0, 0, 0, 0),
                    0);
            });

            var request = new DnsRequestMessage(
                new DnsRequestHeader(0, DnsOpCode.Query),
                new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

            var servers = lookup.Settings.ShuffleNameServers();
            var result = Assert.ThrowsAny<DnsResponseException>(() => lookup.ResolveQuery(servers, lookup.Settings, messageHandler, request));

            Assert.Equal(DnsResponseCode.NotExistentDomain, result.Code);

            Assert.Single(calledIps);
        }

        [Fact]
        public async Task Lookup_NoRetryOnDnsErrorAndThrowAsync()
        {
            var options = new LookupClientOptions(
                new NameServer(IPAddress.Parse("127.0.0.1")),
                new NameServer(IPAddress.Parse("127.0.0.2")),
                new NameServer(IPAddress.Parse("127.0.0.3")))
            {
                EnableAuditTrail = true,
                ContinueOnDnsError = false,
                ThrowDnsErrors = true
            };

            var lookup = new LookupClient(options);

            var calledIps = new List<IPAddress>();
            var messageHandler = new TestMessageHandler((ip, req) =>
            {
                calledIps.Add(ip.Address);
                return new DnsResponseMessage(
                    new DnsResponseHeader(1, (int)DnsResponseCode.NotExistentDomain, 0, 0, 0, 0),
                    0);
            });

            var request = new DnsRequestMessage(
                new DnsRequestHeader(0, DnsOpCode.Query),
                new DnsQuestion("test.com", QueryType.A, QueryClass.IN));

            var servers = lookup.Settings.ShuffleNameServers();
            var result = await Assert.ThrowsAnyAsync<DnsResponseException>(() => lookup.ResolveQueryAsync(servers, lookup.Settings, messageHandler, request));

            Assert.Equal(DnsResponseCode.NotExistentDomain, result.Code);

            Assert.Single(calledIps);
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