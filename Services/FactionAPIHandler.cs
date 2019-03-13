using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace httpserver.Services
{
    // Handler for communication with the Faction API
    public class FactionAPIHandler : IFactionAPIHandler
    {
        private IConfiguration _configuration { get; set; }
        private int _id { get; set; }
        private string _keyName { get; set; }
        private string _secret { get; set; }
        private string _factionEndpoint { get; set; }

        // static GUID for Faction - this should never change for this Transport
        private string _serviceGuid = "2daece20-0d27-4068-b265-ceff27d3f3b2";


        public FactionAPIHandler(IConfiguration configuration)
        {
            _configuration = configuration;
            _id = _configuration.GetValue<int>("Server:FactionId");
            _keyName = _configuration.GetValue<string>("Server:FactionAPIKey");
            _secret = _configuration.GetValue<string>("Server:FactionAPISecret");
            _factionEndpoint = _configuration.GetValue<string>("Server:FactionAPIEndpoint");
        }

        //  Register this Transport service with the Faction service on startup
        public string RegisterServerWithFactionAPI(string clientConfiguration)
        {
            string transportUrl = $"{_factionEndpoint}/api/v1/transport/{_id}/";
            
            // Disable Cert Check
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            WebClient wc = new WebClient();
            wc.Headers[HttpRequestHeader.ContentType] = "application/json";
            string authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_keyName}:{_secret}"));
            wc.Headers[HttpRequestHeader.Authorization] = $"Basic {authHeader}";

            // Convert the config to base64
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(clientConfiguration);
            var _b64Config = System.Convert.ToBase64String(plainTextBytes);

            string _jsonConfig = $"[{{\"Name\":\"CONFIG\", \"Value\":\"{_b64Config}\"}}]";

            Dictionary<string, string> putParams = new Dictionary<string, string>()
            {
                { "Name", "HttpTransport" },
                { "Description", "Http Transport" },
                { "Guid", _serviceGuid},
                { "Configuration", _jsonConfig}
            };

            string jsonMessage = JsonConvert.SerializeObject(putParams);

            Dictionary<string, string> responseDict = new Dictionary<string, string>();
            try
            {
                string response = wc.UploadString(transportUrl, "PUT", jsonMessage);
                responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);

                return responseDict["Success"];
            }
            catch(Exception e)
            {
                Console.WriteLine($"[Marauder DIRECT Transport] Got ERROR: {e.Message}");
                return "False";
            }
        }

        public Dictionary<string, string> HandleStage(string StageName, string StagingId, string Message)
        {

            string stagingUrl = $"{_factionEndpoint}/api/v1/staging/{StageName}/{StagingId}/";
            // Disable Cert Check
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            WebClient wc = new WebClient();
            wc.Headers[HttpRequestHeader.ContentType] = "application/json";
            string authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_keyName}:{_secret}"));
            wc.Headers[HttpRequestHeader.Authorization] = $"Basic {authHeader}";

            Dictionary<string, string> responseDict = new Dictionary<string, string>();
            string jsonMessage = $"{{\"Message\": \"{Message}\"}}";
            try
            {
                Console.WriteLine($"[Marauder DIRECT Transport] Staging URL: {stagingUrl}");
                Console.WriteLine($"[Marauder DIRECT Transport] Key Name: {_keyName}");
                Console.WriteLine($"[Marauder DIRECT Transport] Secret: {_secret}");
                Console.WriteLine($"[Marauder DIRECT Transport] Sending Staging Message: {jsonMessage}");
                string response = wc.UploadString(stagingUrl, jsonMessage);
                Console.WriteLine($"[Marauder DIRECT Transport] Got Response: {response}");
                responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);

            }
            catch
            {
                responseDict["Message"] = "ERROR";
            }
            return responseDict;
        }

        public Dictionary<string, string> HandleBeacon(string AgentName, string Message)
        {
            // Disable cert check
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            string beaconUrl = $"{_factionEndpoint}/api/v1/agent/{AgentName}/checkin/";

            WebClient wc = new WebClient();
            wc.Headers[HttpRequestHeader.ContentType] = "application/json";
            string authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_keyName}:{_secret}"));
            wc.Headers[HttpRequestHeader.Authorization] = $"Basic {authHeader}";

            Dictionary<string, string> responseDict = new Dictionary<string, string>();

            if (!String.IsNullOrEmpty(Message))
            {
                try
                {
                    string jsonMessage = $"{{\"Message\": \"{Message}\"}}";
                    Console.WriteLine($"[Marauder DIRECT Transport] Beacon URL: {beaconUrl}");
                    Console.WriteLine($"[Marauder DIRECT Transport] Key Name: {_keyName}");
                    Console.WriteLine($"[Marauder DIRECT Transport] Secret: {_secret}");
                    Console.WriteLine($"[Marauder DIRECT Transport] POSTING Checkin: {jsonMessage}");
                    string response = wc.UploadString(beaconUrl, jsonMessage);
                    responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[Marauder DIRECT Transport] Got ERROR: {e.Message}");
                    responseDict["Message"] = "ERROR";
                }
            }
            else
            {
                try
                {
                    Console.WriteLine($"[Marauder DIRECT Transport] GETTING Checkin..");
                    string response = wc.DownloadString(beaconUrl);
                    responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[Marauder DIRECT Transport] Got ERROR: {e.Message}");
                    responseDict["Message"] = "ERROR";
                }
            }
            return responseDict;
        }
    }
}
