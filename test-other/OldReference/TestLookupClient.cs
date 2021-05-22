using System;
using System.Net;
using System.Threading.Tasks;
using DnsClient;
using DnsClient.Protocol;

namespace OldReference
{
    public class TestLookupClient
    {
        public TestLookupClient()
        {
            _ = new LookupClient(IPAddress.Loopback, IPAddress.Loopback, IPAddress.Loopback);
            _ = new LookupClient(IPAddress.Loopback, 5465);
            _ = new LookupClient(new IPEndPoint(IPAddress.Loopback, 4444), new IPEndPoint(IPAddress.Loopback, 4443), new IPEndPoint(IPAddress.Loopback, 4442));
            _ = new LookupClient();
        }

        public void TestOtherTypes()
        {
            _ = new NameServer(IPAddress.Any);
            var nameServer = new NameServer(new IPEndPoint(IPAddress.Any, 7979));

            _ = nameServer.SupportedUdpPayloadSize;

            ////_ = NameServer.ResolveNameServers(true, false); actually changed the return type.
            ////// Added in 1.2
            ////_ = NameServer.ResolveNameServersNative();
            //// changed return type to IReadonlyCollection at some point...
        }

        public LookupClient SetNonDefaults()
        {
            var client = new LookupClient
            {
                ContinueOnDnsError = false,
                EnableAuditTrail = true,
                MinimumCacheTimeout = TimeSpan.FromSeconds(11),
                Recursion = false,
                Retries = 10,
                ThrowDnsErrors = true,
                Timeout = TimeSpan.FromMinutes(1),
                UseCache = false,
                UseRandomNameServer = false,
                UseTcpFallback = false,
                UseTcpOnly = true
            };

            return client;
        }

        public void TestQuery_1_1()
        {
            var client = new LookupClient();
            client.Query("domain", QueryType.A);
            client.Query("domain", QueryType.A, QueryClass.IN);
            client.QueryReverse(IPAddress.Loopback);

            client.QueryServer(new[] { NameServer.GooglePublicDns.Address }, "domain", QueryType.A);
            client.QueryServer(new[] { NameServer.GooglePublicDns.Address }, "domain", QueryType.A, QueryClass.IN);
            client.QueryServerReverse(new[] { NameServer.GooglePublicDns.Address }, IPAddress.Loopback);

            client.QueryServer(new[] { NameServer.GooglePublicDns }, "domain", QueryType.A);
            client.QueryServer(new[] { NameServer.GooglePublicDns }, "domain", QueryType.A, QueryClass.IN);
            client.QueryServerReverse(new[] { NameServer.GooglePublicDns }, IPAddress.Loopback);

            client.GetHostEntry(IPAddress.Loopback);
            client.GetHostName(IPAddress.Loopback);
            client.ResolveService("domain", "srv", tag: null);
        }

        public async Task TestQueryAsync_1_1()
        {
            var client = new LookupClient();
            await client.QueryAsync("domain", QueryType.A);
            await client.QueryAsync("domain", QueryType.A, QueryClass.IN);
            await client.QueryAsync("domain", QueryType.A, cancellationToken: default);
            await client.QueryAsync("domain", QueryType.A, QueryClass.IN, default);

            await client.QueryReverseAsync(IPAddress.Loopback);
            await client.QueryReverseAsync(IPAddress.Loopback, default);

            await client.QueryServerAsync(new[] { NameServer.GooglePublicDns.Address }, "domain", QueryType.A);
            await client.QueryServerAsync(new[] { NameServer.GooglePublicDns.Address }, "domain", QueryType.A, cancellationToken: default);
            await client.QueryServerAsync(new[] { NameServer.GooglePublicDns.Address }, "domain", QueryType.A, QueryClass.IN);
            await client.QueryServerAsync(new[] { NameServer.GooglePublicDns.Address }, "domain", QueryType.A, QueryClass.IN, default);
            await client.QueryServerReverseAsync(new[] { NameServer.GooglePublicDns.Address }, IPAddress.Loopback);
            await client.QueryServerReverseAsync(new[] { NameServer.GooglePublicDns.Address }, IPAddress.Loopback, default);

            await client.QueryServerAsync(new[] { NameServer.GooglePublicDns }, "domain", QueryType.A);
            await client.QueryServerAsync(new[] { NameServer.GooglePublicDns }, "domain", QueryType.A, cancellationToken: default);
            await client.QueryServerAsync(new[] { NameServer.GooglePublicDns }, "domain", QueryType.A, QueryClass.IN);
            await client.QueryServerAsync(new[] { NameServer.GooglePublicDns }, "domain", QueryType.A, QueryClass.IN, default);
            await client.QueryServerReverseAsync(new[] { NameServer.GooglePublicDns }, IPAddress.Loopback);
            await client.QueryServerReverseAsync(new[] { NameServer.GooglePublicDns }, IPAddress.Loopback, default);

            await client.GetHostEntryAsync(IPAddress.Loopback);
            await client.GetHostEntryAsync("localhost");
            await client.GetHostNameAsync(IPAddress.Loopback);
            await client.ResolveServiceAsync("domain", "srv", tag: null);
        }

