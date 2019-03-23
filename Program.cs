using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
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
                    // load the SSL settings from Enviornment/Docker
                    string pfxPath = Environment.GetEnvironmentVariable("FACTION_PFX_FILE");
                    string pfxSecret = Environment.GetEnvironmentVariable("FACTION_PFX_PASS");

                    options.Listen(IPAddress.Any, 80);

                    if (System.IO.File.Exists(pfxPath))
                    {
                        options.Listen(IPAddress.Any, 443, listenOptions =>
                        {
                            listenOptions.UseHttps(pfxPath, pfxSecret);
                        });
                    }
                })
                .UseStartup<Startup>();
    }
}
