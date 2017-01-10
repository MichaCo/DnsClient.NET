# DnsClient.NET
DnsClient.NET is a simple to use, extensible .NET DNS client which targets netstandard1.3 or higher and .Net 4.5.

## Build

[![Build status](https://ci.appveyor.com/api/projects/status/y1nlxim8tkv7w3f4?svg=true)](https://ci.appveyor.com/project/MichaCo/dnsclient-net)

### Download it via NuGet
Package: https://www.nuget.org/packages/DnsClient/

Get beta builds from [MyGet](https://www.myget.org/feed/dnsclient/package/nuget/DnsClient).

## Features
#### General
* Full Async API
* UDP and TCP lookup, configurable if TCP should be used as fallback in case UDP result is truncated (default=true).
* Caching
  * Query result cache based on provided TTL 
  * Minimum TTL setting to overrule the result's TTL and always cache the responses for at least that time. (Even verly low value, like a few milliseconds, do make a huge difference if used in high traffic low latency scenarios)
  * Cache can be disabled alltogether
* Supports mulitple dns endpoints to be configured.
* Retry logic in case of timeouts and errors (configurable)
* Audit trail of each response

#### Supported resource records
* PTR for reverse lookups
* A, AAAA, NS, CNAME, SOA, MB, MG, MR, WKS, HINFO, MINFO, MX, RP, TXT, AFSDB, SRV, URI, CAA
* OPT (currently only for reading the supported UDP buffer size, EDNS version)

## Usage examples:
### Simple usage
The following example instantiates a new `LookupClient` without specifying a DNS endpoint. 
DnsClient.NET will query the system's network adapters to determine available DNS servers.

``` csharp

var lookup = new LookupClient();
var result = await lookup.QueryAsync("google.com", QueryType.ANY);

var record = result.Answers.ARecords().FirstOrDefault();
var address = record?.Address;

``` 

### Specify other DNS servers
To explicitly specify a DNS server, there are some overloads:

Create a client using a DNS server on localhost with the default port (53)

``` csharp
var result = new LookupClient(IPAddress.Parse("127.0.0.1"));
```

Create a client using a DNS server on port 5000:

``` csharp
var endpoint = new IPEndPoint(IPAddress.Parse("192.168.178.23"), 5000);
var result = new LookupClient(endpoint);
```

### Audit Trail
If enabled, the lookup client adds an audit trail to each result, `result.AuditTrail`. This is a simple text field log information, which can be pretty useful for debugging etc...

```csharp
var lookup = new LookupClient();
lookup.EnableAuditTrail = true;
var result = await lookup.QueryAsync("google.com", QueryType.ANY);
```

The result.AuditTrail will have some value like this now:

```
; (3 server found)
;; Got answer:
;; ->>HEADER<<- opcode: Query, status: No Error, id: 27205
;; flags: qr rd ra; QUERY: 1, ANSWER: 11, AUTORITY: 0, ADDITIONAL: 1

;; OPT PSEUDOSECTION:
; EDNS: version: 0, flags:; udp: 4096
;; QUESTION SECTION:
google.com.                             IN      ANY

;; ANSWER SECTION:
google.com.                     160     IN      A       216.58.211.46
google.com.                     152     IN      AAAA    2a00:1450:4016:805::200e
google.com.                     398     IN      MX      30 alt2.aspmx.l.google.com.
google.com.                     398     IN      MX      10 aspmx.l.google.com.
google.com.                     398     IN      MX      40 alt3.aspmx.l.google.com.
google.com.                     398     IN      MX      20 alt1.aspmx.l.google.com.
google.com.                     398     IN      MX      50 alt4.aspmx.l.google.com.
google.com.                     329150  IN      NS      ns1.google.com.
google.com.                     329150  IN      NS      ns4.google.com.
google.com.                     329150  IN      NS      ns2.google.com.
google.com.                     329150  IN      NS      ns3.google.com.

;; Query time: 68 msec
;; SERVER: 127.0.0.1#53
;; WHEN: Sat, 01 Dec 2016 15:10:47 GMT
;; MSG SIZE  rcvd: 263
```


## Examples

The [Samples](https://github.com/MichaCo/DnsClient.NET.Samples) repository will have some solutions to showcase the usage and also to test some functionality.

Also, under [samples](https://github.com/MichaCo/DnsClient.NET/tree/dev/samples) in this repository, there is a great console application which kind of works like the well known "dig".
It doesn't have all the features of course but quite many...

To run it, open a command line windows, navigate to \Samples\MiniDig and run `dotnet run`.

`dotnet run -?` gives you the list of options and commands.

`dotnet run google.com ANY` to query for google.com

If nothing else is specified, it uses the DNS server configured for your local network adapter.
To specify a different server, use the `-s` switch, for example:

`dotnet run -s 8.8.8.8 google.com` to use the public google name server.

## Milestones
### 1.1 release
* More testing
* More RRs parsing

## Motivation and Thanks!
I used [Heijden.Dns](https://github.com/ghuntley/Heijden.Dns) as a baseline for my first steps into the world of DNS RFCs. A big thanks to the original author of the code, Alphons van der Heijden and also Geoffrey Huntley who created a great package out of that. This was a great resource to learn from it and start building this API!

In the end, I decided to write my own API to make it easy to use, more robust, extensible and build on dotnet core for xplat support.
