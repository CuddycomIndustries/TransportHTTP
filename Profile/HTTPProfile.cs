using httpserver.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace httpserver
{
    public class HttpProfile : IHttpProfile
    {
        public Profile ClientProfile => 
            new Profile
            {
                // Client Http Get profile, used when the client has no data to post (i.e. standard checkin)
                HttpGet = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "Server",
                            new Dictionary<string, string>
                            {
                                {"IgnoreSSL", "true"},
                                {"Host", "https://localhost:44335" },
                                {"URLs", "/faction.html" }
                            }
                        },
                        {
                            "Headers",
                            new Dictionary<string, string>
                            {
                                {"Referer", "http://faction.com"},
                                {"User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko"}
                            }
                        },
                        {
                            // Where the Client's payload should be located on a Get request
                            "ClientPayload",
                            new Dictionary<string, string>
                            {
                                { "Message", "Cookie::_cfid" },
                                { "AgentName", "Header::x-token" },
                                { "StageName", "Cookie::sessionId" }
                            }
                        },
                        {
                            // Where the cilent should look for the Server Payload
                            "ServerPayload",
                            ServerProfile.HttpGet["Payload"]
                        }
                    },
                // Client Http POST profile, used when the client is responding with data
                HttpPost = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "Server",
                            new Dictionary<string, string>
                            {
                                {"IgnoreSSL", "true"},
                                {"Host", "https://localhost:44335" },
                                {"URLs", "/weather.html" }
                            }
                        },
                        {
                            "Headers",
                            new Dictionary<string, string>
                            {
                                {"Referer", "http://faction.com"},
                                {"User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko"}

                            }
                        },
                        {
                            "Cookies",
                            new Dictionary<string, string>
                            {
                                {"id", "a3fWa"}
                            }
                        },
                        {
                            "ClientPayload",
                            new Dictionary<string, string>
                            {
                                { "Message", "Body::city" },
                                { "AgentName", "Header::x-token" },
                                { "StageName", "Cookie::sessionId" }
                            }
                        },
                        {
                            "ServerPayload",
                            ServerProfile.HttpPost["Payload"]
                        }
                    }
            };
        public Profile ServerProfile =>
            new Profile
            {
                HttpGet = new Dictionary<string, Dictionary<string, string>>
                {
                    {
                        "URLs",
                        new Dictionary<string, string>
                        {
                            { "/faction.html", "<h1>Hello World!</h1><span id = 'notpayload'>Something else entierly</span> <span id ='MESSAGE'>%%MESSAGE%%</span> <span>Not the payload</span>"},
                            { "/socks.js",  "<h1></h1><script>var myHeading = document.querySelector('h1');myHeading.textContent = 'Hello world!'; var MESSAGE='%%MESSAGE%%';</script> <script>var test = 'nothing';</script>"}
                        }
                    },
                    {
                        "Headers",
                        new Dictionary<string, string>
                        {
                            { "ServerName", "FC2 1.0" },
                            { "X-PoweredBy", "Faction" }
                        }
                    },
                    {
                        "Payload",
                        new Dictionary<string, string>
                        {
                            { "Message", "Body::%%MESSAGE%%" },
                            { "AgentName", "Header::auth-data" }
                        }
                    }
                },
                HttpPost = new Dictionary<string, Dictionary<string, string>>
                {
                    {
                        "URLs",
                        new Dictionary<string, string>
                        {
                            {"/weather.html", "<h1>It's snowing!</h1>"}
                        }
                    },
                    {
                        "Headers",
                        new Dictionary<string, string>
                        {
                            { "ServerName", "FC2 1.0" },
                            { "X-PoweredBy", "Faction" }
                        }
                    },
                    {
                        "Payload",
                        new Dictionary<string, string>
                        {
                            { "Message", "Header::session-data" },
                            { "AgentName", "Header::auth-data" }
                        }
                    }
                }
            };

        public string HandleGet(HttpContext httpContext, Dictionary<string,string> Message)
        {
            var _serverProfile = ServerProfile.HttpGet;
            
            // Load any content for the requested route
            var route = "/" + httpContext.GetRouteValue("route").ToString();
            var resultContent = _serverProfile["URLs"].Where(key => key.Key == route)
                .FirstOrDefault()
                .Value;

            // Update the content 
            if (_serverProfile["Payload"].Count != 0)
            {
                foreach (var payload in _serverProfile["Payload"])
                {
                    if (payload.Value.Split("::")[0] == "Body")
                        // find the specified value in the body of the HTML content and replace with the 
                        resultContent = resultContent.Replace(payload.Value.Split("::")[1], Message[payload.Key]);
                    if (payload.Value.Split("::")[0] == "Header")
                        httpContext.Response.Headers.TryAdd(payload.Value.Split("::")[1], Message[payload.Key]);
                }
            }
            // Set the payload content/location
            if (_serverProfile["Headers"].Count != 0)
            {
                // Add any Get profile specific response headers
                foreach (var header in _serverProfile["Headers"])
                {
                    httpContext.Response.Headers.TryAdd(header.Key, header.Value);
                }
            }

            return resultContent;
        }

        public string HandlePost(HttpContext httpContext, Dictionary<string, string> Message)
        {
            var _serverProfile = ServerProfile.HttpPost;

            // Load any content for the requested route
            var route = "/" + httpContext.GetRouteValue("route").ToString();
            var resultContent = _serverProfile["URLs"].Where(key => key.Key == route)
                .FirstOrDefault()
                .Value;

            // Update the content 
            if (_serverProfile["Payload"].Count != 0)
            {
                foreach (var payload in _serverProfile["Payload"])
                {
                    if (payload.Value.Split("::")[0] == "Body")
                        // find the specified value in the body of the HTML content and replace with the 
                        resultContent = resultContent.Replace(payload.Value.Split("::")[1], Message[payload.Key]);
                    if (payload.Value.Split("::")[0] == "Header")
                        httpContext.Response.Headers.TryAdd(payload.Value.Split("::")[1], Message[payload.Key]);
                }
            }
            // Set the payload content/location
            if (_serverProfile["Headers"].Count != 0)
            {
                // Add any Get profile specific response headers
                foreach (var header in _serverProfile["Headers"])
                {
                    httpContext.Response.Headers.TryAdd(header.Key, header.Value);
                }
            }

            return resultContent;
        }

        public string GetSeralizedClientProfile()
        {
            var jsonResult = JsonConvert.SerializeObject(ClientProfile);
            return jsonResult;
        }
    }
}
