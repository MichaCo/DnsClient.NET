using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DnsClient2.Protocol;
using DnsClient2.Protocol.Record;

namespace DnsClient2
{
    public abstract class DnsMessageInvoker
    {
        public static IDictionary<int, Func<ResourceRecordInfo, ResourceRecord>> s_recordFactory =
            new Dictionary<int, Func<ResourceRecordInfo, ResourceRecord>>()
        {
            { 1, (info)=>  new ARecord(info, new IPAddress(info.Data)) }
        };

        public abstract Task<DnsResponseMessage> QueryAsync(DnsEndPoint server, DnsRequestMessage request, CancellationToken cancellationToken);

        protected virtual byte[] GetRequestData(DnsRequestMessage request)
        {
            var writer = new DnsDatagramWriter(DnsRequestHeader.HeaderLength);

            writer.SetInt16Network((short)request.Header.Id);
            writer.SetUInt16Network(request.Header.RawFlags);
            writer.SetInt16Network((short)request.Header.QuestionCount);

            // jump to end of header, we didn't write all fields
            writer.Index = DnsRequestHeader.HeaderLength;

            foreach (var question in request.Questions)
            {
                var questionData = question.QueryName.AsBytes();

                // 4 more bytes for the type and class
                writer.Extend(questionData.Length + 4);
                writer.SetBytes(questionData, questionData.Length);
                writer.SetUInt16Network(question.QuestionType);
                writer.SetUInt16Network(question.QuestionClass);
            }

            return writer.Data;
        }

        protected virtual DnsResponseMessage GetResponseMessage(byte[] responseData)
        {
            var reader = new DnsDatagramReader(responseData);

            var id = reader.ReadUInt16Reverse();
            var flags = reader.ReadUInt16Reverse();
            var questionCount = reader.ReadUInt16Reverse();
            var answerCount = reader.ReadUInt16Reverse();
            var nameServerCount = reader.ReadUInt16Reverse();
            var additionalCount = reader.ReadUInt16Reverse();

            var header = new DnsResponseHeader(id, flags, questionCount, answerCount, additionalCount, nameServerCount);
            var response = new DnsResponseMessage(header);

            // each question has Name, short type, short class
            for (int questionIndex = 0; questionIndex < questionCount; questionIndex++)
            {
                var question = new DnsQuestion(reader.ReadName(), reader.ReadUInt16Reverse(), reader.ReadUInt16Reverse());
                response.AddQuestion(question);
            }

            for (int answerIndex = 0; answerIndex < answerCount; answerIndex++)
            {
                ResourceRecordInfo info = ReadRecordInfo(reader);

                var record = GetRecord(info);
                response.AddAnswer(record);
            }

            for (int serverIndex = 0; serverIndex < nameServerCount; serverIndex++)
            {
                ResourceRecordInfo info = ReadRecordInfo(reader);

                var record = GetRecord(info);
                response.AddServer(record);
            }

            for (int additionalIndex = 0; additionalIndex < additionalCount; additionalIndex++)
            {
                ResourceRecordInfo info = ReadRecordInfo(reader);

                var record = GetRecord(info);
                response.AddAdditional(record);
            }


            return response;
        }

        private ResourceRecord GetRecord(ResourceRecordInfo info)
        {
            ResourceRecord record;

            if (s_recordFactory.ContainsKey(info.RecordType))
            {
                record = s_recordFactory[info.RecordType](info);
            }
            else
            {
                // unknown or base record
                record = new EmptyRecord(info);
            }

            return record;
        }

        /*
        0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        |                                               |
        /                                               /
        /                      NAME                     /
        |                                               |
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        |                      TYPE                     |
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        |                     CLASS                     |
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        |                      TTL                      |
        |                                               |
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        |                   RDLENGTH                    |
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--|
        /                     RDATA                     /
        /                                               /
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
         * */
        private ResourceRecordInfo ReadRecordInfo(DnsDatagramReader reader)
        {
            return new ResourceRecordInfo(
                reader.ReadName(),
                reader.ReadUInt16Reverse(),
                reader.ReadUInt16Reverse(),
                reader.ReadUInt32Reverse(),
                reader.ReadBytes(reader.ReadUInt16Reverse()));
        }
    }

    public class DnsDatagramReader
    {
        private int _index;
        private readonly byte[] _data;

        public bool HasData => (_data.Length - _index) > 0;

        public int Index
        {
            get
            {
                return _index;
            }
            set
            {
                if (value < 0 || value >= _data.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _index = value;
            }
        }

        public DnsDatagramReader(byte[] data, int startIndex = 0)
        {
            _data = data;
            Index = startIndex;
        }

        public IPAddress ReadIPAddress(byte[] data)
        {
            if (data.Length != 4)
            {
                throw new ArgumentOutOfRangeException(nameof(data), "IPAddress expected exactly 4 bytes.");
            }

            return new IPAddress(data);
        }

        public ushort ReadUInt16()
        {
            if (_data.Length < Index + 2)
            {
                throw new IndexOutOfRangeException("Cannot read more data.");
            }

            var result = BitConverter.ToUInt16(_data, _index);
            _index += 2;
            return result;
        }

        public ushort ReadUInt16Reverse()
        {
            if (_data.Length < Index + 2)
            {
                throw new IndexOutOfRangeException("Cannot read more data.");
            }

            byte a = _data[_index++], b = _data[_index++];
            return (ushort)(a << 8 | b);
        }

        public uint ReadUInt32Reverse()
        {
            return (uint)(ReadUInt16() << 16 | ReadUInt16());
        }

        public DnsName ReadName()
        {
            return DnsName.FromBytes(_data, ref _index);
        }

        public byte[] ReadBytes(int length)
        {
            if (_data.Length < _index + length)
            {
                throw new IndexOutOfRangeException($"Cannot read that many bytes: '{length}'.");
            }

            var result = new byte[length];
            Array.Copy(_data, _index, result, 0, length);
            _index += length;
            return result;
        }
    }
}