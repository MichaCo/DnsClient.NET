# DnsClient.NET
DnsClient.NET is a simple to use, extensible .NET DNS client which targets netstandard1.3 or higher and .NET4.5.

## How to use
Simple usage

```` csharp

var lookup = new LookupClient();
var result = await lookup.QueryAsync("google.com", QueryType.ANY);

var record = result.Answers.ARecords().FirstOrDefault();
var address = record?.Address;

```` 
TODO: more examples

## Examples
Under Samples in this repoistory there is a greate console application which kind of works like the well known "dig".
It doesn't have all the features of course but quite many...

To run it, open a command line windows, navigate to \Samples\MiniDig and run `dotnet run`.

`dotnet run -?` gives you the list of options and commands.

`dotnet run google.com ANY` to query for google.com

If not otherwise specified, it uses the DNS server configured for your local network adapter, to specify a different server, use the `-s` switch, e.g. 

`dotnet run -s 8.8.8.8 google.com` to use the public google name server.

## Milestones
The package is currently available as a beta version and is not feature complete yet.

### 1.0 release
Stuff needed for the 1.0 release:
* More testing
* More RRs parsing

## Motivation and Thanks!
I used [Heijden.Dns](https://github.com/ghuntley/Heijden.Dns) as a baseline for my first steps into the world of DNS RFCs. A big thanks to the original author of the code, Alphons van der Heijden and also Geoffrey Huntley who created a great package out of that.
This was a great resource to learn from it and start building this API!

In the end, I decided to write my own API to make it easily useable, extensible and build on dotnet core for xplat support and make it use async all the way, too.
