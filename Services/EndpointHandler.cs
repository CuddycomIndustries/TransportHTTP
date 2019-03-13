using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace httpserver.Services
{
    public class EndpointHandler : IEndpointHandler
    {

        private IConfiguration _configuration { get; set; }
        private IFactionAPIHandler _factionAPIHandler { get; set; }
        private IHttpProfile _httpProfile { get; set; }

        public EndpointHandler(IConfiguration configuration, IFactionAPIHandler factionAPIHandler, IHttpProfile httpProfile)
        {
            _configuration = configuration;
            _factionAPIHandler = factionAPIHandler;
            _httpProfile = httpProfile;
        }

        public string GetClientConfigurationProfile()
        {
            return _httpProfile.GetSeralizedClientProfile();
        }

        public List<string> GetProfileRoutes()
        {
            List<string> _routes = new List<string>();
            if (_httpProfile.ServerProfile.HttpGet["URLs"].Count != 0)
                _routes.AddRange(_httpProfile.ServerProfile.HttpGet["URLs"].Keys);

            if (_httpProfile.ServerProfile.HttpPost["URLs"].Count != 0)
                _routes.AddRange(_httpProfile.ServerProfile.HttpPost["URLs"].Keys);

            return _routes;
        }

        public Dictionary<string, string> GetFactionMessageFromClientRequest(HttpRequest httpRequest, string Profile)
        {
            Dictionary<string, string> _body = new Dictionary<string, string>();

            if (httpRequest.Body != null)
            {
                string _bodyContent = "";
                using (Stream receiveStream = httpRequest.Body)
                {
                    using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                    {
                        _bodyContent = readStream.ReadToEnd();
                    }
                }
                _body = JsonConvert.DeserializeObject<Dictionary<string, string>>(_bodyContent);
            }


            Dictionary<string, string> _factionMessage = new Dictionary<string, string>();
            Dictionary<string, string> _config = new Dictionary<string, string>();

            if (Profile == "HttpGet")
                _config = _httpProfile.ClientProfile.HttpGet["ClientPayload"];
            if (Profile == "HttpPost")
                _config = _httpProfile.ClientProfile.HttpPost["ClientPayload"];

            foreach (var property in _config)
            {
                if (property.Value.Split("::")[0] == "Header")
                {
                    string _value = httpRequest.Headers
                        .Where(h => h.Key == property.Value.Split("::")[1])
                        .FirstOrDefault()
                        .Value;

                    _factionMessage.TryAdd(property.Key, _value);
                }
                if (property.Value.Split("::")[0] == "Cookie")
                {
                    string _value = httpRequest.Cookies
                        .Where(h => h.Key == property.Value.Split("::")[1])
                        .FirstOrDefault()
                        .Value;

                    _factionMessage.TryAdd(property.Key, _value);
                }
                if (property.Value.Split("::")[0] == "Body")
                {
                    string _value = _body
                        .Where(h => h.Key == property.Value.Split("::")[1])
                        .FirstOrDefault()
                        .Value;

                    _factionMessage.TryAdd(property.Key, _value);
                }

            }

            return _factionMessage;
        }

        public string Handle(HttpContext httpContext)
        {
            var route = "/" + httpContext.GetRouteValue("route").ToString();
            var validRoutes = GetProfileRoutes();

            // If the route is not incldued in the list of valid routes, return a default page
            if (! validRoutes.Contains(route))
            {
                return System.IO.File.ReadAllText(".\\wwwroot\\default.html");
            }

            string method = httpContext.Request.Method;
            string resultContent = null;

            if (method == "GET")
            {
                Dictionary<string, string> _clientMessage = GetFactionMessageFromClientRequest(httpContext.Request, "HttpGet");
                Dictionary<string, string> _factionMessage = new Dictionary<string, string>();

                if (_clientMessage.Keys.Contains("StageName") && !string.IsNullOrEmpty(_clientMessage["StageName"]))
                {
                    _factionMessage = _factionAPIHandler.HandleStage(_clientMessage["StageName"], _clientMessage["AgentName"], _clientMessage["Message"]);
                }
                else
                {
                    _factionMessage = _factionAPIHandler.HandleBeacon(_clientMessage["AgentName"], _clientMessage["Message"]);
                }

                resultContent = _httpProfile.HandleGet(httpContext, _factionMessage);
            }

            if (method == "POST")
            {
                Dictionary<string, string> _clientMessage = GetFactionMessageFromClientRequest(httpContext.Request, "HttpPost");
                Dictionary<string, string> _factionMessage = new Dictionary<string, string>();

                if (_clientMessage.Keys.Contains("StageName") && !string.IsNullOrEmpty(_clientMessage["StageName"]))
                {
                    _factionMessage = _factionAPIHandler.HandleStage(_clientMessage["StageName"], _clientMessage["AgentName"], _clientMessage["Message"]);
                }
                else
                {
                    _factionMessage = _factionAPIHandler.HandleBeacon(_clientMessage["AgentName"], _clientMessage["Message"]);
                }

                resultContent = _httpProfile.HandlePost(httpContext, _factionMessage);
            }

            return resultContent;
        }
    }
}
