// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Buffers;

namespace DnsClient.Internal
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public sealed class PooledBytes : IDisposable
    {
        private static readonly ArrayPool<byte> s_pool = ArrayPool<byte>.Create(4096 * 4, 100);
        private int _length;
        private ArraySegment<byte> _buffer;
        private bool _disposed;

        public PooledBytes(int length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            _length = length;
            _buffer = new ArraySegment<byte>(s_pool.Rent(length), 0, _length);
        }

        public void Extend(int length)
        {
            var newBuffer = s_pool.Rent(_length + length);

            System.Buffer.BlockCopy(_buffer.Array, 0, newBuffer, 0, _length);
            s_pool.Return(_buffer.Array);
            _length += length;
            _buffer = new ArraySegment<byte>(newBuffer, 0, _length);
        }

        public byte[] Buffer
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(PooledBytes));
                }

                return _buffer.Array;
            }
        }

        public ArraySegment<byte> BufferSegment
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(PooledBytes));
                }

                return _buffer;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                s_pool.Return(_buffer.Array, clearArray: true);
            }
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
