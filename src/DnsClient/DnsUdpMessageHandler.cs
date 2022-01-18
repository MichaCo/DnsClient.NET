using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DnsClient.Internal;

namespace DnsClient
{
    internal class DnsUdpMessageHandler : DnsMessageHandler
    {
        private const int MaxSize = 4096;
        private static readonly ConcurrentQueue<UdpClient> s_clients = new ConcurrentQueue<UdpClient>();
        private static readonly ConcurrentQueue<UdpClient> s_clientsIPv6 = new ConcurrentQueue<UdpClient>();
        private readonly bool _enableClientQueue;

        public override DnsMessageHandleType Type { get; } = DnsMessageHandleType.UDP;

        public DnsUdpMessageHandler(bool enableClientQueue)
        {
            _enableClientQueue = enableClientQueue;
        }

        public override DnsResponseMessage Query(
            IPEndPoint server,
            DnsRequestMessage request,
            TimeSpan timeout)
        {
            UdpClient udpClient = GetNextUdpClient(server.AddressFamily);

            // -1 indicates infinite
            int timeoutInMillis = timeout.TotalMilliseconds >= int.MaxValue ? -1 : (int)timeout.TotalMilliseconds;
            udpClient.Client.ReceiveTimeout = timeoutInMillis;
            udpClient.Client.SendTimeout = timeoutInMillis;

            bool mustDispose = false;
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

                    ValidateResponse(request, response);

                    Enqueue(server.AddressFamily, udpClient);

                    return response;
                }
            }
            catch
            {
                mustDispose = true;
                throw;
            }
            finally
            {
                if (!_enableClientQueue || mustDispose)
                {
                    try
                    {
#if !NET45
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
            IPEndPoint endpoint,
            DnsRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            UdpClient udpClient = GetNextUdpClient(endpoint.AddressFamily);

            using var callback = cancellationToken.Register(() =>
            {
#if !NET45
                udpClient.Dispose();
#else
                udpClient.Close();
#endif
            });

            bool mustDispose = false;
            try
            {
                using (var writer = new DnsDatagramWriter())
                {
                    GetRequestData(request, writer);
                    await udpClient.SendAsync(writer.Data.Array, writer.Data.Count, endpoint).ConfigureAwait(false);
                }

                var readSize = udpClient.Available > MaxSize ? udpClient.Available : MaxSize;

                using (var memory = new PooledBytes(readSize))
                {
#if !NET45
                    int received = await udpClient.Client.ReceiveAsync(new ArraySegment<byte>(memory.Buffer), SocketFlags.None).ConfigureAwait(false);

                    var response = GetResponseMessage(new ArraySegment<byte>(memory.Buffer, 0, received));

#else
                    var result = await udpClient.ReceiveAsync().ConfigureAwait(false);

                    var response = GetResponseMessage(new ArraySegment<byte>(result.Buffer, 0, result.Buffer.Length));
#endif

                    ValidateResponse(request, response);

                    Enqueue(endpoint.AddressFamily, udpClient);

                    return response;
                }
            }
            catch (SocketException se) when (se.SocketErrorCode == SocketError.OperationAborted)
            {
                throw new TimeoutException();
            }
            catch (ObjectDisposedException)
            {
                // we disposed it in case of a timeout request, lets indicate it actually timed out...
                throw new TimeoutException();
            }
            catch
            {
                mustDispose = true;

                throw;
            }
            finally
            {
                if (!_enableClientQueue || mustDispose)
                {
                    try
                    {
#if !NET45
                        udpClient.Dispose();
#else
                        udpClient.Close();
#endif
                    }
                    catch { }
                }
            }
        }

        private UdpClient GetNextUdpClient(AddressFamily family)
        {
            UdpClient udpClient = null;
            if (_enableClientQueue)
            {
                while (udpClient == null && !TryDequeue(family, out udpClient))
                {
                    udpClient = new UdpClient(family);
                }
            }
            else
            {
                udpClient = new UdpClient(family);
            }

            return udpClient;
        }

        private void Enqueue(AddressFamily family, UdpClient client)
        {
            if (_enableClientQueue)
            {
                if (family == AddressFamily.InterNetwork)
                {
                    s_clients.Enqueue(client);
                }
                else
                {
                    s_clientsIPv6.Enqueue(client);
                }
            }
        }

        private static bool TryDequeue(AddressFamily family, out UdpClient client)
        {
            if (family == AddressFamily.InterNetwork)
            {
                return s_clients.TryDequeue(out client);
            }

            return s_clientsIPv6.TryDequeue(out client);
        }
    }
}
