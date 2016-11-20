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
    public class DnsUdpMessageInvoker : DnsMessageInvoker
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

                var responseHeader = ParseHeader(result.Buffer);

                var response = new DnsResponseMessage();

                return response;
            }
        }


        ////private async Task<Response> UdpRequest2(Request request)
        ////{
        ////    var sw = Stopwatch.StartNew();

        ////    for (int attempt = 0; attempt < _options.Retries; attempt++)
        ////    {
        ////        for (int indexServer = 0; indexServer < _options.DnsServers.Count; indexServer++)
        ////        {
        ////            using (var udpClient = new UdpClient())
        ////            {
        ////                var dnsServer = _options.DnsServers.ElementAt(indexServer);

        ////                try
        ////                {
        ////                    var sendTask = await udpClient
        ////                        .SendAsync(request.Data, request.Data.Length, dnsServer)
        ////                        .TimeoutAfter(_options.Timeout);

        ////                    if (IsLogging)
        ////                    {
        ////                        this._logger.LogDebug($"Sending ({request.Data.Length}) bytes in {sw.ElapsedMilliseconds} ms.");
        ////                        sw.Restart();
        ////                    }

        ////                    var result = await udpClient
        ////                        .ReceiveAsync()
        ////                        .TimeoutAfter(_options.Timeout);

        ////                    if (IsLogging)
        ////                    {
        ////                        this._logger.LogDebug($"Received ({result.Buffer.Length}) bytes in {sw.ElapsedMilliseconds} ms.");
        ////                        sw.Restart();
        ////                    }

        ////                    Response response = new Response(_loggerFactory, dnsServer, result.Buffer);
        ////                    AddToCache(response);
        ////                    return response;
        ////                }
        ////                catch (TimeoutException ex)
        ////                {
        ////                    if (IsLogging)
        ////                    {
        ////                        _logger.LogWarning(0, ex, "Connection to nameserver '{0}' timed out.", dnsServer);
        ////                    }
        ////                }
        ////                catch (SocketException ex)
        ////                {
        ////                    //TODO remove servers which throw not supported protocol exceptions
        ////                    if (IsLogging)
        ////                    {
        ////                        _logger.LogWarning(0, ex, "Socket error occurred with nameserver {0}.", dnsServer);
        ////                    }
        ////                }
        ////                catch (AggregateException aggEx)
        ////                {
        ////                    aggEx.Handle(ex =>
        ////                    {
        ////                        if (ex.GetType() == typeof(SocketException))
        ////                        {
        ////                            if (IsLogging)
        ////                            {
        ////                                _logger.LogWarning(0, ex, "Connection to nameserver {0} failed.", dnsServer);
        ////                            }

        ////                            return true;
        ////                        }
        ////                        if (ex.GetType() == typeof(TimeoutException))
        ////                        {
        ////                            if (IsLogging)
        ////                            {
        ////                                _logger.LogWarning(0, ex, "Connection to nameserver '{0}' timed out.", dnsServer);
        ////                            }

        ////                            return true;
        ////                        }

        ////                        return false;
        ////                    });
        ////                }
        ////                finally
        ////                {
        ////                    _uniqueId++;
        ////                }
        ////            }
        ////        }
        ////    }

        ////    if (IsLogging)
        ////    {
        ////        _logger.LogError("Could not send or receive message from any configured nameserver.");
        ////    }

        ////    return TimeoutResponse;
        ////}
    }
}
