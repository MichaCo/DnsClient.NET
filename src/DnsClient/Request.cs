using System;
using System.Collections.Generic;

namespace DnsClient
{
    internal class Request
    {
        public Request(Header header, params Question[] questions)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }
            if (questions == null || questions.Length == 0)
            {
                throw new ArgumentException("At least one question must be specified for the request.", nameof(questions));
            }
            if (header.QuestionCount != questions.Length)
            {
                throw new InvalidOperationException("Header question count and number of questions do not match.");
            }

            Header = header;
            Questions = questions;
        }

        public Question[] Questions { get; }

        public Header Header { get; }

        public byte[] Data
        {
            get
            {
                List<byte> data = new List<byte>();
                data.AddRange(Header.Data);
                foreach (Question q in Questions)
                {
                    data.AddRange(q.Data);
                }

                return data.ToArray();
            }
        }
    }
}
