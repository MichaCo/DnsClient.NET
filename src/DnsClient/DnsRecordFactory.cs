﻿using System;
using System.Collections.Generic;
using System.Linq;
using DnsClient.Protocol;
using DnsClient.Protocol.Options;

namespace DnsClient
{
    internal class DnsRecordFactory
    {
        private readonly DnsDatagramReader _reader;

        public DnsRecordFactory(DnsDatagramReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
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
                _reader.ReadQuestionQueryString(),                      // name
                (ResourceRecordType)_reader.ReadUInt16NetworkOrder(),   // type
                (QueryClass)_reader.ReadUInt16NetworkOrder(),           // class
                (int)_reader.ReadUInt32NetworkOrder(),                  // TTL - 32bit!!
                _reader.ReadUInt16NetworkOrder());                      // RDLength
        }

        public DnsResourceRecord GetRecord(ResourceRecordInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            var oldIndex = _reader.Index;
            DnsResourceRecord result;

            switch (info.RecordType)
            {
                case ResourceRecordType.A:
                    result = new ARecord(info, _reader.ReadIPAddress());
                    break;

                case ResourceRecordType.NS:
                    result = new NsRecord(info, _reader.ReadDnsName());
                    break;

                case ResourceRecordType.CNAME:
                    result = new CNameRecord(info, _reader.ReadDnsName());
                    break;

                case ResourceRecordType.SOA:
                    result = ResolveSoaRecord(info);
                    break;

                case ResourceRecordType.MB:
                    result = new MbRecord(info, _reader.ReadDnsName());
                    break;

                case ResourceRecordType.MG:
                    result = new MgRecord(info, _reader.ReadDnsName());
                    break;

                case ResourceRecordType.MR:
                    result = new MrRecord(info, _reader.ReadDnsName());
                    break;

                case ResourceRecordType.NULL:
                    result = new NullRecord(info, _reader.ReadBytes(info.RawDataLength).ToArray());
                    break;

                case ResourceRecordType.WKS:
                    result = ResolveWksRecord(info);
                    break;

                case ResourceRecordType.PTR:
                    result = new PtrRecord(info, _reader.ReadDnsName());
                    break;

                case ResourceRecordType.HINFO:
                    result = new HInfoRecord(info, _reader.ReadStringWithLengthPrefix(), _reader.ReadStringWithLengthPrefix());
                    break;

                case ResourceRecordType.MINFO:
                    result = new MInfoRecord(info, _reader.ReadDnsName(), _reader.ReadDnsName());
                    break;

                case ResourceRecordType.MX:
                    result = ResolveMXRecord(info);
                    break;

                case ResourceRecordType.TXT:
                    result = ResolveTXTRecord(info);
                    break;

                case ResourceRecordType.RP:
                    result = new RpRecord(info, _reader.ReadDnsName(), _reader.ReadDnsName());
                    break;

                case ResourceRecordType.AFSDB: // 18
                    result = new AfsDbRecord(info, (AfsType)_reader.ReadUInt16NetworkOrder(), _reader.ReadDnsName());
                    break;

                case ResourceRecordType.AAAA: // 28
                    result = new AaaaRecord(info, _reader.ReadIPv6Address());
                    break;

                case ResourceRecordType.SRV: // 33
                    result = ResolveSrvRecord(info);
                    break;

                case ResourceRecordType.OPT: // 41
                    result = ResolveOptRecord(info);
                    break;

                case ResourceRecordType.DS: // 43
                    result = ResolveDsRecord(info);
                    break;

                case ResourceRecordType.SSHFP: // 44
                    result = ResolveSshfpRecord(info);
                    break;

                case ResourceRecordType.RRSIG: // 46
                    result = ResolveRRSigRecord(info);
                    break;

                case ResourceRecordType.NSEC: // 47
                    result = ResolveNSecRecord(info);
                    break;

                case ResourceRecordType.DNSKEY: // 48
                    result = ResolveDnsKeyRecord(info);
                    break;

                case ResourceRecordType.TLSA: // 52
                    result = ResolveTlsaRecord(info);
                    break;

                case ResourceRecordType.URI: // 256
                    result = ResolveUriRecord(info);
                    break;

                case ResourceRecordType.CAA: // 257
                    result = ResolveCaaRecord(info);
                    break;

                default:
                    result = new UnknownRecord(info, _reader.ReadBytes(info.RawDataLength).ToArray());
                    break;
            }

            // sanity check
            _reader.SanitizeResult(oldIndex + info.RawDataLength, info.RawDataLength);

            return result;
        }

        private DnsResourceRecord ResolveSoaRecord(ResourceRecordInfo info)
        {
            var mName = _reader.ReadDnsName();
            var rName = _reader.ReadDnsName();
            var serial = _reader.ReadUInt32NetworkOrder();
            var refresh = _reader.ReadUInt32NetworkOrder();
            var retry = _reader.ReadUInt32NetworkOrder();
            var expire = _reader.ReadUInt32NetworkOrder();
            var minimum = _reader.ReadUInt32NetworkOrder();

            return new SoaRecord(info, mName, rName, serial, refresh, retry, expire, minimum);
        }

        private DnsResourceRecord ResolveWksRecord(ResourceRecordInfo info)
        {
            var address = _reader.ReadIPAddress();
            var protocol = _reader.ReadByte();
            var bitmap = _reader.ReadBytes(info.RawDataLength - 5);

            return new WksRecord(info, address, protocol, bitmap.ToArray());
        }

