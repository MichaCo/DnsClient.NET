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

        public override DnsMessageHandleType Type { get; } = DnsMessageHandleType.UDP;

        public DnsUdpMessageHandler()
        {
        }

        public override DnsResponseMessage Query(
            IPEndPoint server,
            DnsRequestMessage request,
            TimeSpan timeout)
        {
            var udpClient = new UdpClient(server.AddressFamily);

            try
            {
                // -1 indicates infinite
                int timeoutInMillis = timeout.TotalMilliseconds >= int.MaxValue ? -1 : (int)timeout.TotalMilliseconds;
                udpClient.Client.ReceiveTimeout = timeoutInMillis;
                udpClient.Client.SendTimeout = timeoutInMillis;

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

                    return response;
                }
            }
            finally
            {
                try
                {
                    udpClient.Dispose();
                }
                catch { }
            }
        }

        public override async Task<DnsResponseMessage> QueryAsync(
            IPEndPoint endpoint,
            DnsRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var udpClient = new UdpClient(endpoint.AddressFamily);

            try
            {
                using var callback = cancellationToken.Register(() =>
                {
                    udpClient.Dispose();
                });

                using (var writer = new DnsDatagramWriter())
                {
                    GetRequestData(request, writer);
                    await udpClient.SendAsync(writer.Data.Array, writer.Data.Count, endpoint).ConfigureAwait(false);
                }

                var readSize = udpClient.Available > MaxSize ? udpClient.Available : MaxSize;

                using (var memory = new PooledBytes(readSize))
                {

#if NET6_0_OR_GREATER
                    int received = await udpClient.Client.ReceiveAsync(
                        new ArraySegment<byte>(memory.Buffer),
                        SocketFlags.None,
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    var response = GetResponseMessage(new ArraySegment<byte>(memory.Buffer, 0, received));
#else
                    int received = await udpClient.Client.ReceiveAsync(new ArraySegment<byte>(memory.Buffer), SocketFlags.None).ConfigureAwait(false);

                    var response = GetResponseMessage(new ArraySegment<byte>(memory.Buffer, 0, received));
#endif

                    ValidateResponse(request, response);

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
            finally
            {
                try
                {
                    udpClient.Dispose();
                }
                catch { }
            }
        }
    }
}