using Microsoft.AspNetCore.Http;
using System.Collections.Generic;


namespace httpserver.Services
{
    public class Profile
    {
        public Dictionary<string, Dictionary<string, string>> HttpGet { get; set; }
        public Dictionary<string, Dictionary<string, string>> HttpPost { get; set; }
    }

    public interface IHttpProfile
    {
        Profile ClientProfile { get; }
        Profile ServerProfile { get; }

        string GetSeralizedClientProfile();
        string HandleGet(HttpContext httpContext, Dictionary<string,string> Message);
        string HandlePost(HttpContext httpContext, Dictionary<string,string> Message);
    }
}
