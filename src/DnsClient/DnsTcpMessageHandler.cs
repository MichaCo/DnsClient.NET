// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        public override DnsResponseMessage Query(IPEndPoint server, DnsRequestMessage request, TimeSpan timeout)
        {
            CancellationToken cancellationToken = default;

            using var cts = timeout.TotalMilliseconds != Timeout.Infinite && timeout.TotalMilliseconds < int.MaxValue ?
                new CancellationTokenSource(timeout) : null;

            cancellationToken = cts?.Token ?? default;

            ClientPool pool;
            while (!_pools.TryGetValue(server, out pool))
            {
                _pools.TryAdd(server, new ClientPool(true, server));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var entry = pool.GetNextClient();

            using var cancelCallback = cancellationToken.Register(() =>
            {
                if (entry == null)
                {
                    return;
                }

                entry.DisposeClient();
            });

            try
            {
                var response = QueryInternal(entry.Client, request, cancellationToken);
                ValidateResponse(request, response);

                pool.Enqueue(entry);

                return response;
            }
            catch (ObjectDisposedException)
            {
                throw new OperationCanceledException(cancellationToken);
            }
            catch
            {
                entry.DisposeClient();
                throw;
            }
        }

        public override async Task<DnsResponseMessage> QueryAsync(
            IPEndPoint server,
            DnsRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ClientPool pool;
            while (!_pools.TryGetValue(server, out pool))
            {
                _pools.TryAdd(server, new ClientPool(true, server));
            }

            var entry = await pool.GetNextClientAsync().ConfigureAwait(false);

            using var cancelCallback = cancellationToken.Register(() =>
            {
                if (entry == null)
                {
                    return;
                }

                entry.DisposeClient();
            });

            try
            {
                var response = await QueryAsyncInternal(entry.Client, request, cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();
                ValidateResponse(request, response);

                pool.Enqueue(entry);

                return response;
            }
            catch
            {
                entry.DisposeClient();
                throw;
            }
        }

        private DnsResponseMessage QueryInternal(TcpClient client, DnsRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stream = client.GetStream();

            // use a pooled buffer to writer the data + the length of the data later into the first two bytes
            using var memory = new PooledBytes(DnsQueryOptions.MaximumBufferSize);

            using (var writer = new DnsDatagramWriter(new ArraySegment<byte>(memory.Buffer, 2, memory.Buffer.Length - 2)))
            {
                GetRequestData(request, writer);
                int dataLength = writer.Index;
                memory.Buffer[0] = (byte)((dataLength >> 8) & 0xff);
                memory.Buffer[1] = (byte)(dataLength & 0xff);

                //await client.Client.SendAsync(new ArraySegment<byte>(memory.Buffer, 0, dataLength + 2), SocketFlags.None).ConfigureAwait(false);
                stream.Write(memory.Buffer, 0, dataLength + 2);
                stream.Flush();
            }

            if (!stream.CanRead)
            {
                // might retry
                throw new TimeoutException();
            }

            cancellationToken.ThrowIfCancellationRequested();

            var responses = new List<DnsResponseMessage>();
            byte[] buffer = memory.Buffer;

            do
            {
                int bytesReceivedForLen = 0, readForLen;

                while ((bytesReceivedForLen += readForLen = stream.Read(buffer, bytesReceivedForLen, 2)) < 2)
                {
                    if (readForLen <= 0)
                    {
                        // disconnected, might retry
                        throw new TimeoutException();
                    }
                }

                int length = buffer[0] << 8 | buffer[1];

                if (length <= 0)
                {
                    // server signals close/disconnecting, might retry
                    throw new TimeoutException();
                }

                if (length > buffer.Length)
                {
                    buffer = new byte[length];
                }

                int bytesReceived = 0, read;
                int readSize = length > 4096 ? 4096 : length;

                cancellationToken.ThrowIfCancellationRequested();

                while (!cancellationToken.IsCancellationRequested
                    && (bytesReceived += read = stream.Read(buffer, bytesReceived, readSize)) < length)
                {
                    if (read <= 0)
                    {
                        // disconnected
                        throw new TimeoutException();
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

                DnsResponseMessage response = GetResponseMessage(new ArraySegment<byte>(buffer, 0, bytesReceived));
                responses.Add(response);
            } while (stream.DataAvailable && !cancellationToken.IsCancellationRequested);

            cancellationToken.ThrowIfCancellationRequested();
            return DnsResponseMessage.Combine(responses);
        }

        private async Task<DnsResponseMessage> QueryAsyncInternal(TcpClient client, DnsRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stream = client.GetStream();

            // use a pooled buffer to writer the data + the length of the data later into the first two bytes
            using var memory = new PooledBytes(DnsQueryOptions.MaximumBufferSize);

            using (var writer = new DnsDatagramWriter(new ArraySegment<byte>(memory.Buffer, 2, memory.Buffer.Length - 2)))
            {
                GetRequestData(request, writer);
                int dataLength = writer.Index;
                memory.Buffer[0] = (byte)((dataLength >> 8) & 0xff);
                memory.Buffer[1] = (byte)(dataLength & 0xff);

                await stream.WriteAsync(memory.Buffer, 0, dataLength + 2, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            if (!stream.CanRead)
            {
                // might retry
                throw new TimeoutException();
            }

            cancellationToken.ThrowIfCancellationRequested();

            var responses = new List<DnsResponseMessage>();

            do
            {
                int length;
                int bytesReceivedForLen = 0, readForLen;
                while ((bytesReceivedForLen += (readForLen = await stream.ReadAsync(memory.Buffer, bytesReceivedForLen, 2, cancellationToken).ConfigureAwait(false))) < 2)
                {
                    if (readForLen <= 0)
                    {
                        // disconnected, might retry
                        throw new TimeoutException();
                    }
                }

                length = memory.Buffer[0] << 8 | memory.Buffer[1];

                if (length <= 0)
                {
                    // server signals close/disconnecting, might retry
                    throw new TimeoutException();
                }

                byte[] buffer = memory.Buffer.Length <= length ? new byte[length] : memory.Buffer;
                int bytesReceived = 0, read;
                int readSize = length > 4096 ? 4096 : length;

                cancellationToken.ThrowIfCancellationRequested();

                while (!cancellationToken.IsCancellationRequested
                    && (bytesReceived += read = await stream.ReadAsync(buffer, bytesReceived, readSize, cancellationToken).ConfigureAwait(false)) < length)
                {
                    if (read <= 0)
                    {
                        // disconnected
                        throw new TimeoutException();
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

                DnsResponseMessage response = GetResponseMessage(new ArraySegment<byte>(buffer, 0, bytesReceived));
                responses.Add(response);
            } while (stream.DataAvailable && !cancellationToken.IsCancellationRequested);

            cancellationToken.ThrowIfCancellationRequested();
            return DnsResponseMessage.Combine(responses);
        }

        private class ClientPool : IDisposable
        {
            private bool _disposedValue;
            private readonly bool _enablePool;
            private ConcurrentQueue<ClientEntry> _clients = new ConcurrentQueue<ClientEntry>();
            private readonly IPEndPoint _endpoint;

            public ClientPool(bool enablePool, IPEndPoint endpoint)
            {
                _enablePool = enablePool;
                _endpoint = endpoint;
            }

            public ClientEntry GetNextClient()
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
                        entry.Client.Connect(_endpoint.Address, _endpoint.Port);
                    }
                }
                else
                {
                    entry = new ClientEntry(new TcpClient(_endpoint.AddressFamily), _endpoint);
                    entry.Client.Connect(_endpoint.Address, _endpoint.Port);
                }

                return entry;
            }

            public async Task<ClientEntry> GetNextClientAsync()
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
                        Client.Dispose();
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
