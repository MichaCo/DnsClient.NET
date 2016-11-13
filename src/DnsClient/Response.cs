using System;
using System.IO;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using DnsClient.Records;

namespace DnsClient
{
    public class Response
    {
        /// <summary>
        /// List of Question records
        /// </summary>
        public IReadOnlyCollection<Question> Questions { get; } = new Question[] { };

        /// <summary>
        /// List of AnswerRR records
        /// </summary>
        public IReadOnlyCollection<ResourceRecord> Answers { get; private set; } = new ResourceRecord[] { };

        /// <summary>
        /// List of AuthorityRR records
        /// </summary>
        public IReadOnlyCollection<ResourceRecord> Authorities { get; private set; } = new ResourceRecord[] { };

        /// <summary>
        /// List of AdditionalRR records
        /// </summary>
        public IReadOnlyCollection<ResourceRecord> Additionals { get; private set; } = new ResourceRecord[] { };

        public Header Header { get; }

        /// <summary>
        /// Error message, empty when no error
        /// </summary>
        public string Error { get; } = "";

        /// <summary>
        /// The Size of the message
        /// </summary>
        public int MessageSize { get; internal set; }

        /// <summary>
        /// TimeStamp when cached
        /// </summary>
        public DateTime TimeStamp { get; } = DateTime.Now;

        /// <summary>
        /// Server which delivered this response
        /// </summary>
        public IPEndPoint Server { get; } = new IPEndPoint(0, 0);

        public Response()
        {
        }

        public Response(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                throw new ArgumentNullException(nameof(error));
            }

            Error = error;
        }

        public Response(Response fromResponse)
        {
            Server = fromResponse.Server;
            TimeStamp = fromResponse.TimeStamp;
            MessageSize = fromResponse.MessageSize;
            Questions = fromResponse.Questions;
            Answers = fromResponse.Answers;
            Authorities = fromResponse.Authorities;
            Additionals = fromResponse.Additionals;
        }

        public static Response Concat(Response responseA, Response responseB)
        {
            var result = new Response(responseA);
            result.Answers = responseA.Answers.Concat(responseB.Answers).ToArray();
            result.Authorities = responseA.Authorities.Concat(responseB.Authorities).ToArray();
            result.Additionals = responseA.Additionals.Concat(responseB.Additionals).ToArray();
            return result;
        }

        public Response(IPEndPoint iPEndPoint, byte[] data)
        {
            if (iPEndPoint == null)
            {
                throw new ArgumentNullException(nameof(iPEndPoint));
            }
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            RecordReader recordReader = new RecordReader(data);
            Server = iPEndPoint;
            TimeStamp = DateTime.Now;
            MessageSize = data.Length;

            Header = new Header(recordReader);
            var questions = new List<Question>();
            var answers = new List<ResourceRecord>();
            var authorities = new List<ResourceRecord>();
            var additionals = new List<ResourceRecord>();

            for (int intI = 0; intI < Header.QuestionCount; intI++)
            {
                questions.Add(new Question(recordReader));
            }
            Questions = questions.ToArray();

            for (int intI = 0; intI < Header.AnswerCount; intI++)
            {
                answers.Add(new ResourceRecord(recordReader));
            }
            Answers = answers.ToArray();

            for (int intI = 0; intI < Header.NameServerCount; intI++)
            {
                authorities.Add(new ResourceRecord(recordReader));
            }
            Authorities = authorities.ToArray();

            for (int intI = 0; intI < Header.AdditionalCount; intI++)
            {
                additionals.Add(new ResourceRecord(recordReader));
            }
            Additionals = additionals.ToArray();
        }

        public IReadOnlyCollection<TRecord> GetRecords<TRecord>() where TRecord : Record
        {
            return Answers.Select(p => p.Record).OfType<TRecord>().ToArray();
        }

        /// <summary>
        /// List of RecordMX in Response.Answers
        /// </summary>
        public IReadOnlyCollection<RecordMX> RecordsMX
        {
            get
            {
                var list = GetRecords<RecordMX>().ToList();
                list.Sort();
                return list.ToArray();
            }
        }

        /// <summary>
        /// List of RecordTXT in Response.Answers
        /// </summary>
        public IReadOnlyCollection<RecordTXT> RecordsTXT
        {
            get
            {
                return GetRecords<RecordTXT>();
            }
        }

        /// <summary>
        /// List of RecordA in Response.Answers
        /// </summary>
        public IReadOnlyCollection<RecordA> RecordsA
        {
            get
            {
                return GetRecords<RecordA>();
            }
        }

        /// <summary>
        /// List of RecordPTR in Response.Answers
        /// </summary>
        public IReadOnlyCollection<RecordPTR> RecordsPTR
        {
            get
            {
                return GetRecords<RecordPTR>();
            }
        }

        /// <summary>
        /// List of RecordCNAME in Response.Answers
        /// </summary>
        public IReadOnlyCollection<RecordCNAME> RecordsCNAME
        {
            get
            {
                return GetRecords<RecordCNAME>();
            }
        }

        /// <summary>
        /// List of RecordAAAA in Response.Answers
        /// </summary>
        public IReadOnlyCollection<RecordAAAA> RecordsAAAA
        {
            get
            {
                return GetRecords<RecordAAAA>();
            }
        }

        /// <summary>
        /// List of RecordNS in Response.Answers
        /// </summary>
        public IReadOnlyCollection<RecordNS> RecordsNS
        {
            get
            {
                return GetRecords<RecordNS>();
            }
        }

        /// <summary>
        /// List of RecordSOA in Response.Answers
        /// </summary>
        public IReadOnlyCollection<RecordSOA> RecordsSOA
        {
            get
            {
                return GetRecords<RecordSOA>();
            }
        }

        /// <summary>
        /// List of RecordCERT in Response.Answers
        /// </summary>
        public IReadOnlyCollection<RecordCERT> RecordsCERT
        {
            get
            {
                return GetRecords<RecordCERT>();
            }
        }

        public IReadOnlyCollection<RecordSRV> RecordsSRV
        {
            get
            {
                return GetRecords<RecordSRV>();
            }
        }

        public IEnumerable<ResourceRecord> ResourceRecords
        {
            get
            {
                foreach (ResourceRecord resourceRecord in Answers)
                {
                    yield return resourceRecord;
                }
                foreach (ResourceRecord resourceRecord in Authorities)
                {
                    yield return resourceRecord;
                }
                foreach (ResourceRecord resourceRecord in Additionals)
                {
                    yield return resourceRecord;
                }
            }
        }
    }
}
