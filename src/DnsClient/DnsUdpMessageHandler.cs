using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DnsClient
{
    public class DnsUdpMessageHandler : DnsMessageHandler
    {
        public override bool IsTransientException<T>(T exception)
        {
            Debug.WriteLine("Check transient {0}.", exception);
            if (exception is SocketException) return true;
            return false;
        }

        public override async Task<DnsResponseMessage> QueryAsync(
            IPEndPoint server,
            DnsRequestMessage request,
            CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();

            using (var udpClient = new UdpClient())
            {
                var data = GetRequestData(request);
                await udpClient.SendAsync(data, data.Length, server);

                var result = await udpClient.ReceiveAsync();

                var response = GetResponseMessage(result.Buffer);

                return response;
            }
        }
    }
}