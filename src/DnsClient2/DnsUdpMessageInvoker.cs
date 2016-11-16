using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DnsClient2
{
    public class DnsUdpMessageInvoker : DnsMessageInvoker
    {
        public override async Task<DnsResponseMessage> QueryAsync(
            IPEndPoint server,
            DnsRequestMessage request, 
            CancellationToken cancellationToken)
        {
            return null;
        }
    }
}
