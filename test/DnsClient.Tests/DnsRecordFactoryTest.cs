// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DnsClient.Internal;
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

        internal DnsRecordFactory GetFactory(ArraySegment<byte> data)
        {
            return new DnsRecordFactory(new DnsDatagramReader(data));
        }

        [Fact]
        public void DnsRecordFactory_PTRRecordNotEnoughData()
        {
            var data = Array.Empty<byte>();
            var factory = GetFactory(data);
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.PTR, QueryClass.IN, 0, data.Length);

            void act() => factory.GetRecord(info);
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
            using var writer = new DnsDatagramWriter();
            writer.WriteHostName(name.Value);
            var factory = GetFactory(writer.Data);
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.PTR, QueryClass.IN, 0, writer.Data.Count);

            var result = factory.GetRecord(info) as PtrRecord;

            Assert.Equal(result.PtrDomainName, name);
        }

        [Fact]
        public void DnsRecordFactory_MBRecord()
        {
            var name = DnsString.Parse("Müsli.de");
            using var writer = new DnsDatagramWriter();
            writer.WriteHostName(name.Value);
            var factory = GetFactory(writer.Data);
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
            Assert.Contains("IPv4", ex.Message, StringComparison.OrdinalIgnoreCase);
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
            Assert.Contains("IPv6", ex.Message, StringComparison.OrdinalIgnoreCase);
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
            var data = Array.Empty<byte>();
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
            using var writer = new DnsDatagramWriter();
            var name = DnsString.Parse("result.example.com");
            writer.WriteHostName(name.Value);
            var factory = GetFactory(writer.Data);
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.NS, QueryClass.IN, 0, writer.Data.Count);

            var result = factory.GetRecord(info) as NsRecord;

            Assert.Equal(result.NSDName, name);
        }

        [Fact]
        public void DnsRecordFactory_MXRecordOrderMissing()
        {
            var data = Array.Empty<byte>();
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
            using var writer = new DnsDatagramWriter();
            writer.WriteByte(0);
            writer.WriteByte(1);
            writer.WriteHostName(name.Value);

            var factory = GetFactory(writer.Data);
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.MX, QueryClass.IN, 0, writer.Data.Count);

            var result = factory.GetRecord(info) as MxRecord;

            Assert.Equal(1, result.Preference);
            Assert.Equal(result.Exchange, name);
        }

        [Fact]
        public void DnsRecordFactory_SOARecordEmpty()
        {
            var data = Array.Empty<byte>();
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
            Assert.Equal((uint)1, result.Serial);
            Assert.Equal((uint)2, result.Refresh);
            Assert.Equal((uint)3, result.Retry);
            Assert.Equal((uint)4, result.Expire);
            Assert.Equal((uint)5, result.Minimum);
        }

        [Fact]
        public void DnsRecordFactory_SRVRecordEmpty()
        {
            var data = Array.Empty<byte>();
            var factory = GetFactory(data);
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.SRV, QueryClass.IN, 0, data.Length);

            Assert.ThrowsAny<DnsResponseParseException>(() => factory.GetRecord(info));
        }

        [Fact]
        public void DnsRecordFactory_SRVRecord()
        {
            var name = DnsString.Parse("result.example.com");
            using var writer = new DnsDatagramWriter();
            writer.WriteBytes(new byte[] { 0, 1, 1, 0, 2, 3 }, 6);
            writer.WriteHostName(name.Value);
            var factory = GetFactory(writer.Data);

            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.SRV, QueryClass.IN, 0, writer.Data.Count);

            var result = factory.GetRecord(info) as SrvRecord;

            Assert.Equal(result.Target, name);
            Assert.Equal(1, result.Priority);
            Assert.Equal(256, result.Weight);
            Assert.Equal(515, result.Port);
        }

        [Fact]
        public void DnsRecordFactory_NAPTRRecord()
        {
            var name = DnsString.Parse("result.example.com");
            using var writer = new DnsDatagramWriter();
            writer.WriteUInt16NetworkOrder(0x1e);
            writer.WriteUInt16NetworkOrder(0x00);
            writer.WriteStringWithLengthPrefix(NAPtrRecord.SFlag.ToString());
            writer.WriteStringWithLengthPrefix(NAPtrRecord.ServiceKeySipUdp);
            writer.WriteStringWithLengthPrefix("");
            writer.WriteHostName(name.Value);
            var factory = GetFactory(writer.Data);

            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.NAPTR, QueryClass.IN, 0, writer.Data.Count);

            var result = factory.GetRecord(info) as NAPtrRecord;

            Assert.Equal(name, result.Replacement);
            Assert.Equal(0x1e, result.Order);
            Assert.Equal(0x00, result.Preference);
            Assert.Equal(NAPtrRecord.SFlag.ToString(), result.Flags);
            Assert.Equal(NAPtrRecord.ServiceKeySipUdp, result.Services);
            Assert.Equal("", result.RegularExpression);
        }

        [Fact]
        public void DnsRecordFactory_CertRecord()
        {
            var expectedPublicKey = @"-----BEGIN CERTIFICATE-----
MIIEMzCCAxugAwIBAgIBAzANBgkqhkiG9w0BAQsFADAmMSQwIgYDVQQDDBtkY2R0
MzEuaGVhbHRoaXQuZ292X2NhX3Jvb3QwHhcNMjIwMjA0MTUzNzUxWhcNMzIwMjA1
MDE0OTUxWjBBMS0wKwYJKoZIhvcNAQkBFh5kMUBkb21haW4xLmRjZHQzMS5oZWFs
dGhpdC5nb3YxEDAOBgNVBAMMB0QxX3ZhbEEwggEiMA0GCSqGSIb3DQEBAQUAA4IB
DwAwggEKAoIBAQDHPWJogAq6zCU1zU6ar4GAvRb6bjCTSzm19E98E3dCCG8ZSgWH
yZh3w6M/btu7qMDStrpzMGD1H5TiqS/mEFNNcJP2r8C6T8RKV2xEqhsJlwOoguzJ
4MyePoVYG84/gm5v03BCp91uoz4O1WFrppu439njipv8wUwsvf6ukidhAgP9mEoN
w1sCB1U9zOtpPmbRczMrYyDBWqFaxiaDD9xYaYqal7Ph7adKohBDZA1P7H/Jkxdf
uCwULVDn+bcHD3eW9NToeZ7gc0CV75kVnI/7WbJ6mfx72zOIzEm1AFed36yuEpal
VjCzhJO4ZmmfJxfXr36UICKHQIM/xwSEXqJtAgMBAAGjggFPMIIBSzAfBgNVHSME
GDAWgBSIM9vz74ArTwMFMk3q5ShNOYQhMjApBgNVHQ4EIgQg1WLu98WJoAtR1X7K
ZiHWfIcONgrBBtzuLgNWkQklJugwCQYDVR0TBAIwADAOBgNVHQ8BAf8EBAMCBaAw
KQYDVR0RBCIwIIEeZDFAZG9tYWluMS5kY2R0MzEuaGVhbHRoaXQuZ292MFUGA1Ud
HwROMEwwSqBIoEaGRGh0dHA6Ly9wa2kuZGNkdDMxLmhlYWx0aGl0LmdvdjoxMDA4
MC9kY2R0MzEuaGVhbHRoaXQuZ292X2NhX3Jvb3QuY3JsMGAGCCsGAQUFBwEBBFQw
UjBQBggrBgEFBQcwAoZEaHR0cDovL3BraS5kY2R0MzEuaGVhbHRoaXQuZ292OjEw
MDgwL2RjZHQzMS5oZWFsdGhpdC5nb3ZfY2Ffcm9vdC5jZXIwDQYJKoZIhvcNAQEL
BQADggEBAGqMC2kEA6acNgmUueCbPuLj7uePRGaRk6x0rSEY6mTGoBXci+s9EXbx
a7d/glNFNgQC9KP35esriqSfUn2bsDmtlTs+A79+ldMRH5SWvEmI5f7s9SitLIYR
uRBLE693R7/1DjyUrEFxpdL16O8Y2kIKO9S8lrscNBOg7hW0RKYb4VBnlsNw3jk2
rXyGcFZ63D8VsdgUJTh2BKhpiY37gd/+ILUcylpmC5Uf3yWM2wYRMS6IVACllv+U
PoPWSE2fsrMpfCtDFeUL71gn8g6TYIctVHTn4OeuhHQ6Yt21rgQnlpDFVt0p9sGl
H+L10KwE7wqqmkxwfib5kwgNyrlXtx0=
-----END CERTIFICATE-----";

            var expectedBytes = Encoding.UTF8.GetBytes(expectedPublicKey);
            var name = DnsString.Parse("example.com");
            using var memory = new PooledBytes(expectedBytes.Length);

            using var writer = new DnsDatagramWriter(new ArraySegment<byte>(memory.Buffer));
            writer.WriteInt16NetworkOrder((short)CertificateType.PKIX); // 2 bytes
            writer.WriteInt16NetworkOrder(27891); // 2 bytes
            writer.WriteByte((byte)DnsSecurityAlgorithm.RSASHA256);  // 1 byte
            writer.WriteBytes(expectedBytes, expectedBytes.Length);

            var factory = GetFactory(writer.Data);

            var info = new ResourceRecordInfo(name, ResourceRecordType.CERT, QueryClass.IN, 0, writer.Data.Count);

            var result = factory.GetRecord(info) as CertRecord;
            Assert.NotNull(result);
            Assert.Equal(27891, result.KeyTag);
            Assert.Equal(CertificateType.PKIX, result.CertType);
            Assert.Equal(DnsSecurityAlgorithm.RSASHA256, result.Algorithm);
            Assert.Equal(expectedBytes, result.PublicKey);

            using var cert = new X509Certificate2(Convert.FromBase64String(result.PublicKeyAsString));
            Assert.Equal("sha256RSA", cert.SignatureAlgorithm.FriendlyName);
            Assert.Equal("CN=D1_valA, E=d1@domain1.dcdt31.healthit.gov", cert.Subject);

            var x509Extension = cert.Extensions["2.5.29.17"];
            Assert.NotNull(x509Extension);
            var asnData = new AsnEncodedData(x509Extension.Oid, x509Extension.RawData);
            Assert.Contains("d1@domain1.dcdt31.healthit.gov", asnData.Format(false), StringComparison.OrdinalIgnoreCase);
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
            var data = Array.Empty<byte>();
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
        public void DnsRecordFactory_TXTRecord_DoNotEscape()
        {
            var text = "v=DKIM1; k=rsa;";
            var line = Encoding.ASCII.GetBytes(text);
            var data = new List<byte>();
            data.Add((byte)line.Length);
            data.AddRange(line);

            var factory = GetFactory(data.ToArray());
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.TXT, QueryClass.IN, 0, data.Count);

            var result = factory.GetRecord(info) as TxtRecord;

            Assert.Equal(result.EscapedText.ElementAt(0), text);
        }

        [Fact]
        public void DnsRecordFactory_TLSARecord()
        {
            var certificateUsage = 0;
            var selector = 1;
            var matchingType = 1;
            var certificateAssociationData = "CDE0D742D6998AA554A92D890F8184C698CFAC8A26FA59875A990C03E576343C";
            var expectedBytes = Enumerable.Range(0, certificateAssociationData.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(certificateAssociationData.Substring(x, 2), 16))
                .ToArray();

            var data = new List<byte>()
            {
                (byte)certificateUsage,
                (byte)selector,
                (byte)matchingType,
            };

            data.AddRange(expectedBytes);

            var factory = GetFactory(data.ToArray());
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.TLSA, QueryClass.IN, 0, data.Count);

            var result = factory.GetRecord(info) as TlsaRecord;

            Assert.Equal((TlsaCertificateUsage)certificateUsage, result.CertificateUsage);
            Assert.Equal((TlsaSelector)selector, result.Selector);
            Assert.Equal((TlsaMatchingType)matchingType, result.MatchingType);
            // Checking this in both directions
            Assert.Equal(expectedBytes, result.CertificateAssociationData);
            Assert.Equal(certificateAssociationData, result.CertificateAssociationDataAsString);
        }

        [Fact]
        public void DnsRecordFactory_RRSIGRecord()
        {
            var type = ResourceRecordType.NSEC;
            var algorithmNumber = DnsSecurityAlgorithm.ECDSAP256SHA256;
            var labels = 5;
            var originalTtl = 300;
            var signatureExpiration = 1589414400;
            var signatureInception = 1587600000;
            short keytag = 3942;
            var signersName = DnsString.Parse("result.example.com");
            var signatureString = "kfyyKQoPZJFyOFSDqav7wj5XNRPqZssV2K2k8MJun28QSsCMHyWOjw9Hk4KofnEIUWNui3mMgAEFYbwoeRKkMf5uDAh6ryJ4veQNj86mgYJrpJppUplqlqJE8o1bx0I1VfwheL+M23bL5MnqSGiI5igmMDyeVUraVOO4RQyfGN0=";
            var signature = Convert.FromBase64String(signatureString);

            using var writer = new DnsDatagramWriter();

            writer.WriteInt16NetworkOrder((short)type);
            writer.WriteByte((byte)algorithmNumber);
            writer.WriteByte((byte)labels);
            writer.WriteInt32NetworkOrder(originalTtl);
            writer.WriteInt32NetworkOrder(signatureExpiration);
            writer.WriteInt32NetworkOrder(signatureInception);
            writer.WriteInt16NetworkOrder(keytag);
            writer.WriteHostName(signersName.Value);
            writer.WriteBytes(signature, signature.Length);

            var factory = GetFactory(writer.Data);
            var info = new ResourceRecordInfo("query.example.com", ResourceRecordType.RRSIG, QueryClass.IN, 0, writer.Data.Count);

            var result = factory.GetRecord(info) as RRSigRecord;
            Assert.Equal(type, result.CoveredType);
            Assert.Equal(algorithmNumber, result.Algorithm);
            Assert.Equal(labels, result.Labels);
            Assert.Equal(originalTtl, result.OriginalTtl);
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(signatureExpiration), result.SignatureExpiration);
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(signatureInception), result.SignatureInception);
            Assert.Equal(signersName.Value, result.SignersName);

            Assert.Equal(signature, result.Signature);
            Assert.Equal(signatureString, result.SignatureAsString);
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
            Assert.Equal("\\\"\\195\\164\\195\\182\\195\\188 \\slash/! @bla.com \\\"", result.EscapedText.ElementAt(0));
            Assert.Equal(result.Text.ElementAt(1), textB);
            Assert.Equal(result.EscapedText.ElementAt(1), textB);
        }

        [Fact]
        public void DnsRecordFactory_DnsKeyRecord()
        {
            var expectedPublicKey = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var expectedBytes = Encoding.UTF8.GetBytes(expectedPublicKey);
            var name = DnsString.Parse("example.com");
            using var writer = new DnsDatagramWriter();
            writer.WriteInt16NetworkOrder(256);
            writer.WriteByte(3);
            writer.WriteByte((byte)DnsSecurityAlgorithm.RSASHA256);
            writer.WriteBytes(expectedBytes, expectedBytes.Length);

            var factory = GetFactory(writer.Data);

            var info = new ResourceRecordInfo(name, ResourceRecordType.DNSKEY, QueryClass.IN, 0, writer.Data.Count);

            var result = factory.GetRecord(info) as DnsKeyRecord;
            Assert.Equal(expectedBytes, result.PublicKey);
            Assert.Equal(256, result.Flags);
            Assert.Equal(3, result.Protocol);
            Assert.Equal(DnsSecurityAlgorithm.RSASHA256, result.Algorithm);
        }

        [Fact]
        public void DnsRecordFactory_DsRecord()
        {
            var expectedDigest = "3490A6806D47F17A34C29E2CE80E8A999FFBE4BE";
            var expectedBytes = Enumerable.Range(0, expectedDigest.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(expectedDigest.Substring(x, 2), 16))
                     .ToArray();

            var name = DnsString.Parse("example.com");
            using var writer = new DnsDatagramWriter();
            writer.WriteInt16NetworkOrder(31589);
            writer.WriteByte(8); // algorithm
            writer.WriteByte(1); // type
            writer.WriteBytes(expectedBytes, expectedBytes.Length);

            var factory = GetFactory(writer.Data);

            var info = new ResourceRecordInfo(name, ResourceRecordType.DS, QueryClass.IN, 0, writer.Data.Count);

            var result = factory.GetRecord(info) as DsRecord;
            Assert.Equal(expectedBytes, result.Digest);
            Assert.Equal(expectedDigest, result.DigestAsString);
            Assert.Equal(31589, result.KeyTag);
            Assert.Equal(8, (int)result.Algorithm);
            Assert.Equal(1, result.DigestType);
        }

        [Fact]
        public void DnsRecordFactory_NSecRecord()
        {
            var expectedBitMap = new byte[] { 0, 7, 98, 1, 128, 8, 0, 3, 128 };
            var expectedTypes = new[]
            {
                ResourceRecordType.A,
                ResourceRecordType.NS,
                ResourceRecordType.SOA,
                ResourceRecordType.MX,
                ResourceRecordType.TXT,
                ResourceRecordType.AAAA,
                ResourceRecordType.RRSIG,
                ResourceRecordType.NSEC,
                ResourceRecordType.DNSKEY
            };

            var bitmap = NSecRecord.WriteBitmap(expectedTypes.Select(p => (ushort)p).ToArray()).ToArray();

            var name = DnsString.Parse("example.com");
            using var writer = new DnsDatagramWriter();
            writer.WriteHostName(name);
            writer.WriteBytes(bitmap, bitmap.Length);

            var factory = GetFactory(writer.Data);

            var info = new ResourceRecordInfo(name, ResourceRecordType.NSEC, QueryClass.IN, 0, writer.Data.Count);

            var result = factory.GetRecord(info) as NSecRecord;
            Assert.Equal(expectedBitMap, bitmap);
            Assert.Equal(expectedBitMap, result.TypeBitMapsRaw);
            Assert.Equal(expectedTypes.Length, result.TypeBitMaps.Count);
            Assert.Equal(expectedTypes, result.TypeBitMaps);
            Assert.Equal(name, result.NextDomainName);
        }

        [Fact]
        public void DnsRecordFactory_NSec3Record()
        {
            var expectedTypes = new[]
            {
                ResourceRecordType.A,
                ResourceRecordType.NS,
                ResourceRecordType.SOA,
                ResourceRecordType.MX,
                ResourceRecordType.TXT,
                ResourceRecordType.AAAA,
                ResourceRecordType.RRSIG,
                ResourceRecordType.NSEC3PARAM
            };

            var expectedBitmap = NSecRecord.WriteBitmap(expectedTypes.Select(p => (ushort)p).ToArray()).ToArray();
            var salt = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var nextName = Enumerable.Repeat(0, 50).Select((p, i) => (byte)i).ToArray();
            var nameEncoded = Base32Hex.ToBase32HexString(nextName);
            var name = DnsString.Parse("example.com");

            using var writer = new DnsDatagramWriter();
            writer.WriteByte(1); // Algorithm
            writer.WriteByte(2); // Flags
            writer.WriteUInt16NetworkOrder(100); // Iterations
            writer.WriteByte((byte)salt.Length);
            writer.WriteBytes(salt, salt.Length);
            writer.WriteByte((byte)nextName.Length);
            writer.WriteBytes(nextName, nextName.Length);
            writer.WriteBytes(expectedBitmap, expectedBitmap.Length);

            var factory = GetFactory(writer.Data);

            var info = new ResourceRecordInfo(name, ResourceRecordType.NSEC3, QueryClass.IN, 0, writer.Data.Count);

            var result = factory.GetRecord(info) as NSec3Record;

            Assert.Equal(1, result.HashAlgorithm);
            Assert.Equal(2, result.Flags);
            Assert.Equal(100, result.Iterations);
            Assert.Equal(salt, result.Salt);
            Assert.Equal(nextName, result.NextOwnersName);
            Assert.Equal(nameEncoded, result.NextOwnersNameAsString);
            Assert.Equal(expectedTypes, result.TypeBitMaps);
        }

        [Fact]
        public void DnsRecordFactory_NSec3ParamRecord()
        {
            var salt = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var name = DnsString.Parse("example.com");

            using var writer = new DnsDatagramWriter();
            writer.WriteByte(1); // Algorithm
            writer.WriteByte(2); // Flags
            writer.WriteUInt16NetworkOrder(100); // Iterations
            writer.WriteByte((byte)salt.Length);
            writer.WriteBytes(salt, salt.Length);

            var factory = GetFactory(writer.Data);

            var info = new ResourceRecordInfo(name, ResourceRecordType.NSEC3PARAM, QueryClass.IN, 0, writer.Data.Count);

            var result = factory.GetRecord(info) as NSec3ParamRecord;

            Assert.Equal(1, result.HashAlgorithm);
            Assert.Equal(2, result.Flags);
            Assert.Equal(100, result.Iterations);
            Assert.Equal(salt, result.Salt);
        }

        [Fact]
        public void DnsRecord_TestBitmap_MaxWindows()
        {
            var data = Enumerable.Repeat(0, ushort.MaxValue).Select((v, i) => (ushort)i).ToArray();

            var bitmap = NSecRecord.WriteBitmap(data).ToArray();

            var result = NSecRecord.ReadBitmap(bitmap).OrderBy(p => p).Select(p => (ushort)p).ToArray();
            _ = NSecRecord.ReadBitmap(bitmap).OrderBy(p => p).Select(p => (ResourceRecordType)p).ToArray();

            Assert.Equal(data, result);
        }
    }
}
