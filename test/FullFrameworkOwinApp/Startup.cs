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
                    var ip = GetService(query).Result;
                    ctx.Response.Write($"{{ \"answer\": \"{ip.ToString()}\"}}");
                }
                catch(Exception ex)
                {
                    ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    ctx.Response.Write(ex.InnerException?.Message ?? ex.Message);
                }

                return Task.FromResult(0);
            });
        }

        private static async Task<IPAddress> GetService(string query)
        {
            var dnsClient = new LookupClient();
            var dnsResult = await dnsClient.QueryAsync(query, QueryType.ANY).ConfigureAwait(false);

            var aRecord = dnsResult.Answers.ARecords().FirstOrDefault();
            if (aRecord == null)
            {
                throw new InvalidOperationException($"Record not found for {query}");
            }

            return aRecord.Address;
        }
    }
}