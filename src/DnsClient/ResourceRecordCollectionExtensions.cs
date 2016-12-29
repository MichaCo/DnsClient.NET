using System;
using System.Collections.Generic;
using DnsClient.Protocol;

namespace System.Linq
{
    public static class RecordCollectionExtension
    {
        public static IEnumerable<AaaaRecord> AaaaRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<AaaaRecord>();
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

        [CLSCompliant(false)]
        public static IEnumerable<SoaRecord> SoaRecords(this IEnumerable<DnsResourceRecord> records)
        {
            return records.OfType<SoaRecord>();
        }

        [CLSCompliant(false)]
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