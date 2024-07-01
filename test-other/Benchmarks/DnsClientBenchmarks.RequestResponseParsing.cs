// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using DnsClient;

namespace Benchmarks
{
    public static partial class DnsClientBenchmarks
    {
        public class RequestResponseParsing
        {
            private const int OpsPerMultiRun = 32;
            private static readonly DnsQuestion s_question = new DnsQuestion("google.com", QueryType.ANY, QueryClass.IN);

            private static readonly LookupClient s_lookup = new LookupClient(
                new LookupClientOptions(IPAddress.Loopback)
                {
                    UseCache = false
                },
                new BenchmarkMessageHandler(),
                new BenchmarkMessageHandler(type: DnsMessageHandleType.TCP));

            public RequestResponseParsing()
            {
            }

            [Benchmark(Baseline = true)]
            public void QuerySync()
            {
                var result = s_lookup.Query(s_question);
                if (result.Answers.Count != 11)
                {
                    throw new InvalidOperationException();
                }
                if (result.Questions.Count != 1)
                {
                    throw new InvalidOperationException();
                }
                if (!result.Questions[0].QueryName.Equals("google.com."))
                {
                    throw new InvalidOperationException();
                }
            }

            [Benchmark]
            public async Task QueryAsync()
            {
                var result = await s_lookup.QueryAsync(s_question).ConfigureAwait(false);
                if (result.Answers.Count != 11)
                {
                    throw new InvalidOperationException();
                }
                if (result.Questions.Count != 1)
                {
                    throw new InvalidOperationException();
                }
                if (!result.Questions[0].QueryName.Equals("google.com."))
                {
                    throw new InvalidOperationException();
                }
            }

            [Benchmark(OperationsPerInvoke = OpsPerMultiRun)]
            public void QuerySyncMulti()
            {
                Parallel.Invoke(Enumerable.Repeat(() => Query(), OpsPerMultiRun).ToArray());

                void Query()
                {
                    var result = s_lookup.Query(s_question);
                    if (result.Answers.Count != 11)
                    {
                        throw new InvalidOperationException();
                    }
                    if (result.Questions.Count != 1)
                    {
                        throw new InvalidOperationException();
                    }
                    if (!result.Questions[0].QueryName.Equals("google.com."))
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            [Benchmark(OperationsPerInvoke = OpsPerMultiRun)]
            public void QueryAsyncMulti()
            {
                Parallel.Invoke(Enumerable.Repeat<Action>(() => Query(), OpsPerMultiRun).ToArray());

                Task Query()
                {
                    var result = s_lookup.QueryAsync(s_question).GetAwaiter().GetResult();
                    if (result.Answers.Count != 11)
                    {
                        throw new InvalidOperationException();
                    }
                    if (result.Questions.Count != 1)
                    {
                        throw new InvalidOperationException();
                    }
                    if (!result.Questions[0].QueryName.Equals("google.com."))
                    {
                        throw new InvalidOperationException();
                    }

                    return Task.CompletedTask;
                }
            }
        }
    }

    [ExcludeFromCodeCoverage]
    internal class BenchmarkMessageHandler : DnsMessageHandler
    {
        //google.com any
        private static readonly byte[] s_questionRaw = new byte[] { 95, 207, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 6, 103, 111, 111, 103, 108, 101, 3, 99, 111, 109, 0, 0, 255, 0, 1, 0, 0, 41, 16, 0, 0, 0, 0, 0, 0, 0 };

        //result
        private static readonly byte[] s_answer = new byte[] { 95, 207, 129, 128, 0, 1, 0, 11, 0, 0, 0, 1, 6, 103, 111, 111, 103, 108, 101, 3, 99, 111, 109, 0, 0, 255, 0, 1, 192, 12, 0, 1, 0, 1, 0, 0, 1, 8, 0, 4, 172, 217, 17, 238, 192, 12, 0, 28, 0, 1, 0, 0, 0, 71, 0, 16, 42, 0, 20, 80, 64, 22, 8, 13, 0, 0, 0, 0, 0, 0, 32, 14, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 17, 0, 50, 4, 97, 108, 116, 52, 5, 97, 115, 112, 109, 120, 1, 108, 192, 12, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 4, 0, 10, 192, 91, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 9, 0, 30, 4, 97, 108, 116, 50, 192, 91, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 9, 0, 20, 4, 97, 108, 116, 49, 192, 91, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 9, 0, 40, 4, 97, 108, 116, 51, 192, 91, 192, 12, 0, 2, 0, 1, 0, 4, 31, 116, 0, 6, 3, 110, 115, 51, 192, 12, 192, 12, 0, 2, 0, 1, 0, 4, 31, 116, 0, 6, 3, 110, 115, 50, 192, 12, 192, 12, 0, 2, 0, 1, 0, 4, 31, 116, 0, 6, 3, 110, 115, 52, 192, 12, 192, 12, 0, 2, 0, 1, 0, 4, 31, 116, 0, 6, 3, 110, 115, 49, 192, 12, 0, 0, 41, 16, 0, 0, 0, 0, 0, 0, 0 };

        public override DnsMessageHandleType Type { get; }

        public BenchmarkMessageHandler(DnsMessageHandleType type = DnsMessageHandleType.UDP)
        {
            Type = type;
        }

        public override DnsResponseMessage Query(
                IPEndPoint server,
                DnsRequestMessage request,
                TimeSpan timeout)
        {
            using (var writer = new DnsDatagramWriter())
            {
                GetRequestData(request, writer);
                if (writer.Data.Count != s_questionRaw.Length)
                {
                    throw new Exception();
                }
            }

            // Allocation test will probably include this copy of the array, but the data has to be copied, otherwise the ID change would be shared...
            using (var writer = new DnsDatagramWriter(new ArraySegment<byte>(s_answer.ToArray())))
            {
                writer.Index = 0;
                writer.WriteInt16NetworkOrder((short)request.Header.Id);
                writer.Index = s_answer.Length;

                var response = GetResponseMessage(writer.Data);

                if (response.Header.Id != request.Header.Id)
                {
                    throw new Exception();
                }

                return response;
            }
        }

        public override Task<DnsResponseMessage> QueryAsync(
            IPEndPoint server,
            DnsRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Query(server, request, Timeout.InfiniteTimeSpan));
        }
    }
}
