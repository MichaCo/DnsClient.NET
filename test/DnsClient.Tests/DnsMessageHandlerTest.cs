using System.Linq;
using System.Net;
using DnsClient.Protocol;
using Xunit;

namespace DnsClient.Tests
{
    public class DnsMessageHandlerTest
    {
        [Fact]
        public void DnsRecordFactory_ResolveARecord()
        {
            var header = new DnsResponseHeader(42, 256, 0, 1, 0, 0);
            var responseMessage = new DnsResponseMessage(header, 0);

            var info = new ResourceRecordInfo("query", ResourceRecordType.A, QueryClass.IN, 100, 4);
            var ip = IPAddress.Parse("123.45.67.9");
            var answer = new ARecord(info, ip);
            responseMessage.AddAnswer(answer);
            var response = responseMessage.AsQueryResponse(new NameServer(ip));

            var answerBytes = ip.GetAddressBytes();

            var raw = GetResponseBytes(response, answerBytes);

            var handle = new DnsUdpMessageHandler(true);
            var result = handle.GetResponseMessage(new System.ArraySegment<byte>(raw)).AsQueryResponse(new NameServer(ip));

            Assert.Equal(result.Answers.Count, 1);
            var resultAnswer = result.Answers.OfType<ARecord>().First();
            Assert.Equal(resultAnswer.Address.ToString(), ip.ToString());
            Assert.Equal(resultAnswer.DomainName.Value, "query.");
            Assert.Equal(resultAnswer.RawDataLength, 4);
            Assert.Equal(resultAnswer.RecordClass, QueryClass.IN);
            Assert.Equal(resultAnswer.RecordType, ResourceRecordType.A);
            Assert.True(resultAnswer.TimeToLive == 100);
            Assert.True(result.Header.Id == 42);
            Assert.True(result.Header.AnswerCount == 1);
        }

        private static byte[] GetResponseBytes(DnsQueryResponse message, byte[] answerData)
        {
            using (var writer = new DnsDatagramWriter())
            {
                writer.WriteUInt16NetworkOrder((ushort)message.Header.Id);
                writer.WriteUInt16NetworkOrder((ushort)message.Header.HeaderFlags);
                // lets iterate answers only, makse it easier
                //writer.WriteUInt16Network((ushort)message.Header.QuestionCount);
                writer.WriteUInt16NetworkOrder(0);
                writer.WriteUInt16NetworkOrder(1);
                //writer.WriteUInt16Network((ushort)message.Header.NameServerCount);
                writer.WriteUInt16NetworkOrder(0);
                //writer.WriteUInt16Network((ushort)message.Header.AdditionalCount);
                writer.WriteUInt16NetworkOrder(0);

                var answer = message.Answers.First();                
                writer.WriteHostName(answer.DomainName.Value);                                
                writer.WriteUInt16NetworkOrder((ushort)answer.RecordType);
                writer.WriteUInt16NetworkOrder((ushort)answer.RecordClass);
                writer.WriteUInt32NetworkOrder((uint)answer.TimeToLive);
                writer.WriteUInt16NetworkOrder((ushort)answerData.Length);

                //writer.Extend(answerData.Length);   // the following data->length
                writer.WriteBytes(answerData, answerData.Length);

                return writer.Data.ToArray();
            }
        }
    }
}