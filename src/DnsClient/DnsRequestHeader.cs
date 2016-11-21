namespace DnsClient
{
    public class DnsRequestHeader
    {
        public const int HeaderLength = 12;

        private ushort _flags = 0;

        public ushort RawFlags => _flags;

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
                _flags &= (ushort)~(DnsHeaderFlag.IsQuery);
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
                return (DnsOpCode)((DnsHeader.OPCODE_MASK & _flags) >> DnsHeader.OPCODE_SHIFT);
            }
            set
            {
                _flags &= (ushort)~(DnsHeader.OPCODE_MASK);
                _flags |= (ushort)(((ushort)value << DnsHeader.OPCODE_SHIFT) & DnsHeader.OPCODE_MASK);
            }
        }

        public int QuestionCount { get; set; }

        public bool UseRecursion
        {
            get { return (HeaderFlags | DnsHeaderFlag.RecursionDesired) != 0; }
            set
            {
                HeaderFlags |= DnsHeaderFlag.RecursionDesired;
            }
        }

        public DnsRequestHeader(int id, int questionCount, DnsOpCode queryKind)
            : this(id, questionCount, true, queryKind)
        {
        }

        public DnsRequestHeader(int id, int questionCount, bool useRecursion, DnsOpCode queryKind)
        {
            Id = id;
            QuestionCount = questionCount;
            OpCode = queryKind;
            UseRecursion = useRecursion;
        }

        public override string ToString()
        {
            return $"{Id} - Qs: {QuestionCount} Recursion: {UseRecursion} OpCode: {OpCode}";
        }
    }
}