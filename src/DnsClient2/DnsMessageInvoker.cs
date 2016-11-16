using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DnsClient2
{
    public abstract class DnsMessageInvoker
    {
        public abstract Task<DnsResponseMessage> QueryAsync(IPEndPoint server, DnsRequestMessage request, CancellationToken cancellationToken);
    }
}