# DnsClient.NET

DnsClient.NET is a simple yet very powerful and high performant open source library for the .NET Framework to do DNS lookups.

## Usage

See http://dnsclient.michaco.net for more details and documentation.

The following example instantiates a new `LookupClient` without specifying a DNS endpoint. 
DnsClient.NET will query your system network adapters to determine available DNS servers.

``` csharp

var lookup = new LookupClient();
var result = await lookup.QueryAsync("google.com", QueryType.ANY);

var record = result.Answers.ARecords().FirstOrDefault();
var address = record?.Address;
``` 

## Builds

[![Build status](https://ci.appveyor.com/api/projects/status/y1nlxim8tkv7w3f4?svg=true)](https://ci.appveyor.com/project/MichaCo/dnsclient-net)

Get it via NuGet https://www.nuget.org/packages/DnsClient/

Get beta builds from [MyGet](https://www.myget.org/feed/dnsclient/package/nuget/DnsClient).

## Features

### General

* Full Async API
* UDP and TCP lookup, configurable if TCP should be used as fallback in case UDP result is truncated (default=true).
* Caching
  * Query result cache based on provided TTL 
  * Minimum TTL setting to overrule the result's TTL and always cache the responses for at least that time. (Even very low value, like a few milliseconds, do make a huge difference if used in high traffic low latency scenarios)
  * Cache can be disabled altogether
* Supports multiple DNS endpoints to be configured
* Configurable retry over configured DNS servers if one or more returned a server error
* Configurable retry logic in case of timeouts and other exceptions
* Optional audit trail of each response and exception
* Configurable error handling. Throwing DNS errors, like `NotExistentDomain` is turned off by default

### Supported resource records

* A, AAAA, NS, CNAME, SOA, MB, MG, MR, WKS, HINFO, MINFO, MX, RP, TXT, AFSDB, URI, CAA, NULL, SSHFP
* PTR for reverse lookups
* SRV For service discovery. `LookupClient` has some extensions to help with that.
* OPT (currently only for reading the supported UDP buffer size, EDNS version)
* AXFR zone transfer (as per spec, LookupClient has to be set to TCP mode only for this type. Also, the result depends on if the DNS server trusts your current connection)

## Build from Source

The solution requires a .NET Core 2.x SDK and the [.NET 4.7.1 Dev Pack](https://www.microsoft.com/net/download/dotnet-framework/net471) being installed.

Just clone the repository and open the solution in Visual Studio 2017.
Or use the dotnet client via command line.

The unit tests don't require any additional setup right now.

If you want to test the different record types, there are config files for Bind under tools. 
Just [download Bind](https://www.isc.org/downloads/) for Windows and copy the binaries to tools/BIND, then run bind.cmd.
If you are running this on Linux, you can use my config files and replace the default ones if you want.

Now, you can use **samples/MiniDig** to query the local DNS server. 
The following should return many different resource records:

``` cmd
dotnet run -s localhost micha.mcnet.com any
```

To test some random domain names, run MiniDig with the `random` sub command (works without setting up Bind, too).

``` cmd
dotnet run random -s localhost
```

## Examples

* The [Samples](https://github.com/MichaCo/DnsClient.NET.Samples) repository will have some solutions to showcase the usage and also to test some functionality.

* [MiniDig](https://github.com/MichaCo/DnsClient.NET/tree/dev/samples/MiniDig) (See the readme over there)

