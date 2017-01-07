using System;
using System.Buffers;
using System.Linq;
using System.Threading;

namespace DnsClient
{
    internal class PooledBytes : IDisposable
    {
        private static readonly ArrayPool<byte> _pool = ArrayPool<byte>.Create(4096, 500);

        private readonly byte[] _buffer;

        public PooledBytes(int length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            //_buffer = new byte[length];
            _buffer = _pool.Rent(length);
            Interlocked.Increment(ref StaticLog.ByteArrayAllocations);
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
                Interlocked.Increment(ref StaticLog.ByteArrayReleases);
            }
        }
    }
}