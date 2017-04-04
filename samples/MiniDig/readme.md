# MiniDig 
MiniDig is an example implementation of a DNS lookup command line utility which uses the DnsClient library.
It is supposed to work similar to the well-known `dig` command line tool on Linux, with a lot fewer options of course.

## How to Build/Run it
To run it, open a command line windows, or bash, navigate to \Samples\MiniDig and run `dotnet restore` and `dotnet run`.

## Help
`dotnet run -?` gives you the list of options and commands.

## Examples
`dotnet run google.com ANY` to query for google.com

If nothing else is specified, it uses the DNS server configured for your local network adapter.
To specify a different server, use the `-s` switch, for example:

`dotnet run -s 8.8.8.8 google.com` to use the public google name server.

## Performance Testing
One subcommand `perf` can be used to run performance tests

## Random Testing
The `random` sub command does a lookup on a list of domain names found in names.txt.