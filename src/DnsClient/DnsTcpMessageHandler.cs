using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DnsClient
{
    internal class DnsTcpMessageHandler : DnsMessageHandler
    {
        public override bool IsTransientException<T>(T exception)
        {
            //if (exception is SocketException) return true;
            return false;
        }

        public override DnsResponseMessage Query(IPEndPoint endpoint, DnsRequestMessage request, TimeSpan timeout)
        {
            if (timeout.TotalMilliseconds != Timeout.Infinite && timeout.TotalMilliseconds < int.MaxValue)
            {
                using (var cts = new CancellationTokenSource(timeout))
                {
                    Action onCancel = () => { };
                    return QueryAsync(endpoint, request, cts.Token, (s) => onCancel = s)
                        .WithCancellation(cts.Token, onCancel)
                        .ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }

            return QueryAsync(endpoint, request, CancellationToken.None, (s) => { }).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public override async Task<DnsResponseMessage> QueryAsync(
            IPEndPoint server,
            DnsRequestMessage request,
            CancellationToken cancellationToken,
            Action<Action> cancelationCallback)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var client = new TcpClient(server.AddressFamily))
            {
                cancelationCallback(() =>
                {
#if PORTABLE
                    client.Dispose();
#else
                    client.Close();
#endif
                });

                cancellationToken.ThrowIfCancellationRequested();
                await client.ConnectAsync(server.Address, server.Port).ConfigureAwait(false);
                using (var stream = client.GetStream())
                {
                    // use a pooled buffer to writer the data + the length of the data later into the frist two bytes
                    using (var memory = new PooledBytes(DnsDatagramWriter.BufferSize + 2))
                    using (var writer = new DnsDatagramWriter(new ArraySegment<byte>(memory.Buffer, 2, memory.Buffer.Length - 2)))
                    {
                        GetRequestData(request, writer);
                        int dataLength = writer.Index;
                        memory.Buffer[0] = (byte)((dataLength >> 8) & 0xff);
                        memory.Buffer[1] = (byte)(dataLength & 0xff);

                        //var sendData = new byte[dataLength + 2];
                        //sendData[0] = (byte)((dataLength >> 8) & 0xff);
                        //sendData[1] = (byte)(dataLength & 0xff);
                        //Array.Copy(data, 0, sendData, 2, dataLength);

                        await stream.WriteAsync(memory.Buffer, 0, dataLength + 2, cancellationToken).ConfigureAwait(false);
                        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    }

                    int length = stream.ReadByte() << 8 | stream.ReadByte();
                    if (length <= 0)
                    {
                        throw new DnsResponseException("Received no answer.");
                    }

                    var resultData = new byte[length];
                    int bytesReceived = 0;
                    while (bytesReceived < length)
                    {
                        int read = await stream.ReadAsync(resultData, bytesReceived, length - bytesReceived, cancellationToken).ConfigureAwait(false);
                        bytesReceived += read;

                        if (read == 0 && bytesReceived < length)
                        {
                            // disconnected
                            throw new SocketException(-1);
                        }
                    }

                    var response = GetResponseMessage(new ArraySegment<byte>(resultData, 0, bytesReceived));

                    if (request.Header.Id != response.Header.Id)
                    {
                        throw new DnsResponseException("Header id missmatch.");
                    }

                    return response;
                }
            }
        }
    }
}