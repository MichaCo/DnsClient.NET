using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DnsClient.Internal;

namespace DnsClient
{
    internal class DnsTcpMessageHandler : DnsMessageHandler
    {
        private readonly ConcurrentDictionary<IPEndPoint, ClientPool> _pools = new ConcurrentDictionary<IPEndPoint, ClientPool>();

        public override DnsMessageHandleType Type { get; } = DnsMessageHandleType.TCP;

        public override DnsResponseMessage Query(IPEndPoint endpoint, DnsRequestMessage request, TimeSpan timeout)
        {
            if (timeout.TotalMilliseconds != Timeout.Infinite && timeout.TotalMilliseconds < int.MaxValue)
            {
                using (var cts = new CancellationTokenSource(timeout))
                {
                    return QueryAsync(endpoint, request, cts.Token)
                        .WithCancellation(cts.Token)
                        .ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }

            return QueryAsync(endpoint, request, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public override async Task<DnsResponseMessage> QueryAsync(
            IPEndPoint server,
            DnsRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ClientPool pool;
            ClientPool.ClientEntry entry = null;
            while (!_pools.TryGetValue(server, out pool))
            {
                _pools.TryAdd(server, new ClientPool(true, server));
            }

            using var cancelCallback = cancellationToken.Register(() =>
            {
                if (entry == null)
                {
                    return;
                }

                entry.DisposeClient();
            });

            DnsResponseMessage response = null;

            // Simple retry in case the pooled clients are
            var tries = 0;
            while (response == null && tries < 3)
            {
                tries++;
                entry = await pool.GetNextClient().ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                response = await QueryAsyncInternal(entry.Client, request, cancellationToken)
                    .ConfigureAwait(false);

                if (response != null)
                {
                    pool.Enqueue(entry);
                }
                else
                {
                    entry.DisposeClient();
                }
            }

            ValidateResponse(request, response);

            return response;
        }

        private async Task<DnsResponseMessage> QueryAsyncInternal(TcpClient client, DnsRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stream = client.GetStream();

            // use a pooled buffer to writer the data + the length of the data later into the first two bytes
            using (var memory = new PooledBytes(DnsDatagramWriter.BufferSize + 2))
            using (var writer = new DnsDatagramWriter(new ArraySegment<byte>(memory.Buffer, 2, memory.Buffer.Length - 2)))
            {
                GetRequestData(request, writer);
                int dataLength = writer.Index;
                memory.Buffer[0] = (byte)((dataLength >> 8) & 0xff);
                memory.Buffer[1] = (byte)(dataLength & 0xff);

                //await client.Client.SendAsync(new ArraySegment<byte>(memory.Buffer, 0, dataLength + 2), SocketFlags.None).ConfigureAwait(false);
                await stream.WriteAsync(memory.Buffer, 0, dataLength + 2, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            if (!stream.CanRead)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var responses = new List<DnsResponseMessage>();

            do
            {
                int length;
                try
                {
                    //length = stream.ReadByte() << 8 | stream.ReadByte();
                    using (var lengthBuffer = new PooledBytes(2))
                    {
                        int bytesReceived = 0, read;
                        while ((bytesReceived += (read = await stream.ReadAsync(lengthBuffer.Buffer, bytesReceived, 2, cancellationToken).ConfigureAwait(false))) < 2)
                        {
                            if (read <= 0)
                            {
                                // disconnected, might retry
                                return null;
                            }
                        }

                        length = lengthBuffer.Buffer[0] << 8 | lengthBuffer.Buffer[1];
                    }
                }
                catch (Exception ex) when (ex is IOException || ex is SocketException)
                {
                    return null;
                }

                if (length <= 0)
                {
                    // server signals close/disconnecting
                    return null;
                }

                using (var memory = new PooledBytes(length))
                {
                    int bytesReceived = 0, read;
                    int readSize = length > 4096 ? 4096 : length;

                    while (!cancellationToken.IsCancellationRequested
                        && (bytesReceived += read = await stream.ReadAsync(memory.Buffer, bytesReceived, readSize, cancellationToken).ConfigureAwait(false)) < length)
                    {
                        if (read <= 0)
                        {
                            // disconnected
                            return null;
                        }
                        if (bytesReceived + readSize > length)
                        {
                            readSize = length - bytesReceived;

                            if (readSize <= 0)
                            {
                                break;
                            }
                        }
                    }

                    DnsResponseMessage response = GetResponseMessage(new ArraySegment<byte>(memory.Buffer, 0, bytesReceived));

                    responses.Add(response);
                }
            } while (stream.DataAvailable && !cancellationToken.IsCancellationRequested);

            return DnsResponseMessage.Combine(responses);
        }

        private class ClientPool : IDisposable
        {
            private bool _disposedValue = false;
            private readonly bool _enablePool;
            private ConcurrentQueue<ClientEntry> _clients = new ConcurrentQueue<ClientEntry>();
            private readonly IPEndPoint _endpoint;

            public ClientPool(bool enablePool, IPEndPoint endpoint)
            {
                _enablePool = enablePool;
                _endpoint = endpoint;
            }

            public async Task<ClientEntry> GetNextClient()
            {
                if (_disposedValue)
                {
                    throw new ObjectDisposedException(nameof(ClientPool));
                }

                ClientEntry entry = null;
                if (_enablePool)
                {
                    while (entry == null && !TryDequeue(out entry))
                    {
                        entry = new ClientEntry(new TcpClient(_endpoint.AddressFamily) { LingerState = new LingerOption(true, 0) }, _endpoint);
                        await entry.Client.ConnectAsync(_endpoint.Address, _endpoint.Port).ConfigureAwait(false);
                    }
                }
                else
                {
                    entry = new ClientEntry(new TcpClient(_endpoint.AddressFamily), _endpoint);
                    await entry.Client.ConnectAsync(_endpoint.Address, _endpoint.Port).ConfigureAwait(false);
                }

                return entry;
            }

            public void Enqueue(ClientEntry entry)
            {
                if (_disposedValue)
                {
                    throw new ObjectDisposedException(nameof(ClientPool));
                }

                if (entry == null)
                {
                    throw new ArgumentNullException(nameof(entry));
                }

                if (!entry.Client.Client.RemoteEndPoint.Equals(_endpoint))
                {
                    throw new ArgumentException("Invalid endpoint.");
                }

                // TickCount swap will be fine here as the entry just gets disposed and we'll create a new one starting at 0+ again, totally fine...
                if (_enablePool && entry.Client.Connected && entry.StartMillis + entry.MaxLiveTime >= (Environment.TickCount & int.MaxValue))
                {
                    _clients.Enqueue(entry);
                }
                else
                {
                    // dispose the client and don't keep a reference
                    entry.DisposeClient();
                }
            }

            public bool TryDequeue(out ClientEntry entry)
            {
                if (_disposedValue)
                {
                    throw new ObjectDisposedException(nameof(ClientPool));
                }

                bool result;
                while (result = _clients.TryDequeue(out entry))
                {
                    // validate the client before returning it
                    if (entry.Client.Connected && entry.StartMillis + entry.MaxLiveTime >= (Environment.TickCount & int.MaxValue))
                    {
                        break;
                    }
                    else
                    {
                        entry.DisposeClient();
                    }
                }

                return result;
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        foreach (var entry in _clients)
                        {
                            entry.DisposeClient();
                        }

                        _clients = new ConcurrentQueue<ClientEntry>();
                    }

                    _disposedValue = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }

            public class ClientEntry
            {
                public ClientEntry(TcpClient client, IPEndPoint endpoint)
                {
                    Client = client;
                    Endpoint = endpoint;
                }

                public void DisposeClient()
                {
                    try
                    {
#if !NET45
                        Client.Dispose();
#else
                        Client.Close();
#endif
                    }
                    catch { }
                }

                public TcpClient Client { get; }

                public IPEndPoint Endpoint { get; }

                public int StartMillis { get; set; } = Environment.TickCount & int.MaxValue;

                public int MaxLiveTime { get; set; } = 5000;
            }
        }
    }
}
