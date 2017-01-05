using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DnsClient
{
    internal class DnsUdpMessageHandler : DnsMessageHandler
    {
        private static ConcurrentQueue<UdpClient> _clients = new ConcurrentQueue<UdpClient>();
        private readonly bool _enableClientQueue;

        public DnsUdpMessageHandler(bool enableClientQueue)
        {
            _enableClientQueue = enableClientQueue;
            if (_enableClientQueue)
            {
                for (var i = 0; i < 10; i++)
                {
                    _clients.Enqueue(new UdpClient());
                }
            }
        }

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
            cancellationToken.ThrowIfCancellationRequested();

            UdpClient udpClient = null;
            if (_enableClientQueue)
            {
                while (udpClient == null || !_clients.TryDequeue(out udpClient))
                {
                    udpClient = new UdpClient();
                }
            }
            else
            {
                udpClient = new UdpClient();
            }
            try
            {
                var data = GetRequestData(request);
                await udpClient.SendAsync(data, data.Length, server).ConfigureAwait(false);

                var result = await udpClient.ReceiveAsync().ConfigureAwait(false);

                var response = GetResponseMessage(result.Buffer);

                if (request.Header.Id != response.Header.Id)
                {
                    throw new DnsResponseException("Header id missmatch.");
                }

                if (_enableClientQueue)
                {
                    _clients.Enqueue(udpClient);
                }

                return response;
            }
            finally
            {
                if (!_enableClientQueue)
                {
#if PORTABLE
                    udpClient.Dispose();
#else
                    udpClient.Close();
#endif
                }
            }
        }
    }
}