using System.Collections.Generic;
using DnsClient.Protocol;

namespace DnsClient
{
    public interface IDnsQueryResponse
    {
        ICollection<DnsQuestion> Questions { get; }

        ICollection<DnsResourceRecord> Additionals { get; }

        IEnumerable<DnsResourceRecord> AllRecords { get; }

        ICollection<DnsResourceRecord> Answers { get; }

        ICollection<DnsResourceRecord> Authorities { get; }

        string AuditTrail { get; }

        string ErrorMessage { get; }

        bool HasError { get; }

        DnsResponseHeader Header { get; }

        int MessageSize { get; }

        NameServer NameServer { get; }
    }
}