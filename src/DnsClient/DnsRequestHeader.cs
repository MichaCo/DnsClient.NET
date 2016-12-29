using System;

namespace DnsClient
{
    public class DnsRequestHeader
    {
        public const int HeaderLength = 12;

        private ushort _flags = 0;

        [CLSCompliant(false)]
        public ushort RawFlags => _flags;

        [CLSCompliant(false)]
        public DnsHeaderFlag HeaderFlags
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

        public int Id { get; set; }

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

        [CLSCompliant(false)]
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
            get { return (HeaderFlags | DnsHeaderFlag.RecursionDesired) != 0; }
            set
            {
                HeaderFlags |= DnsHeaderFlag.RecursionDesired;
            }
        }

        public DnsRequestHeader(int id, DnsOpCode queryKind)
            : this(id, true, queryKind)
        {
        }

        public DnsRequestHeader(int id, bool useRecursion, DnsOpCode queryKind)
        {
            Id = id;
            OpCode = queryKind;
            UseRecursion = useRecursion;            
        }

        public override string ToString()
        {
            return $"{Id} - Qs: {1} Recursion: {UseRecursion} OpCode: {OpCode}";
        }
    }
}