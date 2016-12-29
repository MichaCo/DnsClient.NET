using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DnsClient.Protocol;

namespace DnsClient
{
    internal class DnsUdpMessageHandler : DnsMessageHandler
    {
        public override bool IsTransientException<T>(T exception)
        {
            if (exception is SocketException) return true;
            return false;
        }

        public override async Task<DnsResponseMessage> QueryAsync(
            IPEndPoint server,
            DnsRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            var sw = Stopwatch.StartNew();

            cancellationToken.ThrowIfCancellationRequested();

            using (var udpClient = new UdpClient() { })
            {
                var data = GetRequestData(request);
                await udpClient.SendAsync(data, data.Length, server).ConfigureAwait(false);

                var result = await udpClient.ReceiveAsync().ConfigureAwait(false);

                var response = GetResponseMessage(result.Buffer);

                if (request.Header.Id != response.Header.Id)
                {
                    throw new DnsResponseException("Header id missmatch.");
                }

                return response;
            }
        }
    }
}