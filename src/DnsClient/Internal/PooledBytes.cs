using System;
using System.Buffers;
using System.Linq;

namespace DnsClient.Internal
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class PooledBytes : IDisposable
    {
        private static readonly ArrayPool<byte> _pool = ArrayPool<byte>.Create(4096 * 2, 200);

        private readonly byte[] _buffer;
        private bool _disposed = false;

        public PooledBytes(int length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            _buffer = _pool.Rent(length);
        }

        public byte[] Buffer
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(Buffer));
                }

                return _buffer;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _disposed = true;
                _pool.Return(_buffer);
            }
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}