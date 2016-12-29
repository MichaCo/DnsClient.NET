using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DnsClient.Protocol;
using DnsClient.Protocol.Options;

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
            var questionData = question.QueryName.GetBytes();

            /*
                                    1  1  1  1  1  1
      0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |                      ID                       |
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |QR|   Opcode  |AA|TC|RD|RA|   Z    |   RCODE   |
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |                    QDCOUNT                    |
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |                    ANCOUNT                    |
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |                    NSCOUNT                    |
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |                    ARCOUNT                    |
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
             * */
            // 4 more bytes for the type and class
            var writer = new DnsDatagramWriter(DnsRequestHeader.HeaderLength + questionData.Length + 4);

            writer.SetInt16Network((short)request.Header.Id);
            writer.SetUInt16Network(request.Header.RawFlags);
            writer.SetInt16Network(1);   // we support single question only... (as most DNS servers anyways).
            writer.SetInt16Network(0);
            writer.SetInt16Network(0);
            writer.SetInt16Network(1); // one additional for the Opt record.

            // jump to end of header, we didn't write all fields
            writer.Index = DnsRequestHeader.HeaderLength;

            writer.SetBytes(questionData, questionData.Length);
            writer.SetUInt16Network((ushort)question.QuestionType);
            writer.SetUInt16Network((ushort)question.QuestionClass);

            /*
       +------------+--------------+------------------------------+
       | Field Name | Field Type   | Description                  |
       +------------+--------------+------------------------------+
       | NAME       | domain name  | MUST be 0 (root domain)      |
       | TYPE       | u_int16_t    | OPT (41)                     |
       | CLASS      | u_int16_t    | requestor's UDP payload size |
       | TTL        | u_int32_t    | extended RCODE and flags     |
       | RDLEN      | u_int16_t    | length of all RDATA          |
       | RDATA      | octet stream | {attribute,value} pairs      |
       +------------+--------------+------------------------------+
             * */
            var opt = new OptRecord();
            var nameBytes = opt.DomainName.GetBytes();
            writer.Extend(nameBytes.Length + 2 + 2 + 4 + 2);
            writer.SetBytes(nameBytes, nameBytes.Length);
            writer.SetUInt16Network((ushort)opt.RecordType);
            writer.SetUInt16Network((ushort)opt.RecordClass);
            writer.SetUInt32Network((ushort)opt.TimeToLive);
            writer.SetUInt16Network(0);

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
            var response = new DnsResponseMessage(header, responseData.Length);

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