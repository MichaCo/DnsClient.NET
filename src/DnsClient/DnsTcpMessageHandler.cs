using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using DnsClient.Protocol;

namespace DnsClient
{
    public class DnsTcpMessageHandler : DnsMessageHandler, IDisposable
    {
        private bool _disposedValue = false;

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

            using (var client = new TcpClient() { })
            {
                await client.ConnectAsync(server.Address, server.Port);
                var stream = new BufferedStream(client.GetStream());
                var data = GetRequestData(request);

                stream.WriteByte((byte)((data.Length >> 8) & 0xff));
                stream.WriteByte((byte)(data.Length & 0xff));
                await stream.WriteAsync(data, 0, data.Length);

                await stream.FlushAsync();

                //while (true)
                //{
                int intLength = stream.ReadByte() << 8 | stream.ReadByte();
                if (intLength <= 0)
                {
                    throw new DnsResponseException("Received no answer.");
                }

                //intMessageSize += intLength;

                var resultData = new byte[intLength];
                await stream.ReadAsync(resultData, 0, intLength);

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

        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                }

                _disposedValue = true;
            }

            base.Dispose(disposing);
        }
    }
}