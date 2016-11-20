using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DnsClient2
{
    public class DnsUdpMessageHandler : DnsMessageHandler
    {
        public override async Task<DnsResponseMessage> QueryAsync(
            DnsEndPoint server,
            DnsRequestMessage request, 
            CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();

            using (var udpClient = new UdpClient())
            {
                var data = GetRequestData(request);
                await udpClient.SendAsync(data, data.Length, server.Host, server.Port);

                var result = await udpClient.ReceiveAsync();

                var response = GetResponseMessage(result.Buffer);
                
                return response;
            }
        }
    }
}