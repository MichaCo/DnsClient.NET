using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using DnsClient.Protocol;
using Microsoft.Extensions.Logging;

namespace DnsClient
{
    public class Response
    {
        /// <summary>
        /// Gets a list of Question records.
        /// </summary>
        public IReadOnlyCollection<Question> Questions { get; } = new Question[] { };

        /// <summary>
        /// Gets a list of Answer records.
        /// </summary>
        public IReadOnlyCollection<ResourceRecord> Answers { get; private set; } = new ResourceRecord[] { };

        /// <summary>
        /// Gets a list of Authority records.
        /// </summary>
        public IReadOnlyCollection<ResourceRecord> Authorities { get; private set; } = new ResourceRecord[] { };

        /// <summary>
        /// Gets a list of Additional records.
        /// </summary>
        public IReadOnlyCollection<ResourceRecord> Additionals { get; private set; } = new ResourceRecord[] { };

        /// <summary>
        /// Gets the header field.
        /// </summary>
        public Header Header { get; }

        /// <summary>
        /// Gets an error message, empty when successful.
        /// </summary>
        public string Error { get; } = "";

        /// <summary>
        /// Gets the size of the message.
        /// </summary>
        public int MessageSize { get; internal set; }

        /// <summary>
        /// Gets the time stamp used for caching (if enabled).
        /// </summary>
        public DateTime TimeStamp { get; } = DateTime.Now;

        /// <summary>
        /// Gets the server endpoint which delivered this response.
        /// </summary>
        public IPEndPoint Server { get; } = new IPEndPoint(0, 0);

        /// <summary>
        /// Gets a flag indicating if the response was successful.
        /// </summary>
        public bool Success => !string.IsNullOrWhiteSpace(Error);

        /// <summary>
        /// Gets a list of all <see cref="RecordMX"/> records in this response's answers.
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
        /// Gets a list of all <see cref="RecordTXT"/> records in this response's answers.
        /// </summary>
        public IReadOnlyCollection<RecordTXT> RecordsTXT => GetRecords<RecordTXT>();

        /// <summary>
        /// Gets a list of all <see cref="RecordA"/> records in this response's answers.
        /// </summary>
        public IReadOnlyCollection<RecordA> RecordsA => GetRecords<RecordA>();

        /// <summary>
        /// Gets a list of all <see cref="RecordPTR"/> records in this response's answers.
        /// </summary>
        public IReadOnlyCollection<RecordPTR> RecordsPTR => GetRecords<RecordPTR>();

        /// <summary>
        /// Gets a list of all <see cref="RecordCNAME"/> records in this response's answers.
        /// </summary>
        public IReadOnlyCollection<RecordCNAME> RecordsCNAME => GetRecords<RecordCNAME>();

        /// <summary>
        /// Gets a list of all <see cref="RecordAAAA"/> records in this response's answers.
        /// </summary>
        public IReadOnlyCollection<RecordAAAA> RecordsAAAA => GetRecords<RecordAAAA>();

        /// <summary>
        /// Gets a list of all <see cref="RecordNS"/> records in this response's answers.
        /// </summary>
        public IReadOnlyCollection<RecordNS> RecordsNS => GetRecords<RecordNS>();

        /// <summary>
        /// Gets a list of all <see cref="RecordSOA"/> records in this response's answers.
        /// </summary>
        public IReadOnlyCollection<RecordSOA> RecordsSOA => GetRecords<RecordSOA>();

        /// <summary>
        /// Gets a list of all <see cref="RecordCERT"/> records in this response's answers.
        /// </summary>
        public IReadOnlyCollection<RecordCERT> RecordsCERT => GetRecords<RecordCERT>();

        /// <summary>
        /// Gets a list of all <see cref="RecordSRV"/> records in this response's answers.
        /// </summary>
        public IReadOnlyCollection<RecordSRV> RecordsSRV => GetRecords<RecordSRV>();

        /// <summary>
        /// Gets a list of all <see cref="ResourceRecord"/>s of this response result.
        /// </summary>
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

        internal Response()
        {
        }

        internal Response(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                throw new ArgumentNullException(nameof(error));
            }

            Error = error;
        }

        internal Response(Response fromResponse)
        {
            Server = fromResponse.Server;
            TimeStamp = fromResponse.TimeStamp;
            MessageSize = fromResponse.MessageSize;
            Questions = fromResponse.Questions;
            Answers = fromResponse.Answers;
            Authorities = fromResponse.Authorities;
            Additionals = fromResponse.Additionals;
        }

        internal static Response Concat(Response responseA, Response responseB)
        {
            var result = new Response(responseA);
            result.Answers = responseA.Answers.Concat(responseB.Answers).ToArray();
            result.Authorities = responseA.Authorities.Concat(responseB.Authorities).ToArray();
            result.Additionals = responseA.Additionals.Concat(responseB.Additionals).ToArray();
            return result;
        }

        internal Response(ILoggerFactory loggerFactory, IPEndPoint iPEndPoint, byte[] data)
        {
            if (iPEndPoint == null)
            {
                throw new ArgumentNullException(nameof(iPEndPoint));
            }
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            RecordReader recordReader = new RecordReader(loggerFactory, data);
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

        private IReadOnlyCollection<TRecord> GetRecords<TRecord>() where TRecord : Record
        {
            return Answers.Select(p => p.Record).OfType<TRecord>().ToArray();
        }
    }
}