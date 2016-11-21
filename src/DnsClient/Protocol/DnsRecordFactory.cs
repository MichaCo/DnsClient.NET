using System;
using System.Collections.Generic;
using DnsClient.Protocol.Record;

namespace DnsClient.Protocol
{
    public class DnsRecordFactory
    {
        public static IDictionary<int, Func<DnsDatagramReader, ResourceRecordInfo, DnsResourceRecord>> s_recordFactory =
               new Dictionary<int, Func<DnsDatagramReader, ResourceRecordInfo, DnsResourceRecord>>();

        private readonly DnsDatagramReader _reader;

        public DnsRecordFactory(DnsDatagramReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            _reader = reader;
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
        public ResourceRecordInfo ReadRecordInfo()
        {
            return new ResourceRecordInfo(
                _reader.ReadName().ToString(),                   // name
                _reader.ReadUInt16Reverse(),                     // type
                _reader.ReadUInt16Reverse(),                     // class
                _reader.ReadUInt32Reverse(),                     // ttl - 32bit!!
                _reader.ReadUInt16Reverse());                    // RDLength
                                                                //reader.ReadBytes(reader.ReadUInt16Reverse()));  // rdata
        }

        public DnsResourceRecord GetRecord(ResourceRecordInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            var oldIndex = _reader.Index;
            DnsResourceRecord result;

            if (s_recordFactory.ContainsKey(info.RecordType))
            {
                result = s_recordFactory[info.RecordType](_reader, info);
            }
            else
            {
                switch (info.RecordType)
                {
                    case 1:
                        result = ResolveARecord(info);
                        break;

                    case 2:
                        result = ResolveNsRecord(info);
                        break;

                    case 6:
                        result = ResolveSoaRecord(info);
                        break;

                    case 12:
                        result = ResolvePtrRecord(info);
                        break;

                    case 15:
                        result = ResolveMXRecord(info);
                        break;

                    case 16:
                        result = ResolveTXTRecord(info);
                        break;

                    case 28:
                        result = ResolveAAAARecord(info);
                        break;

                    case 33:
                        result = ResolveSrvRecord(info);
                        break;

                    default:
                        // update reader index because we don't read full data for the empty record
                        _reader.Index += info.RawDataLength;
                        result = new EmptyRecord(info);
                        break;
                }
            }

            // sanity check
            if (_reader.Index != oldIndex + info.RawDataLength)
            {
                throw new InvalidOperationException("Record reader index out of sync.");
            }

            return result;
        }

        private PtrRecord ResolvePtrRecord(ResourceRecordInfo info)
        {
            return new PtrRecord(info, _reader.ReadName().ToString());
        }

        private AAAARecord ResolveAAAARecord(ResourceRecordInfo info)
        {
            var address = _reader.ReadIPv6Address();
            return new AAAARecord(info, address);
        }

        // default resolver implementation for an A Record
        private ARecord ResolveARecord(ResourceRecordInfo info)
        {
            if (info.RawDataLength != 4)
            {
                throw new IndexOutOfRangeException($"Reading wrong length for an IP address. Expected 4 found {info.RawDataLength}.");
            }

            return new ARecord(info, _reader.ReadIPAddress());
        }

        private MxRecord ResolveMXRecord(ResourceRecordInfo info)
        {
            var preference = _reader.ReadUInt16Reverse();
            var domain = _reader.ReadName();

            return new MxRecord(info, preference, domain.ToString());
        }

        private NsRecord ResolveNsRecord(ResourceRecordInfo info)
        {
            var name = _reader.ReadName();
            return new NsRecord(info, name.ToString());
        }

        private SoaRecord ResolveSoaRecord(ResourceRecordInfo info)
        {
            var mName = _reader.ReadName();
            var rName = _reader.ReadName();
            var serial = _reader.ReadUInt32Reverse();
            var refresh = _reader.ReadUInt32Reverse();
            var retry = _reader.ReadUInt32Reverse();
            var expire = _reader.ReadUInt32Reverse();
            var minimum = _reader.ReadUInt32Reverse();

            return new SoaRecord(info, mName.ToString(), rName.ToString(), serial, refresh, retry, expire, minimum);
        }

        private SrvRecord ResolveSrvRecord(ResourceRecordInfo info)
        {
            var priority = _reader.ReadUInt16Reverse();
            var weight = _reader.ReadUInt16Reverse();
            var port = _reader.ReadUInt16Reverse();
            var target = _reader.ReadName();

            return new SrvRecord(info, priority, weight, port, target.ToString());
        }

        private TxtRecord ResolveTXTRecord(ResourceRecordInfo info)
        {
            int pos = _reader.Index;

            var values = new List<string>();
            while ((_reader.Index - pos) < info.RawDataLength)
            {
                values.Add(_reader.ReadString());
            }

            return new TxtRecord(info, values.ToArray());
        }
    }
}