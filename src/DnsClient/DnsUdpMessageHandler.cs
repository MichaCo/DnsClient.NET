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
        private const int MaxSize = 4096 * 4;
        private static ConcurrentQueue<UdpClient> _clients = new ConcurrentQueue<UdpClient>();
        private readonly bool _enableClientQueue;

        public DnsUdpMessageHandler(bool enableClientQueue)
        {
            _enableClientQueue = enableClientQueue;
            if (_enableClientQueue && _clients.Count == 0)
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

        public override DnsResponseMessage Query(
            IPEndPoint server,
            DnsRequestMessage request)
        {
            UdpClient udpClient = GetNextUdpClient();
            try
            {
                var data = GetRequestData(request);
                udpClient.Client.SendTo(data, server);

                using (var memory = new PooledBytes(MaxSize))
                {
                    var received = udpClient.Client.Receive(memory.Buffer, 0, 4096, SocketFlags.None);
                    while (udpClient.Available > 0)
                    {
                        received = udpClient.Client.Receive(memory.Buffer, received, 4096, SocketFlags.None);
                    }

                    var response = GetResponseMessage(new ArraySegment<byte>(memory.Buffer, 0, memory.Buffer.Length));
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

        ////private static readonly byte[] fixData = new byte[] { 25, 158, 133, 0, 0, 1, 0, 1, 0, 0, 0, 1, 9, 108, 111, 99, 97, 108, 104, 111, 115, 116, 0, 0, 1, 0, 1, 192, 12, 0, 1, 0, 1, 0, 9, 58, 128, 0, 4, 127, 0, 0, 1, 0, 0, 41, 16, 0, 0, 0, 0, 0, 0, 0 };
        public override async Task<DnsResponseMessage> QueryAsync(
            IPEndPoint server,
            DnsRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            UdpClient udpClient = GetNextUdpClient();
            try
            {
                var data = GetRequestData(request);
                await udpClient.SendAsync(data, data.Length, server).ConfigureAwait(false);

                var result = await udpClient.ReceiveAsync().ConfigureAwait(false);

                var response = GetResponseMessage(new ArraySegment<byte>(result.Buffer, 0, result.Buffer.Length));

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

        private UdpClient GetNextUdpClient()
        {
            UdpClient udpClient = null;
            if (_enableClientQueue)
            {
                while (udpClient == null && !_clients.TryDequeue(out udpClient))
                {
                    udpClient = new UdpClient();
                }
            }
            else
            {
                udpClient = new UdpClient();
            }

            return udpClient;
        }
    }
}