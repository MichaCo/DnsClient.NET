# DnsClient.NET

[![Build Status](https://dev.azure.com/michaco/DnsClient/_apis/build/status/MichaCo.DnsClient.NET?branchName=dev&label=Build)](https://dev.azure.com/michaco/DnsClient/_build/latest?definitionId=1&branchName=dev)
[![Code Coverage](https://img.shields.io/azure-devops/coverage/michaco/DnsClient/1?label=Coverage&style=flat&color=informational)](https://dev.azure.com/michaco/DnsClient/_build/latest?definitionId=1&branchName=dev)
[![NuGet](https://img.shields.io/nuget/v/DnsClient?color=brightgreen&label=NuGet%20Stable)](https://www.nuget.org/packages/DnsClient)
[![NuGet](https://img.shields.io/nuget/vpre/DnsClient?color=yellow&label=NuGet%20Latest)](https://www.nuget.org/packages/DnsClient) 

DnsClient.NET is a simple yet very powerful and high performance open source library for the .NET Framework to do DNS lookups.

## Usage

See [the DnsClient site][dnsclient] for more details and documentation.

The following example instantiates a new `LookupClient` to query some IP address.

``` csharp

var lookup = new LookupClient();
var result = await lookup.QueryAsync("google.com", QueryType.A);

var record = result.Answers.ARecords().FirstOrDefault();
var ip = record?.Address;
``` 

## Features

### General

* Sync & Async API
* UDP and TCP lookup, configurable if TCP should be used as fallback in case the UDP result is truncated (default=true).
* Configurable EDNS support to change the default UDP buffer size and request security relevant records
* Caching
  * Query result cache based on provided TTL 
  * Minimum TTL setting to overrule the result's TTL and always cache the responses for at least that time. (Even very low value, like a few milliseconds, do make a huge difference if used in high traffic low latency scenarios)
  * Maximum TTL to limit cache duration
  * Cache can be disabled
* Nameserver auto discovery. If no servers are explicitly configured, DnsClient will try its best to resolve them based on your local system configuration.
  This includes DNS servers configured via network interfaces or even via Windows specific [NRPT policies](https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-gpnrpt/8cc31cb9-20cb-4140-9e85-3e08703b4745).
* Multiple DNS endpoints can be configured. DnsClient will use them in random or sequential order (configurable), with re-tries.
* Configurable retry of queries
* Optional audit trail of each response and exception
* Configurable error handling. Throwing DNS errors, like `NotExistentDomain` is turned off by default
* Optional Trace/Logging

### Supported resource records

* A, AAAA, NS, CNAME, SOA, MB, MG, MR, WKS, HINFO, MINFO, MX, RP, TXT, AFSDB, URI, CAA, NULL, SSHFP, TLSA, RRSIG, NSEC, NSEC3, NSEC3PARAM, DNSKEY, DS
* PTR for reverse lookups
* SRV for service discovery. `LookupClient` has some extensions to help with that.
* AXFR zone transfer (as per spec, LookupClient has to be set to TCP mode only for this type. Also, the result depends on if the DNS server trusts your current connection)

## Build from Source

To build and contribute to this project, you must have the latest [.NET 5 SDK](https://dotnet.microsoft.com/download) installed.
Just clone the repository and open the solution in Visual Studio 2019.

## Examples

* See [MiniDig](https://github.com/MichaCo/DnsClient.NET/tree/dev/samples/MiniDig)'s readme for what this example command line tool can do.
* [More documentation and examples][dnsclient]
* The [Samples](https://github.com/MichaCo/DnsClient.NET.Samples) repository (there might be more in the future).

[dnsclient]:https://dnsclient.michaco.net
