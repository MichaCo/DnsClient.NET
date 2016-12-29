using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DnsClient.Protocol;

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
            var sw = Stopwatch.StartNew();

            cancellationToken.ThrowIfCancellationRequested();

            using (var client = new TcpClient() {  })
            {
                await client.ConnectAsync(server.Address, server.Port).ConfigureAwait(false);
                using (var stream = client.GetStream())
                {
                    var data = GetRequestData(request);

                    var sendData = new byte[data.Length + 2];
                    sendData[0] = (byte)((data.Length >> 8) & 0xff);
                    sendData[1] = (byte)(data.Length & 0xff);
                    Array.Copy(data, 0, sendData, 2, data.Length);

                    await stream.WriteAsync(sendData, 0, sendData.Length).ConfigureAwait(false);

                    await stream.FlushAsync().ConfigureAwait(false);

                    //while (true)
                    //{
                    int intLength = stream.ReadByte() << 8 | stream.ReadByte();
                    if (intLength <= 0)
                    {
                        throw new DnsResponseException("Received no answer.");
                    }

                    //intMessageSize += intLength;

                    var resultData = new byte[intLength];
                    await stream.ReadAsync(resultData, 0, intLength).ConfigureAwait(false);

                    //}

                    var response = GetResponseMessage(resultData);

                    //Console.WriteLine($"{request.Header.Id} != {response.Header.Id}?");
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