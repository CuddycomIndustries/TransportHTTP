using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace httpserver.Services
{
    public interface IEndpointHandler
    {
        string Handle(HttpContext httpContext);
        List<string> GetProfileRoutes();
        string GetClientConfigurationProfile();
    }
}
