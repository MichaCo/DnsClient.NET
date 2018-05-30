using System;
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
            private static readonly DnsRequestMessage _request = new DnsRequestMessage(
                   new DnsRequestHeader(123, DnsOpCode.Query),
                   new DnsQuestion("google.com", QueryType.ANY, QueryClass.IN));

            private static readonly BenchmarkMessageHandler _handler = new BenchmarkMessageHandler();
            private static readonly BenchmarkMessageHandler _handlerRequest = new BenchmarkMessageHandler(true, false);
            private static readonly BenchmarkMessageHandler _handlerResponse = new BenchmarkMessageHandler(false, true);
            private static readonly LookupClient _lookup = new LookupClient(IPAddress.Loopback);

            public RequestResponseParsing()
            {
            }

            [Benchmark(Baseline = true)]
            public void RequestAndResponse()
            {
                var result = _lookup.ResolveQuery(_lookup.NameServers, _handler, _request);
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
            public void Request()
            {
                var result = _lookup.ResolveQuery(_lookup.NameServers, _handlerRequest, _request);
            }

            [Benchmark]
            public void Response()
            {
                var result = _lookup.ResolveQuery(_lookup.NameServers, _handlerResponse, _request);
            }
        }
    }

    internal class BenchmarkMessageHandler : DnsMessageHandler
    {
        //google.com any
        private static readonly byte[] _questionRaw = new byte[] { 95, 207, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 6, 103, 111, 111, 103, 108, 101, 3, 99, 111, 109, 0, 0, 255, 0, 1, 0, 0, 41, 16, 0, 0, 0, 0, 0, 0, 0 };

        //result
        private static readonly byte[] _answer = new byte[] { 95, 207, 129, 128, 0, 1, 0, 11, 0, 0, 0, 1, 6, 103, 111, 111, 103, 108, 101, 3, 99, 111, 109, 0, 0, 255, 0, 1, 192, 12, 0, 1, 0, 1, 0, 0, 1, 8, 0, 4, 172, 217, 17, 238, 192, 12, 0, 28, 0, 1, 0, 0, 0, 71, 0, 16, 42, 0, 20, 80, 64, 22, 8, 13, 0, 0, 0, 0, 0, 0, 32, 14, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 17, 0, 50, 4, 97, 108, 116, 52, 5, 97, 115, 112, 109, 120, 1, 108, 192, 12, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 4, 0, 10, 192, 91, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 9, 0, 30, 4, 97, 108, 116, 50, 192, 91, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 9, 0, 20, 4, 97, 108, 116, 49, 192, 91, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 9, 0, 40, 4, 97, 108, 116, 51, 192, 91, 192, 12, 0, 2, 0, 1, 0, 4, 31, 116, 0, 6, 3, 110, 115, 51, 192, 12, 192, 12, 0, 2, 0, 1, 0, 4, 31, 116, 0, 6, 3, 110, 115, 50, 192, 12, 192, 12, 0, 2, 0, 1, 0, 4, 31, 116, 0, 6, 3, 110, 115, 52, 192, 12, 192, 12, 0, 2, 0, 1, 0, 4, 31, 116, 0, 6, 3, 110, 115, 49, 192, 12, 0, 0, 41, 16, 0, 0, 0, 0, 0, 0, 0 };

        private readonly bool _request;
        private readonly bool _response;
        private const int MaxSize = 4096;

        public BenchmarkMessageHandler(bool request = true, bool response = true)
        {
            _request = request;
            _response = response;
        }

        public override bool IsTransientException<T>(T exception)
        {
            return false;
        }

        public override DnsResponseMessage Query(
            IPEndPoint server,
            DnsRequestMessage request,
            TimeSpan timeout)
        {
            if (_request)
            {
                using (var writer = new DnsDatagramWriter())
                {
                    GetRequestData(request, writer);
                }
            }

            if (_response)
            {
                var response = GetResponseMessage(new ArraySegment<byte>(_answer, 0, _answer.Length));

                return response;
            }

            return new DnsResponseMessage(new DnsResponseHeader(0, 0, 0, 0, 0, 0), 0);
        }

        public override Task<DnsResponseMessage> QueryAsync(
            IPEndPoint server,
            DnsRequestMessage request,
            CancellationToken cancellationToken,
            Action<Action> cancelationCallback)
        {
            // no need to run async here as we don't do any IO
            return Task.FromResult(Query(server, request, Timeout.InfiniteTimeSpan));
        }
    }
}