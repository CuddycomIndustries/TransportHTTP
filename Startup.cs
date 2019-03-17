using System.IO;
using System.IO.Compression;
using httpserver.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace httpserver
{
    public class Startup
    {
        //private IConfiguration _configuration;
        
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();

            services.Add(new ServiceDescriptor(typeof(IConfiguration),
             provider => new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("config.json",
                                         optional: true,
                                         reloadOnChange: true)
                            .Build(),
             ServiceLifetime.Singleton));

            // Add support for GZIP Compression
            services.AddResponseCompression();
            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Fastest;
            });

            services.AddTransient<IHttpProfile, HttpProfile>();
            services.AddTransient<IEndpointHandler, EndpointHandler>();
            services.AddTransient<IFactionAPIHandler, FactionAPIHandler>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, 
                                IHostingEnvironment env, 
                                IConfiguration configuration, 
                                IEndpointHandler endpointHandler,
                                IFactionAPIHandler factionAPIHandler)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Generate the Client Configuration settings from the HTTPProfile
            // and register the configuration with Faction
            var clientConfig = endpointHandler.GetClientConfigurationProfile();
            factionAPIHandler.RegisterServerWithFactionAPI(clientConfig);            
            
            // Get the http routes from the HttpProfile and register Get/Post routes
            var httpProfileRoutes = endpointHandler.GetProfileRoutes();
            var routeBuilder = new RouteBuilder(app);

            // Map a Get and Post route
            foreach (var route in httpProfileRoutes)
            {
                routeBuilder.MapGet("{route}", httpContext =>
                {
                    return httpContext.Response.WriteAsync(endpointHandler.Handle(httpContext));
                });

                routeBuilder.MapPost("{route}", httpContext =>
                {
                    return httpContext.Response.WriteAsync(endpointHandler.Handle(httpContext));
                });

            }

            app.UseRouter(routeBuilder.Build());
            app.UseDefaultFiles();
            app.UseStaticFiles();

        }
    }
}
