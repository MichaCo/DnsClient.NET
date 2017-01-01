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

        public override async Task<DnsResponseMessage> QueryAsync(
            IPEndPoint server,
            DnsRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            using (var client = new TcpClient(server.AddressFamily))
            {
                await client.ConnectAsync(server.Address, server.Port).ConfigureAwait(false);
                using (var stream = client.GetStream())
                {
                    var data = GetRequestData(request);
                    int dataLength = data.Length;

                    //var sendLength = new byte[2];
                    //sendLength[0] = (byte)((data.Length >> 8) & 0xff);
                    //sendLength[1] = (byte)(data.Length & 0xff);

                    var sendData = new byte[dataLength + 2];
                    sendData[0] = (byte)((dataLength >> 8) & 0xff);
                    sendData[1] = (byte)(dataLength & 0xff);
                    Array.Copy(data, 0, sendData, 2, dataLength);
                    
                    await stream.WriteAsync(sendData, 0, sendData.Length, cancellationToken).ConfigureAwait(false);

                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

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

                    var response = GetResponseMessage(resultData);

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