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
            IPEndPoint endpoint,
            DnsRequestMessage request,
            TimeSpan timeout)
        {
            var udpClient = new UdpClient(endpoint.AddressFamily);

            try
            {
                // -1 indicates infinite
                int timeoutInMillis = timeout.TotalMilliseconds >= int.MaxValue ? -1 : (int)timeout.TotalMilliseconds;
                udpClient.Client.ReceiveTimeout = timeoutInMillis;
                udpClient.Client.SendTimeout = timeoutInMillis;

                using (var writer = new DnsDatagramWriter())
                {
                    GetRequestData(request, writer);
                    udpClient.Send(writer.Data.Array, writer.Data.Count, endpoint);
                }

                var result = udpClient.Receive(ref endpoint);
                var response = GetResponseMessage(new ArraySegment<byte>(result, 0, result.Length));
                ValidateResponse(request, response);
                return response;
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

                var result = await udpClient.ReceiveAsync().ConfigureAwait(false);
                var response = GetResponseMessage(new ArraySegment<byte>(result.Buffer, 0, result.Buffer.Length));
                ValidateResponse(request, response);
                return response;
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
