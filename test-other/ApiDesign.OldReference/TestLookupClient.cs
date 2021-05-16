using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DnsClient;
using DnsClient.Protocol;

namespace ApiDesign.OldReference
{
    public class TestLookupClient
    {
        private readonly LookupClient _client;

        public TestLookupClient()
        {
            _client = new LookupClient(IPAddress.Loopback, IPAddress.Loopback, IPAddress.Loopback);
            _client = new LookupClient(IPAddress.Loopback, 5465);
            _client = new LookupClient(new IPEndPoint(IPAddress.Loopback, 4444), new IPEndPoint(IPAddress.Loopback, 4443), new IPEndPoint(IPAddress.Loopback, 4442));
            _client = new LookupClient();
        }

        public LookupClient SetNonDefaults()
        {
            var client = new LookupClient();
            client.ContinueOnDnsError = false;
            client.EnableAuditTrail = true;
            client.MinimumCacheTimeout = TimeSpan.FromSeconds(11);
            client.Recursion = false;
            client.Retries = 10;
            client.ThrowDnsErrors = true;
            client.Timeout = TimeSpan.FromMinutes(1);
            client.UseCache = false;
            client.UseRandomNameServer = false;
            client.UseTcpFallback = false;
            client.UseTcpOnly = true;
            return client;
        }

        public ILookupClient Client => _client;

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
            await client.QueryAsync("domain", QueryType.A, cancellationToken: default(CancellationToken));
            await client.QueryAsync("domain", QueryType.A, QueryClass.IN, default(CancellationToken));

            await client.QueryReverseAsync(IPAddress.Loopback);
            await client.QueryReverseAsync(IPAddress.Loopback, default(CancellationToken));

            await client.QueryServerAsync(new[] { NameServer.GooglePublicDns.Address }, "domain", QueryType.A);
            await client.QueryServerAsync(new[] { NameServer.GooglePublicDns.Address }, "domain", QueryType.A, cancellationToken: default(CancellationToken));
            await client.QueryServerAsync(new[] { NameServer.GooglePublicDns.Address }, "domain", QueryType.A, QueryClass.IN);
            await client.QueryServerAsync(new[] { NameServer.GooglePublicDns.Address }, "domain", QueryType.A, QueryClass.IN, default(CancellationToken));
            await client.QueryServerReverseAsync(new[] { NameServer.GooglePublicDns.Address }, IPAddress.Loopback);
            await client.QueryServerReverseAsync(new[] { NameServer.GooglePublicDns.Address }, IPAddress.Loopback, default(CancellationToken));

            await client.QueryServerAsync(new[] { NameServer.GooglePublicDns }, "domain", QueryType.A);
            await client.QueryServerAsync(new[] { NameServer.GooglePublicDns }, "domain", QueryType.A, cancellationToken: default(CancellationToken));
            await client.QueryServerAsync(new[] { NameServer.GooglePublicDns }, "domain", QueryType.A, QueryClass.IN);
            await client.QueryServerAsync(new[] { NameServer.GooglePublicDns }, "domain", QueryType.A, QueryClass.IN, default(CancellationToken));
            await client.QueryServerReverseAsync(new[] { NameServer.GooglePublicDns }, IPAddress.Loopback);
            await client.QueryServerReverseAsync(new[] { NameServer.GooglePublicDns }, IPAddress.Loopback, default(CancellationToken));

            await client.GetHostEntryAsync(IPAddress.Loopback);
            await client.GetHostEntryAsync("localhost");
            await client.GetHostNameAsync(IPAddress.Loopback);
            await client.ResolveServiceAsync("domain", "srv", tag: null);
        }

