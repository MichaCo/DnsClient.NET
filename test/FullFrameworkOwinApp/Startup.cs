using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using DnsClient;
using Owin;

namespace FullFrameworkOwinApp
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            //var x = GetService("TenantConfigurationService").Result;

            // forced sync context within async owin, bad bad things will happen
            app.Run(ctx =>
            {
                //var task = Task.Factory.StartNew(()=> GetService("consul"));
                //var uri = task.Unwrap().GetAwaiter().GetResult();
                var uri = GetService("consul").Result;                
                ctx.Response.Write(uri.OriginalString);
                return Task.FromResult(0);
            });

            ////app.Run(async ctx =>
            ////{
            ////    await ctx.Response.WriteAsync((await GetService("TenantConfigurationService")).OriginalString);
            ////    //return Task.FromResult(0);
            ////});
        }

        private static async Task<Uri> GetService(string serviceName)
        {
            // TODO: What about the datacenter/tag features?
            var dnsQuery = string.Format("{0}.service.consul", serviceName);
            var dnsClient = new LookupClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8600));
            var dnsResult = await dnsClient.QueryAsync(dnsQuery, QueryType.SRV).ConfigureAwait(false);

            var srv = dnsResult.Answers.SrvRecords().FirstOrDefault();
            if (srv == null)
            {
                throw new InvalidOperationException($"SRV record not found for {dnsQuery}");
            }

            var ip = dnsResult.Additionals.ARecords().FirstOrDefault(p => p.QueryName == srv.Target)?.Address;
            if (ip == null)
            {
                throw new InvalidOperationException($"Invalid DNS response. A record missing for {srv.Target}");
            }

            // TODO: HTTP scheme is hard-coded!!!
            return new Uri($"http://{ip}:{srv.Port}", UriKind.Absolute);
        }
    }
}