using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DnsClient2
{
    public abstract class DnsMessageInvoker
    {
        public abstract Task<DnsResponseMessage> QueryAsync(DnsEndPoint server, DnsRequestMessage request, CancellationToken cancellationToken);

        protected virtual byte[] GetRequestData(DnsRequestMessage request)
        {
            var writer = new DnsDatagramWriter(DnsRequestHeader.HeaderLength);

            writer.SetShortNetwork((short)request.Header.Id);
            writer.SetUShortNetwork(request.Header.RawFlags);
            writer.SetShortNetwork((short)request.Header.QuestionCount);

            // jump to end of header, we didn't write all fields
            writer.Offset = DnsRequestHeader.HeaderLength;

            foreach (var question in request.Questions)
            {
                var questionData = question.QueryName.ToBytes();

                // 4 more bytes for the type and class
                writer = writer.Extend(questionData.Length + 4);
                writer.SetBytes(questionData, questionData.Length);
                writer.SetUShortNetwork(question.QuestionType);
                writer.SetUShortNetwork(question.QuestionClass);
            }

            return writer.Data;
        }

        protected virtual DnsResponseHeader ParseHeader(byte[] responseData)
        {
            
            var id = ToUInt16(responseData, 0);
            var flags = ToUInt16(responseData, 2);

            var header = new DnsResponseHeader(id, flags, 1, 1, 0, 1);

            //Id = rr.ReadUInt16();
            //_flags = rr.ReadUInt16();
            //QuestionCount = rr.ReadUInt16();
            //AnswerCount = rr.ReadUInt16();
            //NameServerCount = rr.ReadUInt16();
            //AdditionalCount = rr.ReadUInt16();

            return header;
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
    }
}