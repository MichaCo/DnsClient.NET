using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DnsClient2
{
    public abstract class DnsMessageInvoker
    {
        public abstract Task<DnsResponseMessage> QueryAsync(DnsEndPoint server, DnsRequestMessage request, CancellationToken cancellationToken);

        public virtual byte[] GetRequestData(DnsRequestMessage request)
        {
            List<byte> data = new List<byte>();
            data.AddRange(GetBytesInNetworkOrder(request.Header.Id));
            data.AddRange(GetBytesInNetworkOrder(256));
            data.AddRange(GetBytesInNetworkOrder(request.Header.QuestionCount));
            data.AddRange(GetBytesInNetworkOrder(0));
            data.AddRange(GetBytesInNetworkOrder(0));
            data.AddRange(GetBytesInNetworkOrder(0));
            foreach (var question in request.Questions)
            {
                data.AddRange(Encoding.ASCII.GetBytes("\u0006google\u0003com\0"));
                data.AddRange(GetBytesInNetworkOrder((ushort)255));
                data.AddRange(GetBytesInNetworkOrder((ushort)1));
            }
            
            return data.ToArray();
        }

        public virtual DnsResponseHeader ParseHeader(byte[] responseData)
        {
            var header = new DnsResponseHeader();
            var id = ToUInt16(responseData, 0);

            //Id = rr.ReadUInt16();
            //_flags = rr.ReadUInt16();
            //QuestionCount = rr.ReadUInt16();
            //AnswerCount = rr.ReadUInt16();
            //NameServerCount = rr.ReadUInt16();
            //AdditionalCount = rr.ReadUInt16();


            return new DnsResponseHeader();
        }

        protected ushort ToUInt16(byte[] data, int startIndex)
        {
            if (data.Length < startIndex + 2)
            {
                throw new ArgumentOutOfRangeException(nameof(data));
            }

            Array.Reverse(data, startIndex, 2);
            return BitConverter.ToUInt16(data, startIndex);
        }

        protected ushort GetUInt16(byte[] twoBytes)
        {
            if (twoBytes.Length != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(twoBytes));
            }

            Array.Reverse(twoBytes);
            return BitConverter.ToUInt16(twoBytes, 0);
        }

        protected byte[] GetBytesInNetworkOrder(ushort value)
        {
            return BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)value));
        }

        private byte[] Slice(byte[] data, int start, int length)
        {
            var result = new byte[length];
            Array.ConstrainedCopy(data, start, result, 0, length);
            return result;
        }
    }
}