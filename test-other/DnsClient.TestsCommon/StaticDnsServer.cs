﻿using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DnsClient.Internal;

namespace DnsClient
{
    public class StaticDnsServer
    {
        public const int AnyPort = IPEndPoint.MinPort;
        public static readonly IPEndPoint AnyIPEndPoint = new IPEndPoint(IPAddress.Any, AnyPort);

        // static dns server statically always returns this static response ^^
        private static byte[] Response = new byte[]
        {
            0, 42, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 5, 113, 117, 101, 114, 121, 0, 0, 1, 0, 1, 0, 0, 0, 100, 0, 4, 123, 45, 67, 9
        };

        private readonly bool _printStats;
        private readonly int _port;
        private readonly int _workers;

        private int[] _workerHitCounter = new int[4];
        private UdpClient _server;
        private CancellationTokenSource _cancelSource;
        private bool _running = false;

        public StaticDnsServer(bool printStats = true, int port = 5053, int workers = 4)
        {
            _printStats = printStats;
            _port = port;
            _workers = workers;
        }

        public void Start()
        {
            if (_running)
            {
                throw new InvalidOperationException("Already running duce");
            }
            _running = true;
            _cancelSource = new CancellationTokenSource();
            _workerHitCounter = new int[_workers];

            //_udpServer.ExclusiveAddressUse = true;
            _server = new UdpClient(new IPEndPoint(IPAddress.Loopback, _port));

            // async
            //Console.WriteLine("using tasks");
            //for (var workers = 0; workers < _workerHitCounter.Length; workers++)
            //{
            //    var id = workers;
            //    Task.Run(() => HandleRequest(id, server));
            //}

            // threaded
            for (var worker = 0; worker < _workerHitCounter.Length; worker++)
            {
                var id = worker;
                new Thread(new ThreadStart(() =>
                {
                    HandleRequest(id);
                })).Start();
            }

            if (_printStats)
            {
                Task.Run(async () =>
                {
                    while (!_cancelSource.IsCancellationRequested)
                    {
                        // very rough per second hit calc, but more than enough..
                        var lastCount = _workerHitCounter.Sum();
                        var hitsPerSec = _workerHitCounter.Sum() - lastCount;
                        Console.WriteLine($"Counters:" + string.Join(" ", _workerHitCounter) + "\t" + hitsPerSec + "r/sec");

                        await Task.Delay(1000);
                    }
                });
            }
        }

        public void Stop()
        {
            _cancelSource.Cancel(true);
            try
            {
                _server.Dispose();
            }
            catch { }
        }

        private void HandleRequest(int id)
        {
            var obj = new object();
            while (!_cancelSource.IsCancellationRequested)
            {
                try
                {
                    using (var memory = new PooledBytes(512))
                    {
                        EndPoint endpoint = AnyIPEndPoint;
                        var result = _server.Client.ReceiveFrom(memory.Buffer, 0, memory.Buffer.Length, SocketFlags.None, ref endpoint);
                        Interlocked.Increment(ref _workerHitCounter[id]);
                        HandleResponseSocketPooled(_server.Client, endpoint, memory.Buffer);
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        private async Task HandleRequestAsync(int id)
        {
            var obj = new object();
            while (!_cancelSource.IsCancellationRequested)
            {
                try
                {
                    using (var memory = new PooledBytes(512))
                    {
                        var segment = new ArraySegment<byte>(memory.Buffer, 0, memory.Buffer.Length);
                        var result = await _server.Client.ReceiveFromAsync(segment, SocketFlags.None, AnyIPEndPoint);
                        Interlocked.Increment(ref _workerHitCounter[id]);
                        await HandleResponseSocketPooledAsync(_server.Client, result.RemoteEndPoint, segment);
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        private void HandleResponseSocketPooled(Socket server, EndPoint remoteEndpoint, byte[] receiveBuffer)
        {
            using (var memory = new PooledBytes(Response.Length))
            {
                //Buffer.BlockCopy(Response, 0, memory.Buffer, 0, Response.Length);
                memory.Buffer[0] = receiveBuffer[0];
                memory.Buffer[1] = receiveBuffer[1];
                for (var i = 2; i < Response.Length; i++)
                {
                    memory.Buffer[i] = Response[i];
                }

                server.SendTo(memory.Buffer, 0, memory.Buffer.Length, SocketFlags.None, remoteEndpoint);
            }
        }

        private async Task HandleResponseSocketPooledAsync(Socket server, EndPoint remoteEndpoint, ArraySegment<byte> receiveBuffer)
        {
            using (var memory = new PooledBytes(Response.Length))
            {
                //Buffer.BlockCopy(Response, 0, memory.Buffer, 0, Response.Length);
                memory.Buffer[0] = receiveBuffer.Array[0];
                memory.Buffer[1] = receiveBuffer.Array[1];
                for (var i = 2; i < Response.Length; i++)
                {
                    memory.Buffer[i] = Response[i];
                }

                await server.SendToAsync(new ArraySegment<byte>(memory.Buffer, 0, memory.Buffer.Length), SocketFlags.None, remoteEndpoint);
            }
        }
    }
}