        public void TestProtocol_1_1()
        {
            var info = new ResourceRecordInfo(domainName: "domain", recordType: 0, recordClass: QueryClass.IN, timeToLive: 100, rawDataLength: 100);
            var infoStr = info.DomainName + info.RawDataLength + info.RecordClass + info.RecordType + info.TimeToLive;
            var someDomain = DnsString.FromResponseQueryString("domain");
            var str = someDomain.Original + someDomain.Value;

            var aaaa = new AaaaRecord(info: info, address: IPAddress.IPv6Loopback);
            var aaaaStr = aaaa.Address + aaaa.DomainName;

            var a = new ARecord(info: info, address: IPAddress.Any);
            var aStr = a.Address + a.DomainName;

            var afsdb = new AfsDbRecord(info: info, type: AfsType.Dce, name: someDomain);
            var afsdbStr = afsdb.Hostname + afsdb.SubType;

            var caa = new CaaRecord(info: info, flags: 1, tag: "tag", value: "value");
            var caaStr = caa.Flags + caa.Tag + caa.Value;

            var cname = new CNameRecord(info: info, canonicalName: someDomain);
            var cnameStr = cname.CanonicalName;

            var empty = new EmptyRecord(info: info);

            var hinfo = new HInfoRecord(info: info, cpu: "cpu", os: "os");
            var hinfoStr = hinfo.Cpu + hinfo.OS;

            var mb = new MbRecord(info: info, domainName: someDomain);
            var mbStr = mb.MadName;

            var mg = new MgRecord(info: info, domainName: someDomain);
            var mgStr = mg.MgName;

            var minfo = new MInfoRecord(info: info, rmailBox: someDomain, emailBox: someDomain);
            var minfoStr = minfo.EmailBox + minfo.RMailBox;

            var mr = new MrRecord(info: info, name: someDomain);
            var mrStr = mr.NewName;

            var ns = new NsRecord(info: info, name: someDomain);
            var nsStr = ns.NSDName;

            var nul = new NullRecord(info: info, anything: new byte[] { 1 });
            var nulVal = nul.Anything;

            var ptr = new PtrRecord(info: info, ptrDomainName: someDomain);
            var ptrStr = ptr.PtrDomainName;

            var rp = new RpRecord(info: info, mailbox: someDomain, textName: someDomain);
            var rpStr = rp.MailboxDomainName + rp.TextDomainName;

            var soa = new SoaRecord(info: info, mName: someDomain, rName: someDomain, serial: 1, refresh: 2, retry: 3, expire: 4, minimum: 5);
            var soaStr = soa.MName + soa.RName + soa.Serial + soa.Refresh + soa.Retry + soa.Expire + soa.Minimum;

            var srv = new SrvRecord(info: info, priority: 1, weigth: 10, port: 50, target: someDomain);
            var srvStr = srv.DomainName + srv.Port + srv.Priority + srv.Target;

            var txt = new TxtRecord(info, values: new[] { "test", "test" }, utf8Values: new[] { "test", "test" });
            var txtStr = txt.EscapedText.ToString() + txt.Text;

            // Actual broke compat here and changed it to ints
            ////var uri = new UriRecord(info: info, priority: 1, weight: 2, target: "target");
            ////var uriStr = uri.Priority + uri.Weigth + uri.Target;

            var wks = new WksRecord(info: info, address: IPAddress.Any, protocol: 1, bitmap: new byte[0]);
            var wksStr = wks.Address.ToString() + wks.Protocol + wks.Bitmap.ToString();
        }

#if NETCOREAPP3_1

        public void TestProtocol_1_2()
        {
            var info = new ResourceRecordInfo(domainName: "domain", recordType: 0, recordClass: QueryClass.IN, timeToLive: 100, rawDataLength: 100);

            // Actual broke compat here and changed it to ints
            ////var uri = new UriRecord(info: info, priority: 1, weight: 2, target: "target");
            ////var uriStr = uri.Priority + uri.Weigth + uri.Target;

            var sshfp = new SshfpRecord(info, algorithm: SshfpAlgorithm.Ed25519, fingerprintType: SshfpFingerprintType.SHA1, fingerprint: "finger");
            var sshfpStr = sshfp.Fingerprint + sshfp.Algorithm + sshfp.FingerprintType;
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
