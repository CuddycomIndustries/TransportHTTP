using httpserver.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace httpserver
{
    public class HttpProfile : IHttpProfile
    {
        private IConfiguration _configuration { get; set; }
        private string _httpServerEndpoint { get; set; }
        public HttpProfile(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpServerEndpoint = _configuration.GetValue<string>("Server:HttpServerEndpoint");
        }

        public Profile ClientProfile => 
            new Profile
            {

                // Client Http Get profile, used when the client has no data to post (i.e. standard checkin)
                HttpGet = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            // This section defines basics information on how to talk to this Http Server
                            "Server",
                            new Dictionary<string, string>
                            {
                                {"IgnoreSSL", "true"},
                                {"Host", _httpServerEndpoint },
                                {"URLs", "/faction.html" }
                            }
                        },
                        {
                            // Set static Headers the client should send for HttpGet requests
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
                                {"Host", _httpServerEndpoint },
                                {"URLs", "/weather.html" }
                            }
                        },
                        {
                            // Set static Headers the client should send for HttpPosts requests
                            "Headers",
                            new Dictionary<string, string>
                            {
                                {"Referer", "http://faction.com"},
                                {"User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko"}

                            }
                        },
                        {
                            // Set static cookies sent with every POST operation
                            "Cookies",
                            new Dictionary<string, string>
                            {
                                {"id", "a3fWa"}
                            }
                        },
                        {
                            // Define where (Body, Header, or Cookies) we will stuff payload data and which
                            // key the Server should look for the content in
                            "ClientPayload",
                            new Dictionary<string, string>
                            {
                                { "Message", "Body::city" },
                                { "AgentName", "Header::x-token" },
                                { "StageName", "Cookie::sessionId" }
                            }
                        },
                        {
                            // The Client needs reference to where the Server is stuffing payload data
                            // (Headers or Body) so the client can find and decode the 
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
                        // Define which URL's are "valid" for payloads and the content of the response
                        // For any payload data defined in Body the content must include a place to interporlate the data
                        // based on the configruation defined Payload section below.
                        "URLs",
                        new Dictionary<string, string>
                        {
                            { "/faction.html", "<h1>Hello World!</h1><span id = 'notpayload'>Something else entierly</span> <span id ='MESSAGE'>%%MESSAGE%%</span> <span>Not the payload</span>"},
                            { "/socks.js",  "<h1></h1><script>var myHeading = document.querySelector('h1');myHeading.textContent = 'Hello world!'; var MESSAGE='%%MESSAGE%%';</script> <script>var test = 'nothing';</script>"}
                        }
                    },
                    {
                        // Define static headers for every Server response
                        "Headers",
                        new Dictionary<string, string>
                        {
                            { "ServerName", "FC2 1.0" },
                            { "X-PoweredBy", "Faction" }
                        }
                    },
                    {
                        // Define where (Body or Header) we will stuff payload data and
                        // where we should interperlate data in the content defined in URL's above.
                        // For Body defined content the interpolation key must match
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
                        // Define which URL's are "valid" for receiving POST data and what the static response content should be
                        "URLs",
                        new Dictionary<string, string>
                        {
                            {"/weather.html", "<h1>It's snowing!</h1>"}
                        }
                    },
                    {
                        // Define static response Headers for HttpPost requests
                        "Headers",
                        new Dictionary<string, string>
                        {
                            { "ServerName", "FC2 1.0" },
                            { "X-PoweredBy", "Faction" }
                        }
                    },
                    {
                        // Define where (Header or Cookie) we will stuff payload data and
                        // what Key the client should expect to find the payload content
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
