using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DnsClient.Protocol;

namespace DnsClient
{
    public abstract class DnsMessageHandler : IDisposable
    {
        private bool _disposedValue = false;

        public abstract Task<DnsResponseMessage> QueryAsync(IPEndPoint server, DnsRequestMessage request, CancellationToken cancellationToken);

        public abstract bool IsTransientException<T>(T exception) where T : Exception;

        public virtual byte[] GetRequestData(DnsRequestMessage request)
        {
            var question = request.Question;
            var questionData = question.QueryName.AsBytes();
            
            // 4 more bytes for the type and class
            var writer = new DnsDatagramWriter(DnsRequestHeader.HeaderLength + questionData.Length + 4);

            writer.SetInt16Network((short)request.Header.Id);
            writer.SetUInt16Network(request.Header.RawFlags);
            writer.SetInt16Network((short)request.Header.QuestionCount);

            // jump to end of header, we didn't write all fields
            writer.Index = DnsRequestHeader.HeaderLength;

            writer.SetBytes(questionData, questionData.Length);
            writer.SetUInt16Network((ushort)question.QuestionType);
            writer.SetUInt16Network((ushort)question.QuestionClass);

            return writer.Data;
        }

        public virtual DnsResponseMessage GetResponseMessage(byte[] responseData)
        {
            var reader = new DnsDatagramReader(responseData);
            var factory = new DnsRecordFactory(reader);

            var id = reader.ReadUInt16Reverse();
            var flags = reader.ReadUInt16Reverse();
            var questionCount = reader.ReadUInt16Reverse();
            var answerCount = reader.ReadUInt16Reverse();
            var nameServerCount = reader.ReadUInt16Reverse();
            var additionalCount = reader.ReadUInt16Reverse();

            var header = new DnsResponseHeader(id, flags, questionCount, answerCount, additionalCount, nameServerCount);
            var response = new DnsResponseMessage(header);

            for (int questionIndex = 0; questionIndex < questionCount; questionIndex++)
            {
                var question = new DnsQuestion(reader.ReadName(), (QueryType)reader.ReadUInt16Reverse(), (QueryClass)reader.ReadUInt16Reverse());
                response.AddQuestion(question);
            }

            for (int answerIndex = 0; answerIndex < answerCount; answerIndex++)
            {
                var info = factory.ReadRecordInfo();
                var record = factory.GetRecord(info);
                response.AddAnswer(record);
            }

            for (int serverIndex = 0; serverIndex < nameServerCount; serverIndex++)
            {
                var info = factory.ReadRecordInfo();
                var record = factory.GetRecord(info);
                response.AddAuthority(record);
            }

            for (int additionalIndex = 0; additionalIndex < additionalCount; additionalIndex++)
            {
                var info = factory.ReadRecordInfo();
                var record = factory.GetRecord(info);
                response.AddAdditional(record);
            }

            return response;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                }

                _disposedValue = true;
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
        }
    }
}