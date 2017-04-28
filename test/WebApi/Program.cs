using System;
using System.IO;
using System.Linq;
using System.Net;
using DnsClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                   .UseKestrel()
                   .UseContentRoot(Directory.GetCurrentDirectory())
                   .UseUrls("http://*:5000")
                   .UseStartup<Startup>()
                   .Build();

            host.Run();
        }
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvcCore(options =>
            {
            });

            var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8600);
            var lookup = new LookupClient(endpoint);
            lookup.UseCache = true;
            lookup.MinimumCacheTimeout = TimeSpan.FromMilliseconds(1);
            services.AddSingleton(lookup);
        }

        private const string TestText = "{\"Ping\":\"Pong\"}";

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, LookupClient dnsClient)
        {
            loggerFactory.AddConsole();

            app.Use(async (ctx, next) =>
            {
                if (ctx.Request.Path.StartsWithSegments("/test"))
                {
                    await ctx.Response.WriteAsync(TestText);
                }
                else
                {
                    await next.Invoke();
                }
            });

            app.Use(async (ctx, next) =>
            {
                if (ctx.Request.Path.StartsWithSegments("/dns"))
                {
                    var result = await dnsClient.QueryAsync("consul.service.consul", QueryType.SRV);
                    await ctx.Response.WriteAsync("Answers: " + result.Answers.Count);
                }
                else
                {
                    await next.Invoke();
                }
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action}/{id?}");
            });
        }
    }
}