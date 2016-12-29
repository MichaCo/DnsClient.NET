using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DnsClient.Protocol;
using DnsClient.Protocol.Options;

namespace DnsClient
{
    internal abstract class DnsMessageHandler
    {
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

            writer.WriteInt16NetworkOrder((short)request.Header.Id);
            writer.WriteUInt16NetworkOrder(request.Header.RawFlags);
            writer.WriteInt16NetworkOrder(1);   // we support single question only... (as most DNS servers anyways).
            writer.WriteInt16NetworkOrder(0);
            writer.WriteInt16NetworkOrder(0);
            writer.WriteInt16NetworkOrder(1); // one additional for the Opt record.

            // jump to end of header, we didn't write all fields
            writer.Index = DnsRequestHeader.HeaderLength;

            writer.WriteBytes(questionData, questionData.Length);
            writer.WriteUInt16NetworkOrder((ushort)question.QuestionType);
            writer.WriteUInt16NetworkOrder((ushort)question.QuestionClass);

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
            writer.WriteBytes(nameBytes, nameBytes.Length);
            writer.WriteUInt16NetworkOrder((ushort)opt.RecordType);
            writer.WriteUInt16NetworkOrder((ushort)opt.RecordClass);
            writer.WriteUInt32NetworkOrder((ushort)opt.TimeToLive);
            writer.WriteUInt16NetworkOrder(0);

            return writer.Data;
        }

        public virtual DnsResponseMessage GetResponseMessage(byte[] responseData)
        {
            var reader = new DnsDatagramReader(responseData);
            var factory = new DnsRecordFactory(reader);

            var id = reader.ReadUInt16NetworkOrder();
            var flags = reader.ReadUInt16NetworkOrder();
            var questionCount = reader.ReadUInt16NetworkOrder();
            var answerCount = reader.ReadUInt16NetworkOrder();
            var nameServerCount = reader.ReadUInt16NetworkOrder();
            var additionalCount = reader.ReadUInt16NetworkOrder();

            var header = new DnsResponseHeader(id, flags, questionCount, answerCount, additionalCount, nameServerCount);
            var response = new DnsResponseMessage(header, responseData.Length);

            for (int questionIndex = 0; questionIndex < questionCount; questionIndex++)
            {
                var question = new DnsQuestion(reader.ReadName(), (QueryType)reader.ReadUInt16NetworkOrder(), (QueryClass)reader.ReadUInt16NetworkOrder());
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
    }
}