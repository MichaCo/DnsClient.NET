// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Threading;
using DnsClient.Protocol;

namespace DnsClient
{
    internal class ThreadSafeFixedBitmap
    {
        private static readonly int[] s_cachedShifts = new int[]
        {
            1,2,4,8,16,32,64,128,256,512,1024,2048,4096,8192,16384,32768,65536,131072,262144,524288,1048576,2097152,4194304,8388608,16777216,33554432,67108864,134217728,268435456,536870912,1073741824,-2147483648
        };

        private readonly int _length = ushort.MaxValue;
        private readonly int[] _arr = new int[2048]; // 65536 / 32 = 2048

        public ThreadSafeFixedBitmap()
        {
        }

        public int Length
        {
            get { return _length; }
        }

        public bool Get(int index)
        {
            if (index < 0 || index >= _length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            }

            return (Interlocked.CompareExchange(ref _arr[index >> 5], 0, 0) & (s_cachedShifts[index & 31])) != 0;
        }

        public int FindFirstFalse()
        {
            for (int i = 0; i < _arr.Length; i++)
            {
                if (_arr[i] != -1) // All bits set to 1
                {
                    for (int j = 0; j < 32; j++)
                    {
                        if ((_arr[i] & s_cachedShifts[j]) == 0)
                        {
                            var foundIndex = (i << 5) + j;

                            if (foundIndex < 0 || foundIndex >= ushort.MaxValue)
                            {
                                // Indicate that no false bit was found.
                                return -1;
                            }

                            return foundIndex;
                        }
                    }
                }
            }

            // Indicate that no false bit was found.
            return -1;
        }

        public bool TrySet(int index, bool value)
        {
            if (index < 0 || index >= _length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Index is out of range: {index}.");
            }

            var prevValue = Interlocked.CompareExchange(ref _arr[index >> 5], 0, 0);
            var prevBool = (prevValue & (s_cachedShifts[index & 31])) != 0;

            if (prevBool == value)
            {
                // Value is already set to the desired state, cannot change it.
                return false;
            }

            if (value)
            {
                var r = Interlocked.CompareExchange(ref _arr[index >> 5], prevValue | (s_cachedShifts[index & 31]), prevValue) == prevValue;
                return r;
            }

            return Interlocked.CompareExchange(ref _arr[index >> 5], prevValue & ~(s_cachedShifts[index & 31]), prevValue) == prevValue;
        }
    }

    internal class QueryId : IDisposable
    {
        private static readonly Random s_random = new Random();
        private static ThreadSafeFixedBitmap s_usedIds = new ThreadSafeFixedBitmap();

        public static QueryId NewId()
        {
            int nextIndex = -1;
            int tries = 0;
            do
            {
#pragma warning disable CA5394 // Do not use insecure randomness
                nextIndex = tries < 100
                    ? s_random.Next(0, ushort.MaxValue)
                    : s_usedIds.FindFirstFalse();
#pragma warning restore CA5394 // Do not use insecure randomness

                if (nextIndex == -1)
                {
                    // For some reason, the bitmap is full, reset it.
                    Reset();
                    throw new InvalidOperationException("No free query ids available.");
                }

                tries++;
            }
            while (s_usedIds.TrySet(nextIndex, true) == false);

            return new QueryId { Id = nextIndex + 1 };
        }

        public int Id { get; private set; }

        public void Dispose()
        {
            s_usedIds.TrySet(Id - 1, false);
        }

        public static void Reset()
        {
            s_usedIds = new ThreadSafeFixedBitmap();
        }

        public static implicit operator int(QueryId id)
        {
            if (id is null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            return id.Id;
        }
    }

    internal class DnsRequestHeader : IDisposable
    {
        public const int HeaderLength = 12;
        private ushort _flags;

        public ushort RawFlags => _flags;

        internal DnsHeaderFlag HeaderFlags
        {
            get
            {
                return (DnsHeaderFlag)_flags;
            }
            set
            {
                _flags &= (ushort)~(DnsHeaderFlag.IsCheckingDisabled);
                _flags &= (ushort)~(DnsHeaderFlag.IsAuthenticData);
                _flags &= (ushort)~(DnsHeaderFlag.FutureUse);
                _flags &= (ushort)~(DnsHeaderFlag.HasQuery);
                _flags &= (ushort)~(DnsHeaderFlag.HasAuthorityAnswer);
                _flags &= (ushort)~(DnsHeaderFlag.ResultTruncated);
                _flags &= (ushort)~(DnsHeaderFlag.RecursionDesired);
                _flags &= (ushort)~(DnsHeaderFlag.RecursionAvailable);
                _flags |= (ushort)value;
            }
        }

        public QueryId Id { get; private set; }

        public DnsOpCode OpCode
        {
            get
            {
                return (DnsOpCode)((DnsHeader.OPCodeMask & _flags) >> DnsHeader.OPCodeShift);
            }
            set
            {
                _flags &= (ushort)~(DnsHeader.OPCodeMask);
                _flags |= (ushort)(((ushort)value << DnsHeader.OPCodeShift) & DnsHeader.OPCodeMask);
            }
        }

        public ushort RCode
        {
            get
            {
                return (ushort)(DnsHeader.RCodeMask & _flags);
            }
            set
            {
                _flags &= (ushort)~(DnsHeader.RCodeMask);
                _flags |= (ushort)(value & DnsHeader.RCodeMask);
            }
        }

        public bool UseRecursion
        {
            get
            {
                return HeaderFlags.HasFlag(DnsHeaderFlag.RecursionDesired);
            }
            set
            {
                if (value)
                {
                    _flags |= (ushort)DnsHeaderFlag.RecursionDesired;
                }
                else
                {
                    _flags &= (ushort)~(DnsHeaderFlag.RecursionDesired);
                }
            }
        }

        public DnsRequestHeader(DnsOpCode queryKind)
            : this(true, queryKind)
        {
        }

        public DnsRequestHeader(bool useRecursion, DnsOpCode queryKind)
        {
            Id = GetNextUniqueId();
            OpCode = queryKind;
            UseRecursion = useRecursion;
        }

        public override string ToString()
        {
            return $"{Id} - Qs: {1} Recursion: {UseRecursion} OpCode: {OpCode}";
        }

        public void RefreshId()
        {
            Id = GetNextUniqueId();
        }

        private static QueryId GetNextUniqueId()
        {
            return QueryId.NewId();
        }

        public void Dispose()
        {
            // Dispose the QueryId if it is not null
            Id?.Dispose();
        }
    }
}
