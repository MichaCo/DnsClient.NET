using System;
using System.Buffers;
using System.Linq;

namespace DnsClient
{
    internal class PooledBytes : IDisposable
    {
        private static readonly ArrayPool<byte> _pool = ArrayPool<byte>.Create();

        private readonly byte[] _buffer;

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
            get { return _buffer; }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pool.Return(_buffer);
            }
        }
    }
}