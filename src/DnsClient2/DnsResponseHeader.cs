using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DnsClient2
{
    public class DnsResponseHeader
    {
        private readonly object _flags;

        public ushort AdditionalCount { get; private set; }

        public ushort AnswerCount { get; private set; }

        public ushort Id { get; private set; }

        public ushort NameServerCount { get; private set; }

        public ushort QuestionCount { get; private set; }


        // 0 indicating query, 1 indicating response
        public ushort QRFlag { get; } = 0;
    }
}
