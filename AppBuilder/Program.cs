using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // Minimal configuration paths
        const string LogFilePath = @"C:\Projects\devgpt\log";
        const string StoresJsonPath = "stores.json";
        const string AgentsJsonPath = "agents.json";

        var openAISettings = OpenAIConfig.Load();
        string openAIApiKey = openAISettings.ApiKey;

        // Instantiate AgentManager with paths (from new DevGPT.AgentFactory project)
        var agentManager = new AgentManager(
            StoresJsonPath,
            AgentsJsonPath,
            openAIApiKey,
            LogFilePath
        );
        await agentManager.LoadStoresAndAgents();

        // Start the interactive agent user loop. The AgentManager now manages all agent history.
        await agentManager.InteractiveUserLoop();
    }
}
