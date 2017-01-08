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
        private const int MaxSize = 4096;
        private static ConcurrentQueue<UdpClient> _clients = new ConcurrentQueue<UdpClient>();
        private readonly bool _enableClientQueue;

        public DnsUdpMessageHandler(bool enableClientQueue)
        {
            _enableClientQueue = enableClientQueue;
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
                using (var writer = new DnsDatagramWriter())
                {
                    GetRequestData(request, writer);
                    udpClient.Client.SendTo(writer.Data.Array, writer.Data.Offset, writer.Data.Count, SocketFlags.None, server);
                }

                var readSize = udpClient.Available > MaxSize ? udpClient.Available : MaxSize;

                using (var memory = new PooledBytes(readSize))
                {
                    var received = udpClient.Client.Receive(memory.Buffer, 0, readSize, SocketFlags.None);

                    var response = GetResponseMessage(new ArraySegment<byte>(memory.Buffer, 0, received));
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
            catch
            {
                throw;
            }
            finally
            {
                if (!_enableClientQueue)
                {
                    try
                    {
#if PORTABLE
                        udpClient.Dispose();
#else
                        udpClient.Close();
#endif
                    }
                    catch { }
                }
            }
        }

        public override async Task<DnsResponseMessage> QueryAsync(
            IPEndPoint server,
            DnsRequestMessage request,
            CancellationToken cancellationToken,
            Action<Action> cancelationCallback)
        {
            cancellationToken.ThrowIfCancellationRequested();

            UdpClient udpClient = GetNextUdpClient();

            try
            {
                // setup timeout cancelation, dispose socket (the only way to acutally cancel the request in async...
                cancelationCallback(() =>
                {
#if PORTABLE
                    udpClient.Dispose();
#else
                    udpClient.Close();
#endif
                });

                using (var writer = new DnsDatagramWriter())
                {
                    GetRequestData(request, writer);
                    await udpClient.SendAsync(writer.Data.Array, writer.Data.Count, server).ConfigureAwait(false);
                }

                var readSize = udpClient.Available > MaxSize ? udpClient.Available : MaxSize;

                using (var memory = new PooledBytes(readSize))
                {
#if PORTABLE
                    int received = await udpClient.Client.ReceiveAsync(new ArraySegment<byte>(memory.Buffer), SocketFlags.None).ConfigureAwait(false);

                    var response = GetResponseMessage(new ArraySegment<byte>(memory.Buffer, 0, received));

#else
                    var result = await udpClient.ReceiveAsync().ConfigureAwait(false);

                    var response = GetResponseMessage(new ArraySegment<byte>(result.Buffer, 0, result.Buffer.Length));
#endif
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
            catch (ObjectDisposedException)
            {
                // we disposed it in case of a timeout request, lets indicate it actually timed out...
                throw new TimeoutException();
            }
            finally
            {
                if (!_enableClientQueue)
                {
                    try
                    {
#if PORTABLE
                        udpClient.Dispose();
#else
                        udpClient.Close();
#endif
                    }
                    catch { }
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