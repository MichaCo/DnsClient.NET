using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DnsClient2
{
    public class DnsRequestHeader
    {
        public ushort Id { get; private set; }
        
        public ushort QuestionCount { get; private set; }
    }
}
