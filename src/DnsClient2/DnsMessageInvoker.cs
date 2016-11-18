using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DnsClient2
{
    public abstract class DnsMessageInvoker
    {
        public abstract Task<DnsResponseMessage> QueryAsync(IPEndPoint server, DnsRequestMessage request, CancellationToken cancellationToken);

        public virtual IEnumerable<byte> GetRequestHeaderData(DnsRequestHeader header)
        {

            yield return GetBytesInNetworkOrder(header.Id);
                //data.AddRange(GetBytes(_flags));
                //data.AddRange(GetBytes(QuestionCount));
                //data.AddRange(GetBytes(AnswerCount));
                //data.AddRange(GetBytes(NameServerCount));
                //data.AddRange(GetBytes(AdditionalCount));
                //return data.ToArray();
        }

        private byte[] GetBytesInNetworkOrder(ushort value)
        {
            return BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)value));
        }
    }
}