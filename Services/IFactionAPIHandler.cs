using System.Collections.Generic;

namespace httpserver.Services
{
    public interface IFactionAPIHandler
    {
        Dictionary<string, string> HandleBeacon(string AgentName, string Message);
        Dictionary<string, string> HandleStage(string StageName, string AgentName, string Message);
        string RegisterServerWithFactionAPI(string clientConfiguration);
    }
}