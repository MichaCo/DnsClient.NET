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
        private readonly UdpClient _client = new UdpClient();
        private bool _disposedValue = false;

        public override bool IsTransientException<T>(T exception)
        {
            Debug.WriteLine("Check transient {0}.", exception);
            if (exception is SocketException) return true;
            return false;
        }

        public override async Task<DnsResponseMessage> QueryAsync(
            IPEndPoint server,
            DnsRequestMessage request,
            CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();

            //using (var udpClient = new UdpClient())
            //{
            var data = GetRequestData(request);
            await _client.SendAsync(data, data.Length, server);

            var result = await _client.ReceiveAsync();

            var response = GetResponseMessage(result.Buffer);

            if (request.Header.Id != response.Header.Id)
            {
                throw new DnsResponseException("Header id missmatch.");
            }

            return response;
            //}
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
#if !XPLAT
                    _client.Close();
#else
                    _client.Dispose();
#endif
                }

                _disposedValue = true;
            }

            base.Dispose(disposing);
        }
    }
}