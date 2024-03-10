﻿using System;
using System.Security.Cryptography;
using DnsClient.Protocol;

namespace DnsClient
{
    internal class DnsRequestHeader
    {
        public const int HeaderLength = 12;
        private ushort _flags = 0;

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

        public int Id { get; private set; }

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

        private static ushort GetNextUniqueId()
        {
#if NET6_0_OR_GREATER
            return (ushort)Random.Shared.Next(1, ushort.MaxValue);
        }

#else

            return GetRandom();
        }

        // Full Framework and netstandard 2.x do not have Random.Shared
        private static readonly RandomNumberGenerator s_generator = RandomNumberGenerator.Create();

        private static ushort GetRandom()
        {
            var block = new byte[2];
            s_generator.GetBytes(block);
            return BitConverter.ToUInt16(block, 0);
        }
#endif
    }
}