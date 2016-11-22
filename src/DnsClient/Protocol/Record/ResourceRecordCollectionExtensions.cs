using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using DnsClient.Protocol;
using DnsClient.Protocol.Record;

namespace DnsClient
{
    public static class RecordCollectionExtension
    {
        public static IEnumerable<AAAARecord> AaaaRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<AAAARecord>();
        }

        public static IEnumerable<ARecord> ARecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<ARecord>();
        }

        public static IEnumerable<CaaRecord> CaaRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<CaaRecord>();
        }

        public static IEnumerable<NsRecord> NsRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<NsRecord>();
        }

        public static IEnumerable<DnsResourceRecord> OfRecordType(this IEnumerable<DnsResourceRecord> records, ResourceRecordType type)
        {
            return records.Where(p => p.RecordType == type);
        }

        public static IEnumerable<PtrRecord> PtrRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<PtrRecord>();
        }

        public static IEnumerable<SoaRecord> SoaRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<SoaRecord>();
        }

        public static IEnumerable<SrvRecord> SrvRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<SrvRecord>();
        }

        public static IEnumerable<TxtRecord> TxtRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<TxtRecord>();
        }
    }
}