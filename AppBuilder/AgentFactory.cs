// See https://aka.ms/new-console-template for more information
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using static System.Formats.Asn1.AsnWriter;
using System.Collections.Generic;

public class AgentFactory {
    public AgentFactory(string openAIApiKey, string logFilePath)
    {
        OpenAiApiKey = openAIApiKey;
        LogFilePath = logFilePath;
    }

    public bool WriteMode = false;
    public List<DevGPTChatMessage> Messages; // Set on init by AgentManager.
    public string OpenAiApiKey;
    public string LogFilePath;

    public ChatToolParameter keyParameter = new ChatToolParameter { Name = "key", Description = "The key/path of the file.", Type = "string", Required = true };
    public ChatToolParameter contentParameter = new ChatToolParameter { Name = "content", Description = "The content of the file.", Type = "string", Required = true };
    public ChatToolParameter relevancyParameter = new ChatToolParameter { Name = "query", Description = "The relevancy search query.", Type = "string", Required = true };
    public ChatToolParameter instructionParameter = new ChatToolParameter { Name = "instruction", Description = "The instruction to send to the agent.", Type = "string", Required = true };
    public ChatToolParameter argumentsParameter = new ChatToolParameter { Name = "arguments", Description = "The arguments to call git with.", Type = "string", Required = true };

    public Dictionary<string, DevGPTAgent> Agents = new Dictionary<string, DevGPTAgent>();

    public async Task<string> CallAgent(string name, string query, string caller)
    {
        var agent = Agents[name];
        Messages.Add(new DevGPTChatMessage { Role = DevGPTMessageRole.Assistant, Text = $"{caller}: {query}" });
        var response = await agent.Generator.GetResponse(query + (WriteMode ? writeModeText : ""), null, true, true, agent.Tools, null);
        Messages.Add(new DevGPTChatMessage { Role = DevGPTMessageRole.Assistant, Text = $"{name}: {response}" });
        return response;
    }

    const string writeModeText = "\nYou are now in write mode. You cannot call any other {agent}_write tools or write file tools in this mode. The file modifications need to be included in your response.";

    public async Task<string> CallCoderAgent(string name, string query, string caller)
    {
        var agent = Agents[name];
        Messages.Add(new DevGPTChatMessage { Role = DevGPTMessageRole.Assistant, Text = $"{caller}: {query}" });
        var response = await agent.Generator.UpdateStore(query + writeModeText + "\nALL YOUR MODIFICATIONS MUST ALWAYS SUPPLY THE WHOLE FILE. NEVER leave antyhing out and NEVER replace it with something like /* the rest of the code goes here */ or /* the rest of the code stays the same */", null, true, true, agent.Tools, null);
        Messages.Add(new DevGPTChatMessage { Role = DevGPTMessageRole.Assistant, Text = $"{name}: {response}" });
        return response;
    }

    public async Task<DevGPTAgent> CreateAgent(string name, string systemPrompt, IEnumerable<(IDocumentStore Store, bool Write)> stores, IEnumerable<string> function, IEnumerable<string> agents, bool isCoder = false)
    {
        var config = new OpenAIConfig(OpenAiApiKey);
        var llmClient = new OpenAIClientWrapper(config);
        var tools = new ToolsContextBase();
        AddStoreTools(stores, tools, function, agents, name);
        var tempStores = stores.Skip(1).Select(s => s.Store as IDocumentStore).ToList();
        var generator = new DocumentGenerator(stores.First().Store, new List<DevGPTChatMessage>() { new DevGPTChatMessage { Role = DevGPTMessageRole.System, Text = systemPrompt } }, llmClient, OpenAiApiKey, LogFilePath, tempStores);
        var agent = new DevGPTAgent(name, generator, tools, isCoder);
        Agents[name] = agent;
        return agent;
    }

    // ---| Added implementation here |---
    // Implements AddStoreTools for the agent creation process.
    private void AddStoreTools(IEnumerable<(IDocumentStore Store, bool Write)> stores, ToolsContextBase tools, IEnumerable<string> functions, IEnumerable<string> agents, string agentName)
    {
        // This is a stub implementation - add your custom logic for tools based on stores, functions, or agents
        // Optionally add standard tools to tools.Tools
    }
}
