﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using DnsClient.Protocol;
using Xunit;

namespace DnsClient.Tests
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class DnsRecordFactoryTest
    {
        static DnsRecordFactoryTest()
        {
            Tracing.Source.Switch.Level = System.Diagnostics.SourceLevels.All;
        }

        internal DnsRecordFactory GetFactory(byte[] data)
        {
            return new DnsRecordFactory(new DnsDatagramReader(new ArraySegment<byte>(data)));
        }

        [Fact]
        public void DnsRecordFactory_PTRRecordNotEnoughData()
        {
            var data = new byte[0];
            var factory = GetFactory(data);
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.PTR, QueryClass.IN, 0, data.Length);

            Action act = () => factory.GetRecord(info);
            var ex = Assert.ThrowsAny<DnsResponseParseException>(act);
            Assert.Equal(0, ex.Index);
        }

        [Fact]
        public void DnsRecordFactory_PTRRecordEmptyName()
        {
            var data = new byte[] { 0 };
            var factory = GetFactory(data);
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.PTR, QueryClass.IN, 0, data.Length);

            var result = factory.GetRecord(info) as PtrRecord;

            Assert.Equal(".", result.PtrDomainName.Value);
        }

        [Fact]
        public void DnsRecordFactory_PTRRecord()
        {
            var name = DnsString.Parse("result.example.com");
            var writer = new DnsDatagramWriter();
            writer.WriteHostName(name.Value);
            var factory = GetFactory(writer.Data.ToArray());
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.PTR, QueryClass.IN, 0, writer.Data.Count);

            var result = factory.GetRecord(info) as PtrRecord;

            Assert.Equal(result.PtrDomainName, name);
        }

        [Fact]
        public void DnsRecordFactory_MBRecord()
        {
            var name = DnsString.Parse("Müsli.de");
            var writer = new DnsDatagramWriter();
            writer.WriteHostName(name.Value);
            var factory = GetFactory(writer.Data.ToArray());
            var info = new ResourceRecordInfo("Müsli.de", ResourceRecordType.MB, QueryClass.IN, 0, writer.Data.Count);

            var result = factory.GetRecord(info) as MbRecord;

            Assert.Equal(result.MadName, name);
            Assert.Equal("müsli.de.", result.MadName.Original);
        }

        [Fact]
        public void DnsRecordFactory_ARecordNotEnoughData()
        {
            var data = new byte[] { 23, 23, 23 };
            var factory = GetFactory(data);
            var info = new ResourceRecordInfo("example.com", ResourceRecordType.A, QueryClass.IN, 0, data.Length);

            var ex = Assert.ThrowsAny<DnsResponseParseException>(() => factory.GetRecord(info));
            Assert.Contains("IPv4", ex.Message);
            Assert.Equal(0, ex.Index);
            Assert.Equal(4, ex.ReadLength);
        }

        [Fact]
        public void DnsRecordFactory_ARecord()
        {
            var data = new byte[] { 23, 24, 25, 26 };
            var factory = GetFactory(data);
            var info = new ResourceRecordInfo("example.com", ResourceRecordType.A, QueryClass.IN, 0, data.Length);

            var result = factory.GetRecord(info) as ARecord;

            Assert.Equal(result.Address, IPAddress.Parse("23.24.25.26"));
        }

        [Fact]
        public void DnsRecordFactory_AAAARecordNotEnoughData()
        {
            var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
            var factory = GetFactory(data);
            var info = new ResourceRecordInfo("example.com", ResourceRecordType.AAAA, QueryClass.IN, 0, data.Length);

            var ex = Assert.ThrowsAny<DnsResponseParseException>(() => factory.GetRecord(info));
            Assert.Contains("IPv6", ex.Message);
            Assert.Equal(0, ex.Index);
            Assert.Equal(16, ex.ReadLength);
        }

        [Fact]
        public void DnsRecordFactory_AAAARecord()
        {
            var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            var factory = GetFactory(data);
            var info = new ResourceRecordInfo("example.com", ResourceRecordType.AAAA, QueryClass.IN, 0, data.Length);

            var result = factory.GetRecord(info) as AaaaRecord;

            Assert.Equal(result.Address, IPAddress.Parse("102:304:506:708:90a:b0c:d0e:f10"));
            Assert.Equal(result.Address.GetAddressBytes(), data);
        }

        [Fact]
        public void DnsRecordFactory_NSRecordNotEnoughData()
        {
            var data = new byte[0];
            var factory = GetFactory(data);
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.NS, QueryClass.IN, 0, data.Length);

            var ex = Assert.ThrowsAny<DnsResponseParseException>(() => factory.GetRecord(info));
            Assert.Equal(0, ex.Index);
        }

        [Fact]
        public void DnsRecordFactory_NSRecordEmptyName()
        {
            var data = new byte[] { 0 };
            var factory = GetFactory(data);
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.NS, QueryClass.IN, 0, data.Length);

            var result = factory.GetRecord(info) as NsRecord;

            Assert.Equal(".", result.NSDName.Value);
        }

        [Fact]
        public void DnsRecordFactory_NSRecord()
        {
            var writer = new DnsDatagramWriter();
            var name = DnsString.Parse("result.example.com");
            writer.WriteHostName(name.Value);
            var factory = GetFactory(writer.Data.ToArray());
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.NS, QueryClass.IN, 0, writer.Data.Count);

            var result = factory.GetRecord(info) as NsRecord;

            Assert.Equal(result.NSDName, name);
        }

        [Fact]
        public void DnsRecordFactory_MXRecordOrderMissing()
        {
            var data = new byte[0];
            var factory = GetFactory(data);
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.MX, QueryClass.IN, 0, data.Length);

            Assert.ThrowsAny<DnsResponseParseException>(() => factory.GetRecord(info));
        }

        [Fact]
        public void DnsRecordFactory_MXRecordNameMissing()
        {
            var data = new byte[] { 1, 2 };
            var factory = GetFactory(data);
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.MX, QueryClass.IN, 0, data.Length);

            Assert.ThrowsAny<DnsResponseParseException>(() => factory.GetRecord(info));
        }

        [Fact]
        public void DnsRecordFactory_MXRecordEmptyName()
        {
            var data = new byte[] { 1, 0, 0 };
            var factory = GetFactory(data);
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.MX, QueryClass.IN, 0, data.Length);

            var result = factory.GetRecord(info) as MxRecord;

            Assert.Equal(256, result.Preference);
            Assert.Equal(".", result.Exchange.Value);
        }

        [Fact]
        public void DnsRecordFactory_MXRecord()
        {
            var name = DnsString.Parse("result.example.com");
            var writer = new DnsDatagramWriter();
            writer.WriteByte(0);
            writer.WriteByte(1);
            writer.WriteHostName(name.Value);

            var factory = GetFactory(writer.Data.ToArray());
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.MX, QueryClass.IN, 0, writer.Data.Count);

            var result = factory.GetRecord(info) as MxRecord;

            Assert.Equal(1, result.Preference);
            Assert.Equal(result.Exchange, name);
        }

        [Fact]
        public void DnsRecordFactory_SOARecordEmpty()
        {
            var data = new byte[0];
            var factory = GetFactory(data);
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.SOA, QueryClass.IN, 0, data.Length);

            Assert.ThrowsAny<DnsResponseParseException>(() => factory.GetRecord(info));
        }

        [Fact]
        public void DnsRecordFactory_SOARecord()
        {
            var data = new byte[] { 0, 0, 0, 0, 0, 1, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0, 4, 0, 0, 0, 5 };
            var factory = GetFactory(data);
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.SOA, QueryClass.IN, 0, data.Length);

            var result = factory.GetRecord(info) as SoaRecord;

            Assert.Equal(".", result.MName.Value);
            Assert.Equal(".", result.RName.Value);
            Assert.True(result.Serial == 1);
            Assert.True(result.Refresh == 2);
            Assert.True(result.Retry == 3);
            Assert.True(result.Expire == 4);
            Assert.True(result.Minimum == 5);
        }

        [Fact]
        public void DnsRecordFactory_SRVRecordEmpty()
        {
            var data = new byte[0];
            var factory = GetFactory(data);
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.SRV, QueryClass.IN, 0, data.Length);

            Assert.ThrowsAny<DnsResponseParseException>(() => factory.GetRecord(info));
        }

        [Fact]
        public void DnsRecordFactory_SRVRecord()
        {
            var name = DnsString.Parse("result.example.com");
            var writer = new DnsDatagramWriter();
            writer.WriteBytes(new byte[] { 0, 1, 1, 0, 2, 3 }, 6);
            writer.WriteHostName(name.Value);
            var factory = GetFactory(writer.Data.ToArray());

            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.SRV, QueryClass.IN, 0, writer.Data.Count);

            var result = factory.GetRecord(info) as SrvRecord;

            Assert.Equal(result.Target, name);
            Assert.True(result.Priority == 1);
            Assert.True(result.Weight == 256);
            Assert.True(result.Port == 515);
        }

        [Fact]
        public void DnsRecordFactory_NAPTRRecord()
        {
            var name = DnsString.Parse("result.example.com");
            var writer = new DnsDatagramWriter();
            writer.WriteUInt16NetworkOrder(0x1e);
            writer.WriteUInt16NetworkOrder(0x00);
            writer.WriteStringWithLengthPrefix(NaptrRecord.S_FLAG.ToString()); 
            writer.WriteStringWithLengthPrefix(NaptrRecord.SIP_UDP_SERVICE_KEY);
            writer.WriteStringWithLengthPrefix("");
            writer.WriteHostName(name.Value);
            var factory = GetFactory(writer.Data.ToArray());

            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.NAPTR, QueryClass.IN, 0, writer.Data.Count);

            var result = factory.GetRecord(info) as NaptrRecord;

            Assert.Equal(name, result.Replacement);
            Assert.True(result.Order == 0x1e);
            Assert.True(result.Preference == 0x00);
            Assert.Equal(NaptrRecord.S_FLAG.ToString(), result.Flags);
            Assert.Equal(NaptrRecord.SIP_UDP_SERVICE_KEY, result.Services);
            Assert.Equal("", result.Regexp);
        }

        [Fact]
        public void DnsRecordFactory_TXTRecordEmpty()
        {
            var textA = "Some Text";
            var lineA = Encoding.ASCII.GetBytes(textA);
            var data = new List<byte> { 5 };
            data.AddRange(lineA);

            var factory = GetFactory(data.ToArray());
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.TXT, QueryClass.IN, 0, data.Count);

            Assert.ThrowsAny<DnsResponseParseException>(() => factory.GetRecord(info));
        }

        [Fact]
        public void DnsRecordFactory_TXTRecordWrongTextLength()
        {
            var data = new byte[0];
            var factory = GetFactory(data);
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.TXT, QueryClass.IN, 0, data.Length);

            var result = factory.GetRecord(info) as TxtRecord;

            Assert.Empty(result.EscapedText);
        }

        [Fact]
        public void DnsRecordFactory_TXTRecord()
        {
            var textA = @"Some lines of text.";
            var textB = "Another line";
            var lineA = Encoding.ASCII.GetBytes(textA);
            var lineB = Encoding.ASCII.GetBytes(textB);
            var data = new List<byte>();
            data.Add((byte)lineA.Length);
            data.AddRange(lineA);
            data.Add((byte)lineB.Length);
            data.AddRange(lineB);

            var factory = GetFactory(data.ToArray());
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.TXT, QueryClass.IN, 0, data.Count);

            var result = factory.GetRecord(info) as TxtRecord;

            Assert.Equal(2, result.EscapedText.Count);
            Assert.Equal(result.EscapedText.ElementAt(0), textA);
            Assert.Equal(result.EscapedText.ElementAt(1), textB);
        }

        [Fact]
        public void DnsRecordFactory_SSHFPRecord()
        {
            var algo = SshfpAlgorithm.RSA;
            var type = SshfpFingerprintType.SHA1;
            var fingerprint = "9DBA55CEA3B8E15528665A6781CA7C35190CF0EC";
            // Value is stored as raw bytes in the record, so convert the HEX string above to it's original bytes
            var fingerprintBytes = Enumerable.Range(0, fingerprint.Length / 2)
                .Select(i => Convert.ToByte(fingerprint.Substring(i * 2, 2), 16));

            var data = new List<byte>
            {
                (byte)algo,
                (byte)type
            };
            data.AddRange(fingerprintBytes);

            var factory = GetFactory(data.ToArray());
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.SSHFP, QueryClass.IN, 0, data.Count);

            var result = factory.GetRecord(info) as SshfpRecord;

            Assert.Equal(SshfpAlgorithm.RSA, result.Algorithm);
            Assert.Equal(SshfpFingerprintType.SHA1, result.FingerprintType);
            Assert.Equal(fingerprint, result.Fingerprint);
        }

        [Fact]
        public void DnsRecordFactory_SpecialChars()
        {
            var textA = "\"äöü \\slash/! @bla.com \"";
            var textB = "(Another line)";
            var lineA = Encoding.UTF8.GetBytes(textA);
            var lineB = Encoding.UTF8.GetBytes(textB);
            var data = new List<byte>();
            data.Add((byte)lineA.Length);
            data.AddRange(lineA);
            data.Add((byte)lineB.Length);
            data.AddRange(lineB);

            var factory = GetFactory(data.ToArray());
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.TXT, QueryClass.IN, 0, data.Count);

            var result = factory.GetRecord(info) as TxtRecord;

            Assert.Equal(2, result.EscapedText.Count);
            Assert.Equal(result.Text.ElementAt(0), textA);
            Assert.Equal("\\\"\\195\\164\\195\\182\\195\\188 \\\\slash/! @bla.com \\\"", result.EscapedText.ElementAt(0));
            Assert.Equal(result.Text.ElementAt(1), textB);
            Assert.Equal(result.EscapedText.ElementAt(1), textB);
        }
    }
}