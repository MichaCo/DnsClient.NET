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
    public class DnsUdpMessageHandler : DnsMessageHandler, IDisposable
    {
        private bool _disposedValue = false;

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
            var sw = Stopwatch.StartNew();

            using (var udpClient = new UdpClient() { EnableBroadcast = true })
            {
                var data = GetRequestData(request);
                await udpClient.SendAsync(data, data.Length, server);

                var result = await udpClient.ReceiveAsync();

                var response = GetResponseMessage(result.Buffer);
                
                if (request.Header.Id != response.Header.Id)
                {
                    throw new DnsResponseException("Header id missmatch.");
                }

                return response;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                }

                _disposedValue = true;
            }

            base.Dispose(disposing);
        }
    }
}