        private DnsResourceRecord ResolveMXRecord(ResourceRecordInfo info)
        {
            var preference = _reader.ReadUInt16NetworkOrder();
            var domain = _reader.ReadDnsName();

            return new MxRecord(info, preference, domain);
        }

        private DnsResourceRecord ResolveTXTRecord(ResourceRecordInfo info)
        {
            int pos = _reader.Index;

            var values = new List<string>();
            var utf8Values = new List<string>();
            while ((_reader.Index - pos) < info.RawDataLength)
            {
                var length = _reader.ReadByte();
                var bytes = _reader.ReadBytes(length);
                var escaped = DnsDatagramReader.ParseString(bytes);
                var utf = DnsDatagramReader.ReadUTF8String(bytes);
                values.Add(escaped);
                utf8Values.Add(utf);
            }

            return new TxtRecord(info, values.ToArray(), utf8Values.ToArray());
        }

        private DnsResourceRecord ResolveSrvRecord(ResourceRecordInfo info)
        {
            var priority = _reader.ReadUInt16NetworkOrder();
            var weight = _reader.ReadUInt16NetworkOrder();
            var port = _reader.ReadUInt16NetworkOrder();
            var target = _reader.ReadDnsName();

            return new SrvRecord(info, priority, weight, port, target);
        }

        private DnsResourceRecord ResolveOptRecord(ResourceRecordInfo info)
        {
            // Consume bytes in case the OPT record has any.
            var bytes = _reader.ReadBytes(info.RawDataLength).ToArray();
            return new OptRecord((int)info.RecordClass, ttlFlag: info.InitialTimeToLive, length: info.RawDataLength, data: bytes);
        }

        private DnsResourceRecord ResolveDsRecord(ResourceRecordInfo info)
        {
            var startIndex = _reader.Index;
            var keyTag = _reader.ReadUInt16NetworkOrder();
            var algorithm = _reader.ReadByte();
            var digestType = _reader.ReadByte();
            var digest = _reader.ReadBytesToEnd(startIndex, info.RawDataLength).ToArray();
            return new DsRecord(info, keyTag, algorithm, digestType, digest);
        }

        private DnsResourceRecord ResolveSshfpRecord(ResourceRecordInfo info)
        {
            var algorithm = (SshfpAlgorithm)_reader.ReadByte();
            var fingerprintType = (SshfpFingerprintType)_reader.ReadByte();
            var fingerprint = _reader.ReadBytes(info.RawDataLength - 2).ToArray();
            var fingerprintHexString = string.Join(string.Empty, fingerprint.Select(b => b.ToString("X2")));
            return new SshfpRecord(info, algorithm, fingerprintType, fingerprintHexString);
        }

        private DnsResourceRecord ResolveRRSigRecord(ResourceRecordInfo info)
        {
            var startIndex = _reader.Index;
            var type = _reader.ReadUInt16NetworkOrder();
            var algorithmNumber = _reader.ReadByte();
            var labels = _reader.ReadByte();
            var originalTtl = _reader.ReadUInt32NetworkOrder();
            var signatureExpiration = _reader.ReadUInt32NetworkOrder();
            var signatureInception = _reader.ReadUInt32NetworkOrder();
            var keyTag = _reader.ReadUInt16NetworkOrder();
            var signersName = _reader.ReadDnsName();
            var signature = _reader.ReadBytesToEnd(startIndex, info.RawDataLength).ToArray();
            return new RRSigRecord(info, type, algorithmNumber, labels, originalTtl, signatureExpiration, signatureInception, keyTag, signersName, signature);
        }

        private DnsResourceRecord ResolveNSecRecord(ResourceRecordInfo info)
        {
            var startIndex = _reader.Index;
            var nextName = _reader.ReadDnsName();
            var bitMaps = _reader.ReadBytesToEnd(startIndex, info.RawDataLength).ToArray();
            return new NSecRecord(info, nextName, bitMaps);
        }

        private DnsResourceRecord ResolveDnsKeyRecord(ResourceRecordInfo info)
        {
            var startIndex = _reader.Index;
            int flags = _reader.ReadUInt16NetworkOrder();
            var protocol = _reader.ReadByte();
            var algorithm = _reader.ReadByte();
            var publicKey = _reader.ReadBytesToEnd(startIndex, info.RawDataLength).ToArray();
            return new DnsKeyRecord(info, flags, protocol, algorithm, publicKey);
        }

        private DnsResourceRecord ResolveTlsaRecord(ResourceRecordInfo info)
        {
            var startIndex = _reader.Index;
            var certificateUsage = _reader.ReadByte();
            var selector = _reader.ReadByte();
            var matchingType = _reader.ReadByte();
            var certificateAssociationData = _reader.ReadBytesToEnd(startIndex, info.RawDataLength).ToArray();
            return new TlsaRecord(info, certificateUsage, selector, matchingType, certificateAssociationData);
        }

        private DnsResourceRecord ResolveUriRecord(ResourceRecordInfo info)
        {
            var prio = _reader.ReadUInt16NetworkOrder();
            var weight = _reader.ReadUInt16NetworkOrder();
            var target = _reader.ReadString(info.RawDataLength - 4);
            return new UriRecord(info, prio, weight, target);
        }

        private DnsResourceRecord ResolveCaaRecord(ResourceRecordInfo info)
        {
            var flag = _reader.ReadByte();
            var tag = _reader.ReadStringWithLengthPrefix();
            var stringValue = DnsDatagramReader.ParseString(_reader, info.RawDataLength - 2 - tag.Length);
            return new CaaRecord(info, flag, tag, stringValue);
        }
    }
}