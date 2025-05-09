// See https://aka.ms/new-console-template for more information
using System.Text.Json;
using System.Xml.Linq;
using DevGPT.NewAPI;
using Microsoft.Extensions.Configuration;
using Store.OpnieuwOpnieuw;
using Store.OpnieuwOpnieuw.AIClient;
using Store.OpnieuwOpnieuw.DocumentStore;
using static System.Formats.Asn1.AsnWriter;
//string appDir = @"C:\Projects\martiendejongnl\extract";
//string documentStoreRoot = @"C:\Projects\martiendejongnl\extract";
//string embeddingsFile = @"C:\Projects\martiendejongnl\extract\embeddings";
//string partsFile = @"C:\Projects\martiendejongnl\extract\parts";
//string logFilePath = @"C:\Projects\martiendejongnl\extract\log";

//string tempStoreRoot = @"C:\Projects\martiendejongnl\extract\tempstore";
//string tempEmbeddingsFile = @"C:\Projects\martiendejongnl\extract\embeddings";

//string tempPartsFile = @"C:\Projects\martiendejongnl\extract\tempstore\parts";

public class AgentFactory {
    public AgentFactory(string openAIApiKey, string logFilePath)
    {
        OpenAiApiKey = openAIApiKey;
        LogFilePath = logFilePath;
    }

    public string OpenAiApiKey;
    public string LogFilePath;

    public ChatToolParameter keyParameter = new ChatToolParameter { Name = "key", Description = "The key/path of the file.", Type = "string", Required = true };
    public ChatToolParameter contentParameter = new ChatToolParameter { Name = "content", Description = "The content of the file.", Type = "string", Required = true };
    public ChatToolParameter relevancyParameter = new ChatToolParameter { Name = "query", Description = "The relevancy search query.", Type = "string", Required = true };
    public ChatToolParameter instructionParameter = new ChatToolParameter { Name = "instruction", Description = "The instruction to send to the agent.", Type = "string", Required = true };
    public ChatToolParameter argumentsParameter = new ChatToolParameter { Name = "arguments", Description = "The arguments to call git with.", Type = "string", Required = true };

    public Dictionary<string, DevGPTAgent> Agents = new Dictionary<string, DevGPTAgent>();

    public async Task<string> CallAgent(string name, string query)
    {
        return await Agents[name].Generator.UpdateStore(query);
    }

    public async Task<DevGPTAgent> CreateAgent(string name, string systemPrompt, IEnumerable<(DocumentStore Store, bool Write)> stores, IEnumerable<string> function, IEnumerable<string> agents)
    {
        var config = new OpenAIConfig(OpenAiApiKey);
        var llmClient = new OpenAIClientWrapper(config);

        var tools = new ToolsContextBase();

        AddStoreTools(stores, tools, function, agents);

        var tempStores = stores.Select(s => s.Store as IDocumentStore).ToList();

        var generator = new DocumentGenerator(stores.First().Store, new List<DevGPTChatMessage>() { new DevGPTChatMessage { Role = DevGPTMessageRole.System, Text = systemPrompt } }, llmClient, OpenAiApiKey, LogFilePath, tempStores);
        var agent = new DevGPTAgent(name, generator, tools);
        Agents[name] = agent;
        return agent;
    }

    private void AddStoreTools(IEnumerable<(DocumentStore Store, bool Write)> stores, ToolsContextBase tools, IEnumerable<string> functions, IEnumerable<string> agents)
    {
        AddAgentTools(agents);
        var i = 0;
        foreach (var storeItem in stores)
        {
            var store = storeItem.Store;
            AddReadTools(tools, store);
            if (storeItem.Write)
            {
                AddWriteTools(tools, store);
                if (i == 0)
                {
                    AddBuildTools(tools, functions, store);
                }
            }
            ++i;
        }
    }

    private void AddWriteTools(ToolsContextBase tools, DocumentStore store)
    {
        var writeFile = new DevGPTChatTool($"{store.Name}_write", $"Store a file in store {store.Name}", [keyParameter, contentParameter], async (messages, toolCall) =>
        {
            if (keyParameter.TryGetValue(toolCall, out JsonElement key))
                if (contentParameter.TryGetValue(toolCall, out JsonElement content))
                    return await store.Store(key.ToString(), content.ToString(), false) ? "success" : "content provided was the same as the file";
                else
                    return "No content given";
            return "No key given";
        });
        tools.Add(writeFile);
        var deleteFile = new DevGPTChatTool($"{store.Name}_delete", $"Removes a file from store {store.Name}", [keyParameter], async (messages, toolCall) =>
        {
            if (keyParameter.TryGetValue(toolCall, out JsonElement key))
                return await store.Remove(key.ToString()) ? "success" : "the file was already deleted"; ;
            return "No key given";
        });
        tools.Add(deleteFile);
    }

    private void AddBuildTools(ToolsContextBase tools, IEnumerable<string> functions, DocumentStore store)
    {
        if (functions.Contains("git"))
        {
            var git = new DevGPTChatTool($"git", $"Calls git and returns the output.", [argumentsParameter], async (messages, toolCall) =>
            {
                using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                if (argumentsJson.RootElement.TryGetProperty("arguments", out JsonElement args))
                {
                    var output = GitOutput.GetGitOutput(store.TextStore.RootFolder, args.ToString());
                    return output.Item1 + "\n" + output.Item2;
                }
                return "arguments not provided";
            });
            tools.Add(git);
        }
        if (functions.Contains("build"))
        {
            var build = new DevGPTChatTool($"build", $"Builds the solution and returns the output.", [], async (messages, toolCall) => BuildOutput.GetBuildOutput(store.TextStore.RootFolder, "build.bat", "build_errors.log"));
            tools.Add(build);
        }
    }

    private void AddReadTools(ToolsContextBase tools, DocumentStore store)
    {
        var getFiles = new DevGPTChatTool($"{store.Name}_list", $"Retrieve a list of the files in store {store.Name}", [], async (messages, toolCall) => string.Join("\n", await store.List()));
        tools.Add(getFiles);
        var getRelevancy = new DevGPTChatTool($"{store.Name}_relevancy", $"Retrieve a list of relevant files in store {store.Name}", [relevancyParameter], async (messages, toolCall) =>
        {
            if (relevancyParameter.TryGetValue(toolCall, out JsonElement key))
                return string.Join("\n", await store.RelevantItems(key.ToString()));
            return "No key given";
        });
        tools.Add(getRelevancy);
        var getFile = new DevGPTChatTool($"{store.Name}_read", $"Retrieve a file from store {store.Name}", [keyParameter], async (messages, toolCall) =>
        {
            if (keyParameter.TryGetValue(toolCall, out JsonElement key))
                return await store.Get(key.ToString());
            return "No key given";
        });
        tools.Add(getFile);
    }

    private void AddAgentTools(IEnumerable<string> agents)
    {
        foreach (var agent in agents)
        {
            var getRelevancy = new DevGPTChatTool($"{agent}", $"Calls {agent} to execute a taks and return a message", [instructionParameter], async (messages, toolCall) =>
            {
                if (instructionParameter.TryGetValue(toolCall, out JsonElement key))
                    return await CallAgent(agent, key.ToString());
                return "No key given";
            });
        }
    }
}


