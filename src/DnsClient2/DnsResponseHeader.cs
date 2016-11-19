using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DnsClient2
{
    public class DnsResponseHeader
    {
        private readonly ushort _flags;
        
        public ushort AdditionalCount { get; }

        public ushort AnswerCount { get; }

        public ushort Id { get; }

        public ushort NameServerCount { get; }

        public ushort QuestionCount { get; }
        
        // 0 indicating query, 1 indicating response
        public ushort QRFlag { get; } = 1;
    }
}
