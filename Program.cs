using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;

namespace httpserver
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseKestrel(options =>
                {
                    
                    options.Listen(IPAddress.Any, 443, listenOptions =>
                    {
                        // load the SSL settings from Enviornment/Docker
                        string pfxPath = Environment.GetEnvironmentVariable("CERTPATH");
                        string pfxSecret = Environment.GetEnvironmentVariable("CERTSECRET");
                        if (!string.IsNullOrEmpty(pfxPath) && !string.IsNullOrEmpty(pfxSecret))
                        {
                            listenOptions.UseHttps(pfxPath, pfxSecret);
                        }
                    });
                })
                .UseStartup<Startup>();
    }
}
