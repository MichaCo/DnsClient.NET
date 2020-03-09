﻿using System;
using System.Linq;
using System.Net;
using DnsClient.Protocol;
using DnsClient.Protocol.Options;
using Xunit;

namespace DnsClient.Tests
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class DnsMessageHandlerTest
    {
        // https://github.com/MichaCo/DnsClient.NET/issues/51
        [Fact]
        public void DnsRecordFactory_Issue51()
        {
            var value = "UsWBgAABAAMAAAAECF9tb25nb2RiBF90Y3ANcXNsLWRldi1wYXJ2dQVhenVyZQdtb25nb2RiA25ldAAAIQABCF9tb25nb2RiBF90Y3ANcXNsLWRldi1wYXJ2dQVhenVyZQdtb25nb2RiA25ldAAAIQABAAAAHgAzAAAAAGmJGXFzbC1kZXYtc2hhcmQtMDAtMDAtcGFydnUFYXp1cmUHbW9uZ29kYgNuZXQACF9tb25nb2RiBF90Y3ANcXNsLWRldi1wYXJ2dQVhenVyZQdtb25nb2RiA25ldAAAIQABAAAAHgAzAAAAAGmJGXFzbC1kZXYtc2hhcmQtMDAtMDEtcGFydnUFYXp1cmUHbW9uZ29kYgNuZXQACF9tb25nb2RiBF90Y3ANcXNsLWRldi1wYXJ2dQVhenVyZQdtb25nb2RiA25ldAAAIQABAAAAHgAzAAAAAGmJGXFzbC1kZXYtc2hhcmQtMDAtMDItcGFydnUFYXp1cmUHbW9uZ29kYgNuZXQAAAApEAAAAAAAABT/nAAQnAgTYxmUVUGBo1503OWO6Blxc2wtZGV2LXNoYXJkLTAwLTAxLXBhcnZ1BWF6dXJlB21vbmdvZGIDbmV0AAABAAEAAAAeAAQoVtVbGXFzbC1kZXYtc2hhcmQtMDAtMDAtcGFydnUFYXp1cmUHbW9uZ29kYgNuZXQAAAEAAQAAAB4ABChFZfIZcXNsLWRldi1zaGFyZC0wMC0wMi1wYXJ2dQVhenVyZQdtb25nb2RiA25ldAAAAQABAAAAHgAENOVxjwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==";
            var bytes = Convert.FromBase64String(value);
            var handle = new DnsUdpMessageHandler(true);
            var result = handle.GetResponseMessage(new ArraySegment<byte>(bytes)).AsQueryResponse(new NameServer(IPAddress.Parse("127.0.0.1")), null);

            Assert.Equal(20, result.Additionals.OfType<OptRecord>().First().Data.Length);
        }

        // TODO: decide what to do & test
        // https://github.com/MichaCo/DnsClient.NET/issues/52
        [Fact(Skip = "fails")]
        public void DnsRecordFactory_Issue52()
        {
            var data = new byte[] { 31, 169, 129, 128, 0, 1, 0, 18, 0, 0, 0, 1, 20, 95, 97, 99, 109, 101, 45, 99, 104, 97, 108, 108, 101, 110, 103, 101, 45, 116, 101, 115, 116, 8, 97, 122, 117, 114, 101, 100, 110, 115, 4, 115, 105, 116, 101, 0, 0, 16, 0, 1, 192, 12, 0, 16, 0, 1, 0, 0, 0, 60, 0, 42, 41, 87, 84, 98, 112, 74, 121, 122, 81, 87, 112, 85, 112, 82, 97, 45, 108, 73, 95, 118, 73, 95, 121, 51, 95, 68, 101, 75, 80, 48, 77, 114, 109, 121, 87, 107, 55, 86, 85, 76, 82, 51, 192, 12, 0, 16, 0, 1, 0, 0, 0, 60, 0, 42, 41, 87, 84, 98, 112, 74, 121, 122, 81, 87, 112, 85, 112, 82, 97, 45, 108, 73, 95, 118, 73, 95, 121, 51, 95, 68, 101, 75, 80, 48, 77, 114, 109, 121, 87, 107, 55, 86, 85, 76, 82, 52, 192, 12, 0, 16, 0, 1, 0, 0, 0, 60, 0, 43, 42, 87, 84, 98, 112, 74, 121, 122, 81, 87, 112, 85, 100, 45, 108, 73, 95, 118, 73, 95, 121, 51, 95, 68, 101, 75, 80, 48, 77, 114, 109, 121, 87, 107, 55, 86, 85, 76, 82, 118, 66, 73, 57, 192, 12, 0, 16, 0, 1, 0, 0, 0, 60, 0, 43, 42, 87, 84, 98, 112, 74, 121, 122, 81, 87, 112, 85, 112, 82, 97, 45, 108, 73, 95, 118, 73, 95, 121, 51, 95, 68, 101, 75, 80, 48, 77, 114, 109, 121, 87, 107, 55, 86, 85, 76, 82, 118, 97, 192, 12, 0, 16, 0, 1, 0, 0, 0, 60, 0, 43, 42, 87, 84, 98, 112, 74, 121, 122, 81, 87, 112, 85, 112, 82, 97, 45, 108, 73, 95, 118, 73, 95, 121, 51, 95, 68, 101, 75, 80, 48, 77, 114, 109, 121, 87, 107, 55, 86, 85, 76, 82, 118, 115, 192, 12, 0, 16, 0, 1, 0, 0, 0, 60, 0, 43, 42, 87, 84, 98, 112, 74, 121, 122, 81, 87, 112, 85, 112, 82, 97, 45, 108, 73, 95, 118, 73, 95, 121, 51, 95, 68, 101, 75, 80, 48, 77, 114, 109, 121, 87, 107, 55, 86, 85, 76, 82, 118, 119, 192, 12, 0, 16, 0, 1, 0, 0, 0, 60, 0, 43, 42, 87, 84, 98, 112, 74, 121, 122, 81, 87, 112, 85, 112, 82, 97, 45, 108, 73, 95, 118, 73, 95, 121, 51, 95, 68, 101, 75, 80, 48, 77, 114, 109, 121, 87, 107, 55, 86, 85, 76, 82, 119, 101, 192, 12, 0, 16, 0, 1, 0, 0, 0, 60, 0, 44, 43, 87, 84, 98, 112, 74, 121, 122, 81, 87, 112, 85, 112, 82, 97, 45, 108, 73, 95, 118, 73, 95, 121, 51, 95, 68, 101, 75, 80, 48, 77, 114, 109, 121, 87, 52, 55, 86, 85, 76, 82, 118, 66, 73, 192, 12, 0, 16, 0, 1, 0, 0, 0, 60, 0, 44, 43, 87, 84, 98, 112, 74, 121, 122, 81 };
            var handle = new DnsUdpMessageHandler(true);
            var result = handle.GetResponseMessage(new System.ArraySegment<byte>(data)).AsQueryResponse(new NameServer(IPAddress.Parse("40.90.4.1")), null);
        }

        [Fact]
        public void DnsRecordFactory_ResolveARecord()
        {
            var header = new DnsResponseHeader(42, 256, 0, 1, 0, 0);
            var responseMessage = new DnsResponseMessage(header, 0);

            var info = new ResourceRecordInfo(DnsString.Parse("query"), ResourceRecordType.A, QueryClass.IN, 100, 4);
            var ip = IPAddress.Parse("123.45.67.9");
            var answer = new ARecord(info, ip);
            responseMessage.AddAnswer(answer);
            var response = responseMessage.AsQueryResponse(new NameServer(ip), null);

            var answerBytes = ip.GetAddressBytes();

            var raw = GetResponseBytes(response, answerBytes);

            var handle = new DnsUdpMessageHandler(true);
            var result = handle.GetResponseMessage(new System.ArraySegment<byte>(raw)).AsQueryResponse(new NameServer(ip), null);

            Assert.Equal(1, result.Answers.Count);
            var resultAnswer = result.Answers.OfType<ARecord>().First();
            Assert.Equal(resultAnswer.Address.ToString(), ip.ToString());
            Assert.Equal("query.", resultAnswer.DomainName.Value);
            Assert.Equal(4, resultAnswer.RawDataLength);
            Assert.Equal(QueryClass.IN, resultAnswer.RecordClass);
            Assert.Equal(ResourceRecordType.A, resultAnswer.RecordType);
            Assert.True(resultAnswer.InitialTimeToLive == 100);
            Assert.True(result.Header.Id == 42);
            Assert.True(result.Header.AnswerCount == 1);
        }

        private static byte[] GetResponseBytes(DnsQueryResponse message, byte[] answerData)
        {
            using (var writer = new DnsDatagramWriter())
            {
                writer.WriteUInt16NetworkOrder((ushort)message.Header.Id);
                writer.WriteUInt16NetworkOrder((ushort)message.Header.HeaderFlags);

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
                writer.WriteUInt32NetworkOrder((uint)answer.InitialTimeToLive);
                writer.WriteUInt16NetworkOrder((ushort)answerData.Length);

                //writer.Extend(answerData.Length);   // the following data->length
                writer.WriteBytes(answerData, answerData.Length);

                return writer.Data.ToArray();
            }
        }
    }
}