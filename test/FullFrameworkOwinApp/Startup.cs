using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DnsClient;
using Owin;

namespace FullFrameworkOwinApp
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.Run(ctx =>
            {
                var query = ctx.Request.Query.Get("q") ?? Dns.GetHostName();
                // explicitly use Result here although middleware could be async
                // just to test the bad blocking behavior of the owin stuff and see
                // if QueryAsync produces a deadlock.
                try
                {
                    var entry = GetService(query).Result;
                    ctx.Response.Write(entry.AddressList.FirstOrDefault()?.ToString());
                }
                catch(Exception ex)
                {
                    ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    ctx.Response.Write(ex.InnerException?.Message ?? ex.Message);
                }

                return Task.FromResult(0);
            });
        }

        private static async Task<ServiceHostEntry> GetService(string query)
        {
            var dnsClient = new LookupClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8600));
            //dnsClient.UseTcpOnly = true;
            //var dnsResult = await dnsClient.QueryAsync(query, QueryType.ANY).ConfigureAwait(false);

            //var aRecord = dnsResult.Answers.ARecords().FirstOrDefault();
            //if (aRecord == null)
            //{
            //    throw new InvalidOperationException($"Record not found for {query}");
            //}

            var service = await dnsClient.ResolveServiceAsync("service.consul", "consul").ConfigureAwait(false);

            return service.FirstOrDefault();
        }
    }
}