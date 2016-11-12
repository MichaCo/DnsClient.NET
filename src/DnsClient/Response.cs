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

        /// <summary>
        /// List of RecordMX in Response.Answers
        /// </summary>
        public RecordMX[] RecordsMX
        {
            get
            {
                var list = Answers.OfType<RecordMX>().ToList();
                //foreach (ResourceRecord answerRR in this.Answers)
                //{
                //    RecordMX record = answerRR.Record as RecordMX;
                //    if (record != null)
                //    {
                //        list.Add(record);
                //    }
                //}

                list.Sort();
                return list.ToArray();
            }
        }

        /// <summary>
        /// List of RecordTXT in Response.Answers
        /// </summary>
        public RecordTXT[] RecordsTXT
        {
            get
            {
                List<RecordTXT> list = new List<RecordTXT>();
                foreach (ResourceRecord answerRR in Answers)
                {
                    RecordTXT record = answerRR.Record as RecordTXT;
                    if (record != null)
                        list.Add(record);
                }
                return list.ToArray();
            }
        }

        /// <summary>
        /// List of RecordA in Response.Answers
        /// </summary>
        public RecordA[] RecordsA
        {
            get
            {
                List<RecordA> list = new List<RecordA>();
                foreach (ResourceRecord answerRR in Answers)
                {
                    RecordA record = answerRR.Record as RecordA;
                    if (record != null)
                        list.Add(record);
                }
                return list.ToArray();
            }
        }

        /// <summary>
        /// List of RecordPTR in Response.Answers
        /// </summary>
        public RecordPTR[] RecordsPTR
        {
            get
            {
                List<RecordPTR> list = new List<RecordPTR>();
                foreach (ResourceRecord answerRR in Answers)
                {
                    RecordPTR record = answerRR.Record as RecordPTR;
                    if (record != null)
                        list.Add(record);
                }
                return list.ToArray();
            }
        }

        /// <summary>
        /// List of RecordCNAME in Response.Answers
        /// </summary>
        public RecordCNAME[] RecordsCNAME
        {
            get
            {
                List<RecordCNAME> list = new List<RecordCNAME>();
                foreach (ResourceRecord answerRR in Answers)
                {
                    RecordCNAME record = answerRR.Record as RecordCNAME;
                    if (record != null)
                        list.Add(record);
                }
                return list.ToArray();
            }
        }

        /// <summary>
        /// List of RecordAAAA in Response.Answers
        /// </summary>
        public RecordAAAA[] RecordsAAAA
        {
            get
            {
                List<RecordAAAA> list = new List<RecordAAAA>();
                foreach (ResourceRecord answerRR in Answers)
                {
                    RecordAAAA record = answerRR.Record as RecordAAAA;
                    if (record != null)
                        list.Add(record);
                }
                return list.ToArray();
            }
        }

        /// <summary>
        /// List of RecordNS in Response.Answers
        /// </summary>
        public RecordNS[] RecordsNS
        {
            get
            {
                List<RecordNS> list = new List<RecordNS>();
                foreach (ResourceRecord answerRR in Answers)
                {
                    RecordNS record = answerRR.Record as RecordNS;
                    if (record != null)
                        list.Add(record);
                }
                return list.ToArray();
            }
        }

        /// <summary>
        /// List of RecordSOA in Response.Answers
        /// </summary>
        public RecordSOA[] RecordsSOA
        {
            get
            {
                List<RecordSOA> list = new List<RecordSOA>();
                foreach (ResourceRecord answerRR in Answers)
                {
                    RecordSOA record = answerRR.Record as RecordSOA;
                    if (record != null)
                        list.Add(record);
                }
                return list.ToArray();
            }
        }

        /// <summary>
        /// List of RecordCERT in Response.Answers
        /// </summary>
        public RecordCERT[] RecordsCERT
        {
            get
            {
                List<RecordCERT> list = new List<RecordCERT>();
                foreach (ResourceRecord answerRR in Answers)
                {
                    RecordCERT record = answerRR.Record as RecordCERT;
                    if (record != null)
                        list.Add(record);
                }
                return list.ToArray();
            }
        }

        public RecordSRV[] RecordsSRV
        {
            get
            {
                List<RecordSRV> list = new List<RecordSRV>();
                foreach (ResourceRecord answerRR in Answers)
                {
                    RecordSRV record = answerRR.Record as RecordSRV;
                    if (record != null)
                        list.Add(record);
                }
                return list.ToArray();
            }
        }

        public ResourceRecord[] ResourceRecords
        {
            get
            {
                List<ResourceRecord> list = new List<ResourceRecord>();
                foreach (ResourceRecord rr in Answers)
                {
                    list.Add(rr);
                }
                foreach (ResourceRecord rr in Authorities)
                {
                    list.Add(rr);
                }
                foreach (ResourceRecord rr in Additionals)
                {
                    list.Add(rr);
                }

                return list.ToArray();
            }
        }


    }
}
