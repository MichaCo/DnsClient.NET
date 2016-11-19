using System;
using System.Collections.Generic;
using System.Text;
using DnsClient.Protocol;
using Microsoft.Extensions.Logging;

namespace DnsClient
{
    internal class RecordReader
    {
        public byte[] Data { get; }
        private readonly ILogger<RecordReader> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private int _position;

        internal RecordReader(ILoggerFactory loggerFactory, byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (loggerFactory != null)
            {
                _loggerFactory = loggerFactory;
                _logger = loggerFactory.CreateLogger<RecordReader>();
            }

            Data = data;
            _position = 0;
        }

        internal RecordReader(ILoggerFactory loggerFactory, byte[] data, int Position)
            : this(loggerFactory, data)
        {
            _position = Position;
        }

        public int Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
            }
        }

        public byte ReadByte()
        {
            if (_position >= Data.Length)
            {
                return 0;
            }
            else
            {
                return Data[_position++];
            }
        }

        public char ReadChar()
        {
            return (char)ReadByte();
        }

        public ushort ReadUInt16()
        {
            return (ushort)(ReadByte() << 8 | ReadByte());
        }

        public ushort ReadUInt16(int offset)
        {
            _position += offset;
            return ReadUInt16();
        }

        public uint ReadUInt32()
        {
            return (uint)(ReadUInt16() << 16 | ReadUInt16());
        }

        private bool IsLogging => _logger != null;

        public string ReadDomainName()
        {
            StringBuilder name = new StringBuilder();
            int length = 0;

            // get  the length of the first label
            while ((length = ReadByte()) != 0)
            {
                // top 2 bits set denotes domain name compression and to reference elsewhere
                if ((length & 0xc0) == 0xc0)
                {
                    // work out the existing domain name, copy this pointer
                    RecordReader newRecordReader = new RecordReader(_loggerFactory, Data, (length & 0x3f) << 8 | ReadByte());

                    name.Append(newRecordReader.ReadDomainName());
                    return name.ToString();
                }

                // if not using compression, copy a char at a time to the domain name
                while (length > 0)
                {
                    name.Append(ReadChar());
                    length--;
                }
                name.Append('.');
            }
            if (name.Length == 0)
            {
                return ".";
            }
            else
            {
                return name.ToString();
            }
        }

        public string ReadString()
        {
            short length = ReadByte();

            StringBuilder name = new StringBuilder();
            for (int intI = 0; intI < length; intI++)
            {
                name.Append(ReadChar());
            }

            return name.ToString();
        }

        public byte[] ReadBytes(int intLength)
        {
            List<byte> list = new List<byte>();
            for (int intI = 0; intI < intLength; intI++)
            {
                list.Add(ReadByte());
            }

            return list.ToArray();
        }

        public Record ReadRecord(ResourceRecord resource, TypeValue type, int length)
        {
            if (IsLogging)
            {
                _logger.LogTrace("Reading record of type '{0}'.", type);
            }

            switch (type)
            {
                case TypeValue.A:
                    return new RecordA(resource, this);
                case TypeValue.NS:
                    return new RecordNS(resource, this);
                case TypeValue.CNAME:
                    return new RecordCNAME(resource, this);
                case TypeValue.SOA:
                    return new RecordSOA(resource, this);
                case TypeValue.MB:
                    return new RecordMB(resource, this);
                case TypeValue.MG:
                    return new RecordMG(resource, this);
                case TypeValue.MR:
                    return new RecordMR(resource, this);
                case TypeValue.NULL:
                    return new RecordNULL(resource, this);
                case TypeValue.WKS:
                    return new RecordWKS(resource, this);
                case TypeValue.PTR:
                    return new RecordPTR(resource, this);
                case TypeValue.HINFO:
                    return new RecordHINFO(resource, this);
                case TypeValue.MINFO:
                    return new RecordMINFO(resource, this);
                case TypeValue.MX:
                    return new RecordMX(resource, this);
                case TypeValue.TXT:
                    return new RecordTXT(resource, this, length);
                case TypeValue.RP:
                    return new RecordRP(resource, this);
                case TypeValue.AFSDB:
                    return new RecordAFSDB(resource, this);
                case TypeValue.X25:
                    return new RecordX25(resource, this);
                case TypeValue.ISDN:
                    return new RecordISDN(resource, this);
                case TypeValue.RT:
                    return new RecordRT(resource, this);
                case TypeValue.NSAP:
                    return new RecordNSAP(resource, this);
                case TypeValue.SIG:
                    return new RecordSIG(resource, this);
                case TypeValue.KEY:
                    return new RecordKEY(resource, this);
                case TypeValue.PX:
                    return new RecordPX(resource, this);
                case TypeValue.AAAA:
                    return new RecordAAAA(resource, this);
                case TypeValue.LOC:
                    return new RecordLOC(resource, this);
                case TypeValue.EID:
                    return new RecordEID(resource, this);
                case TypeValue.NIMLOC:
                    return new RecordNIMLOC(resource, this);
                case TypeValue.SRV:
                    return new RecordSRV(resource, this);
                case TypeValue.ATMA:
                    return new RecordATMA(resource, this);
                case TypeValue.NAPTR:
                    return new RecordNAPTR(resource, this);
                case TypeValue.KX:
                    return new RecordKX(resource, this);
                case TypeValue.CERT:
                    return new RecordCERT(resource, this);
                case TypeValue.A6:
                    return new RecordA6(resource, this);
                case TypeValue.DNAME:
                    return new RecordDNAME(resource, this);
                case TypeValue.SINK:
                    return new RecordSINK(resource, this);
                case TypeValue.OPT:
                    return new RecordOPT(resource, this);
                case TypeValue.APL:
                    return new RecordAPL(resource, this);
                case TypeValue.DS:
                    return new RecordDS(resource, this);
                case TypeValue.SSHFP:
                    return new RecordSSHFP(resource, this);
                case TypeValue.IPSECKEY:
                    return new RecordIPSECKEY(resource, this);
                case TypeValue.RRSIG:
                    return new RecordRRSIG(resource, this);
                case TypeValue.NSEC:
                    return new RecordNSEC(resource, this);
                case TypeValue.DNSKEY:
                    return new RecordDNSKEY(resource, this);
                case TypeValue.DHCID:
                    return new RecordDHCID(resource, this);
                case TypeValue.NSEC3:
                    return new RecordNSEC3(resource, this);
                case TypeValue.NSEC3PARAM:
                    return new RecordNSEC3PARAM(resource, this);
                case TypeValue.HIP:
                    return new RecordHIP(resource, this);
                case TypeValue.SPF:
                    return new RecordSPF(resource, this);
                case TypeValue.UINFO:
                    return new RecordUINFO(resource, this);
                case TypeValue.UID:
                    return new RecordUID(resource, this);
                case TypeValue.GID:
                    return new RecordGID(resource, this);
                case TypeValue.UNSPEC:
                    return new RecordUNSPEC(resource, this);
                case TypeValue.TKEY:
                    return new RecordTKEY(resource, this);
                case TypeValue.TSIG:
                    return new RecordTSIG(resource, this);
                //case TypeValue.MD:
                //    return new RecordMD(resource, this);
                //case TypeValue.MF:
                //    return new RecordMF(resource, this);
                //case TypeValue.GPOS:
                //    return new RecordGPOS(resource, this);
                //case TypeValue.NSAPPTR:
                //    return new RecordNSAPPTR(resource, this);
                //case TypeValue.NXT:
                //    return new RecordNXT(resource, this);
                case TypeValue.MD:
                case TypeValue.MF:
                case TypeValue.GPOS:
                case TypeValue.NSAPPTR:
                case TypeValue.NXT:
                    if (IsLogging)
                    {
                        _logger.LogWarning("Received obsolete record with type '{0}'.", type);
                    }

                    return new RecordUnknown(resource, this);
                default:
                    if (IsLogging)
                    {
                        _logger.LogWarning("Received unknown record with type '{0}'.", type);
                    }

                    return new RecordUnknown(resource, this);
            }
        }
    }
}