        public void TestProtocol_1_1()
        {
            var info = new ResourceRecordInfo(domainName: "domain", recordType: 0, recordClass: QueryClass.IN, timeToLive: 100, rawDataLength: 100);
            _ = info.DomainName + info.RawDataLength + info.RecordClass + info.RecordType + info.TimeToLive;
            var someDomain = DnsString.FromResponseQueryString("domain");
            _ = someDomain.Original + someDomain.Value;

            var aaaa = new AaaaRecord(info: info, address: IPAddress.IPv6Loopback);
            _ = aaaa.Address + aaaa.DomainName;

            var a = new ARecord(info: info, address: IPAddress.Any);
            _ = a.Address + a.DomainName;

            var afsdb = new AfsDbRecord(info: info, type: AfsType.Dce, name: someDomain);
            _ = afsdb.Hostname + afsdb.SubType;

            var caa = new CaaRecord(info: info, flags: 1, tag: "tag", value: "value");
            _ = caa.Flags + caa.Tag + caa.Value;

            var cname = new CNameRecord(info: info, canonicalName: someDomain);
            _ = cname.CanonicalName;

            _ = new EmptyRecord(info: info);

            var hinfo = new HInfoRecord(info: info, cpu: "cpu", os: "os");
            _ = hinfo.Cpu + hinfo.OS;

            var mb = new MbRecord(info: info, domainName: someDomain);
            _ = mb.MadName;

            var mg = new MgRecord(info: info, domainName: someDomain);
            _ = mg.MgName;

            var minfo = new MInfoRecord(info: info, rmailBox: someDomain, emailBox: someDomain);
            _ = minfo.EmailBox + minfo.RMailBox;

            var mr = new MrRecord(info: info, name: someDomain);
            _ = mr.NewName;

            var ns = new NsRecord(info: info, name: someDomain);
            _ = ns.NSDName;

            var nul = new NullRecord(info: info, anything: new byte[] { 1 });
            _ = nul.Anything;

            var ptr = new PtrRecord(info: info, ptrDomainName: someDomain);
            _ = ptr.PtrDomainName;

            var rp = new RpRecord(info: info, mailbox: someDomain, textName: someDomain);
            _ = rp.MailboxDomainName + rp.TextDomainName;

            var soa = new SoaRecord(info: info, mName: someDomain, rName: someDomain, serial: 1, refresh: 2, retry: 3, expire: 4, minimum: 5);
            _ = soa.MName + soa.RName + soa.Serial + soa.Refresh + soa.Retry + soa.Expire + soa.Minimum;

            var srv = new SrvRecord(info: info, priority: 1, weigth: 10, port: 50, target: someDomain);
            _ = srv.DomainName + srv.Port + srv.Priority + srv.Target;

            var txt = new TxtRecord(info, values: new[] { "test", "test" }, utf8Values: new[] { "test", "test" });
            _ = txt.EscapedText.ToString() + txt.Text;

            // Actual broke compat here and changed it to ints
            ////var uri = new UriRecord(info: info, priority: 1, weight: 2, target: "target");
            ////var uriStr = uri.Priority + uri.Weigth + uri.Target;

            var wks = new WksRecord(info: info, address: IPAddress.Any, protocol: 1, bitmap: new byte[0]);
            _ = wks.Address.ToString() + wks.Protocol + wks.Bitmap.ToString();
        }

#if NETCOREAPP3_1

        public void TestProtocol_1_2()
        {
            var info = new ResourceRecordInfo(domainName: "domain", recordType: 0, recordClass: QueryClass.IN, timeToLive: 100, rawDataLength: 100);

            // Actual broke compat here and changed it to ints
            ////var uri = new UriRecord(info: info, priority: 1, weight: 2, target: "target");
            ////var uriStr = uri.Priority + uri.Weigth + uri.Target;

            var sshfp = new SshfpRecord(info, algorithm: SshfpAlgorithm.Ed25519, fingerprintType: SshfpFingerprintType.SHA1, fingerprint: "finger");
            _ = sshfp.Fingerprint + sshfp.Algorithm + sshfp.FingerprintType;
        }

#endif
        ////public void TestProtocol_1_3()
        ////{
        ////    var dnsKey = new DnsKeyRecord();
        ////    var ds = new DsRecord();
        ////    var nsec = new NsecRecord();
        ////    var rrsig = new RRSigRecord();
        ////    new TlsaRecord();
        ////    new UnknownRecord();
        ////}
    }
}
