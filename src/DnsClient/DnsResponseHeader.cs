namespace DnsClient
{
    public class DnsResponseHeader
    {
        private readonly ushort _flags = 0;

        public int AdditionalCount { get; }

        public int AnswerCount { get; }

        public bool FutureUse => HasFlag(DnsHeaderFlag.FutureUse);

        public bool HasAuthorityAnswer => HasFlag(DnsHeaderFlag.HasAuthorityAnswer);

        public DnsHeaderFlag HeaderFlags => (DnsHeaderFlag)_flags;

        public int Id { get; }

        public bool IsAuthenticData => HasFlag(DnsHeaderFlag.IsAuthenticData);

        public bool IsCheckingDisabled => HasFlag(DnsHeaderFlag.IsCheckingDisabled);

        public bool IsResponse => !HasFlag(DnsHeaderFlag.IsQuery);

        public int NameServerCount { get; }

        public DnsOpCode OPCode => (DnsOpCode)((DnsHeader.OPCODE_MASK & _flags) >> DnsHeader.OPCODE_SHIFT);

        public int QuestionCount { get; }

        public bool RecursionAvailable => HasFlag(DnsHeaderFlag.RecursionAvailable);

        public DnsResponseCode ResponseCode => (DnsResponseCode)(_flags & DnsHeader.RCODE_MASK);

        ////ResponseCode {set
        ////{
        ////    _flags &= (ushort)~(DnsHeader.RCODE_MASK);
        ////    _flags |= (ushort)((ushort)value & DnsHeader.RCODE_MASK);
        ////}}

        public bool ResultTruncated => HasFlag(DnsHeaderFlag.ResultTruncated);

        public bool UseRecursion => HasFlag(DnsHeaderFlag.RecursionDesired);

        public DnsResponseHeader(int id, ushort flags, int questionCount, int answerCount, int additionalCount, int serverCount)
        {
            Id = id;
            _flags = flags;
            QuestionCount = questionCount;
            AnswerCount = answerCount;
            AdditionalCount = additionalCount;
            NameServerCount = serverCount;
        }

        private bool HasFlag(DnsHeaderFlag flag) => (HeaderFlags & flag) != 0;
    }
}