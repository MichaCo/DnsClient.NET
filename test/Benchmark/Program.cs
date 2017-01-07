using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DnsClient;

namespace ConsoleApplication2
{
    internal class Program
    {
        private static Random rnd = new Random();
        private static HashSet<ushort> Ids = new HashSet<ushort>();
        private static LookupClient _client;
        private static long _count;
        private static long _reportingCount;
        private static long _bytesCount;
        private static bool _isRunning;
        private static IPEndPoint _endpoint = new IPEndPoint(IPAddress.Parse("192.168.178.21"), 53);

        private static void Main(string[] args)
        {
            _client = new LookupClient(_endpoint);
            _client.UseCache = false;
            _client.EnableAuditTrail = false;

            double seconds = 10;
            int runtime = 1000 * (int)seconds;
            int clients = 40;

            // warmup
            for(var i = 0; i < 1000; i++)
            {
                SendSimple(_endpoint);
            }

            var swatch = Stopwatch.StartNew();
            _count = 0;
            _reportingCount = 0;
            _bytesCount = 0;
            _isRunning = true;
            
            List<Task> actions = RunSync(clients);
            
            while (swatch.Elapsed.Seconds < seconds)
            {
                Task.Delay(999).Wait();
                Console.WriteLine($"Requests per sec: {_reportingCount:N0} {swatch.Elapsed.Seconds}s {_bytesCount / 1000000d:N2}mb received.");
                Console.WriteLine($";;Log: arraysAllocated: {StaticLog.ByteArrayAllocations} arraysReleased: {StaticLog.ByteArrayReleases} queries: {StaticLog.SyncResolveQueryCount} queryTries: {StaticLog.SyncResolveQueryTries}");
                Interlocked.Exchange(ref _reportingCount, 0);
            }
            _isRunning = false;
            Console.WriteLine(";; ############################################################# ;;");
            Console.WriteLine($"Requests per sec: {_count / (swatch.ElapsedMilliseconds / (double)1000):N0} {swatch.Elapsed.Seconds}s {_bytesCount / 1000000d:N2}mb received.");
            Console.WriteLine($";;Log: arraysAllocated: {StaticLog.ByteArrayAllocations} arraysReleased: {StaticLog.ByteArrayReleases} queries: {StaticLog.SyncResolveQueryCount} queryTries: {StaticLog.SyncResolveQueryTries}");
            Console.ReadLine();
        }

        private static List<Task> RunSync(int clients)
        {
            var actions = new List<Task>();
            for (var i = 0; i < clients; i++)
            {
                actions.Add(Task.Run(() =>
                {
                    while (_isRunning)
                    {
                        var r = SendSimple(_endpoint);
                        Interlocked.Add(ref _bytesCount, r);
                        Interlocked.Increment(ref _count);
                        Interlocked.Increment(ref _reportingCount);                        
                    }
                }));
            }

            return actions;
        }

        private static List<Task> RunAsync(int clients)
        {
            var actions = new List<Task>();
            for (var i = 0; i < clients; i++)
            {
                actions.Add(Task.Run(async () =>
                {
                    while (_isRunning)
                    {
                        var r = await SendSimpleAsync(_endpoint);
                        Interlocked.Add(ref _bytesCount, r);
                        Interlocked.Increment(ref _count);
                        Interlocked.Increment(ref _reportingCount);
                    }
                }));
            }

            return actions;
        }

        private static int SendSimple(IPEndPoint endpoint)
        {
            try
            {
                var result = _client.Query("localhost", QueryType.A);
                return result.MessageSize;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        private static async Task<int> SendSimpleAsync(IPEndPoint endpoint)
        {
            try
            {
                var result = await _client.QueryAsync("localhost", QueryType.A);
                return result.MessageSize;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }
    }
}