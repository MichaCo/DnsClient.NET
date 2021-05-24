# MiniDig 
MiniDig is an example implementation of a DNS lookup command line utility which uses the DnsClient library.
It is supposed to work similar to the well-known `dig` command line tool on Linux, with a lot fewer options of course.

## How to Build/Run it
To run it, open a command line windows, or bash, navigate to `/Samples/MiniDig` and run `dotnet restore` and `dotnet run`.

MiniDig is targeting .NET5 and will run on any supported platform as long as the .NET5 SDK or runtime is installed.

## Examples
`dotnet run google.com ANY` to query for google.com

Example output:
```csharp
; <<>> MiniDiG 1.0.0.0 Microsoft Windows 10.0.19042 <<>> google.com any
; Servers: 192.168.0.2:53
; (1 server found)
;; Got answer:
;; ->>HEADER<<- opcode: Query, status: No Error, id: 39207
;; flags: qr rd ra; QUERY: 1, ANSWER: 22, AUTHORITY: 0, ADDITIONAL: 1

;; OPT PSEUDOSECTION:
; EDNS: version: 0, flags:; UDP: 1472; code: NoError
;; QUESTION SECTION:
google.com.                             IN      ANY

;; ANSWER SECTION:
google.com.  1195    IN      A       216.58.207.142
google.com.  1195    IN      AAAA    2a00:1450:4016:806::200e
google.com.  1195    IN      SOA     ns1.google.com. dns-admin.google.com. 375442030 900 900 1800 60
google.com.  345595  IN      NS      ns2.google.com.
google.com.  345595  IN      NS      ns4.google.com.
google.com.  345595  IN      NS      ns3.google.com.
google.com.  345595  IN      NS      ns1.google.com.
google.com.  86395   IN      CAA     0 issue "pki.goog"
google.com.  3595    IN      TXT     "docusign=05958488-4752-4ef2-95eb-aa7ba8a3bd0e"
google.com.  3595    IN      TXT     "docusign=1b0a6754-49b1-4db5-8540-d2c12664b289"
google.com.  3595    IN      TXT     "facebook-domain-verification=22rm551cu4k0ab0bxsw536tlds4h95"
google.com.  3595    IN      TXT     "google-site-verification=TV9-DBe4R80X4v0M4U_bd_J9cpOJM0nikft0jAgjmsQ"
google.com.  3595    IN      TXT     "MS=E4A68B9AB2BB9670BCE15412F62916164C0B20BB"
google.com.  3595    IN      TXT     "globalsign-smime-dv=CDYX+XFHUw2wml6/Gb8+59BsH31KzUr6c1l2BPvqKX8="
google.com.  3595    IN      TXT     "google-site-verification=wD8N7i1JTNTkezJ49swvWW48f8_9xveREV4oB-0Hf5o"
google.com.  3595    IN      TXT     "apple-domain-verification=30afIBcvSuDV2PLX"
google.com.  3595    IN      TXT     "v=spf1 include:_spf.google.com ~all"
google.com.  1195    IN      MX      10 aspmx.l.google.com.
google.com.  1195    IN      MX      20 alt1.aspmx.l.google.com.
google.com.  1195    IN      MX      30 alt2.aspmx.l.google.com.
google.com.  1195    IN      MX      40 alt3.aspmx.l.google.com.
google.com.  1195    IN      MX      50 alt4.aspmx.l.google.com.

;; Query time: 58 msec
;; SERVER: 192.168.0.2#53
;; WHEN: Mon May 24 21:52:45 Z 2021
;; MSG SIZE  rcvd: 922
```

If nothing else is specified, it uses the DNS server configured for your local network adapter.
To specify a different server, use the `-s` switch, for example:

`dotnet run -s 8.8.8.8 google.com` to use the public Google name server.


`dotnet run -s 127.0.0.1#8600` to also specify a custom port.


## Performance Testing
One subcommand `perf` can be used to run performance tests

## Random Testing
The `random` sub command does a lookup on a list of domain names found in names.txt.
