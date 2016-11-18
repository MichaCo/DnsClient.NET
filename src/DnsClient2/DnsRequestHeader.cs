using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DnsClient2
{
    public class DnsRequestHeader
    {
        public DnsRequestHeader(ushort id, ushort questionCount, bool useRecursion, QueryKind queryKind)
        {
            Id = id;
            QuestionCount = questionCount;
            RecursionDesired = useRecursion;
            OpCode = queryKind;
        }

        public ushort Id { get; set; }

        public ushort QuestionCount { get; set; }

        public bool RecursionDesired { get; } = true;

        // 0 indicating query, 1 indicating response
        public byte QRFlag { get; } = 0;

        public QueryKind OpCode { get; } = QueryKind.Query;

        // reservced for future use, must be zero.
        private ushort ZFlag { get; } = 0;
    }
}
