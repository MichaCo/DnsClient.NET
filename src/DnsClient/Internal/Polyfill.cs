// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DnsClient.Internal
{
    internal static class Polyfill
    {
        public static Task<SocketReceiveFromResult> ReceiveFromAsync(this Socket socket, ArraySegment<byte> buffer, EndPoint endPoint)
#if NETFRAMEWORK || NETSTANDARD2_0
            => ReceiveFromAsyncAPM(socket, buffer, endPoint);      
#else
            => socket.ReceiveFromAsync(buffer, SocketFlags.None, endPoint);
#endif

        public static Task<int> SendToAsync(this Socket socket, ArraySegment<byte> buffer, EndPoint endPoint)
#if NETFRAMEWORK || NETSTANDARD2_0
            => SendToAsyncAPM(socket, buffer, endPoint);      
#else
            => socket.SendToAsync(buffer, SocketFlags.None, endPoint);
#endif

#if NETFRAMEWORK || NETSTANDARD2_0
        // Task-based async Socket methods result in memory leak on .NET Framework:
        // https://github.com/MichaCo/DnsClient.NET/issues/192
        // To avoid this, we use the old Begin/End pattern.
        public static Task<SocketReceiveFromResult> ReceiveFromAsyncAPM(Socket socket, ArraySegment<byte> buffer, EndPoint ep)
        {
            object[] packedArguments = new object[] { socket, ep };

            return Task<SocketReceiveFromResult>.Factory.FromAsync(
                (buffer, callback, state) =>
                {
                    var arguments = (object[])state;
                    var s = (Socket)arguments[0];
                    var e = (EndPoint)arguments[1];

                    IAsyncResult result = s.BeginReceiveFrom(
                        buffer.Array,
                        buffer.Offset,
                        buffer.Count,
                        SocketFlags.None,
                        ref e,
                        callback,
                        state);

                    arguments[1] = e;
                    return result;
                },
                asyncResult =>
                {
                    var arguments = (object[])asyncResult.AsyncState;
                    var s = (Socket)arguments[0];
                    var e = (EndPoint)arguments[1];

                    int bytesReceived = s.EndReceiveFrom(asyncResult, ref e);

                    return new SocketReceiveFromResult()
                    {
                        ReceivedBytes = bytesReceived,
                        RemoteEndPoint = e
                    };
                },
                buffer,
                state: packedArguments);
        }

        public static Task<int> SendToAsyncAPM(this Socket socket, ArraySegment<byte> buffer, EndPoint ep)
        {
            return Task<int>.Factory.FromAsync(
                (targetBuffer, flags, endPoint, callback, state) => ((Socket)state).BeginSendTo(
                                                                        targetBuffer.Array,
                                                                        targetBuffer.Offset,
                                                                        targetBuffer.Count,
                                                                        flags,
                                                                        endPoint,
                                                                        callback,
                                                                        state),
                asyncResult => ((Socket)asyncResult.AsyncState).EndSendTo(asyncResult),
                buffer,
                SocketFlags.None,
                ep,
                state: socket);
        }
#endif
    }
}
