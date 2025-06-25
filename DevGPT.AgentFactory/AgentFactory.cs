using System;
using System.Threading;
using System.Xml.Linq;

using Google.Apis.Auth.OAuth2.Responses;
using Google.Cloud.BigQuery.V2;
using MathNet.Numerics.RootFinding;

public class AgentFactory {
    public AgentFactory(string openAIApiKey, string logFilePath, string googleProjectId = "")
    {
        OpenAiApiKey = openAIApiKey;
        GoogleProjectId = googleProjectId;
        LogFilePath = logFilePath;
    }
    
    public List<StoreConfig> storesConfig;
    public List<AgentConfig> agentsConfig;
    public List<FlowConfig> flowsConfig;

    public bool WriteMode = false;
    public List<DevGPTChatMessage> Messages; // Set on init by AgentManager.
    public string OpenAiApiKey;
    public string GoogleProjectId;
    public string LogFilePath;

    public ChatToolParameter keyParameter = new ChatToolParameter { Name = "key", Description = "The key/path of the file.", Type = "string", Required = true };
    public ChatToolParameter contentParameter = new ChatToolParameter { Name = "content", Description = "The content of the file.", Type = "string", Required = true };
    public ChatToolParameter relevancyParameter = new ChatToolParameter { Name = "query", Description = "The relevancy search query.", Type = "string", Required = true };
    public ChatToolParameter instructionParameter = new ChatToolParameter { Name = "instruction", Description = "The instruction to send to the agent.", Type = "string", Required = true };
    public ChatToolParameter argumentsParameter = new ChatToolParameter { Name = "arguments", Description = "The arguments to call git with.", Type = "string", Required = true };
    public ChatToolParameter bigQueryParameter = new ChatToolParameter { Name = "arguments", Description = "The arguments to call Google BigQuery with.", Type = "string", Required = true };
    public ChatToolParameter bigQueryDataSetParameter = new ChatToolParameter { Name = "datacollection", Description = "The dataset in Google BigQuery.", Type = "string", Required = true };

    public Dictionary<string, DevGPTAgent> Agents = new Dictionary<string, DevGPTAgent>();
    public Dictionary<string, DevGPTFlow> Flows = new Dictionary<string, DevGPTFlow>();

    // Updated: messages are stored with full meta-info and updated in-place with Response upon agent reply
    public async Task<string> CallAgent(string name, string query, string caller)
    {
        var agent = Agents[name];
        var id = Guid.NewGuid().ToString();
        agent.Tools.SendMessage(id, name, query);
        Guid messageId = Guid.NewGuid();
        var message = new DevGPTChatMessage
        {
            MessageId = messageId,
            Role = DevGPTMessageRole.Assistant,
            Text = $"{caller}: {query}",
            AgentName = name,
            FunctionName = string.Empty,
            FlowName = string.Empty,
            Response = string.Empty
        };
        Messages.Add(message);
        string response = await agent.Generator.GetResponse(query + (WriteMode ? writeModeText : ""), null, true, true, agent.Tools, null);
        // Find the message by MessageId and update Response
        var storedMsg = Messages.FirstOrDefault(m => m.MessageId == messageId);
        if(storedMsg != null) storedMsg.Response = response;
        // Log reply as new message entry for history
        var replyMsg = new DevGPTChatMessage
        {
            MessageId = Guid.NewGuid(),
            Role = DevGPTMessageRole.Assistant,
            Text = $"{name}: {response}",
            AgentName = name,
            FunctionName = string.Empty,
            FlowName = string.Empty,
            Response = response
        };
        Messages.Add(replyMsg);
        agent.Tools.SendMessage(id, name, response);
        return response;
    }

    // CallFlow updated to store/track agent/flow meta-data in chat history per message
    public async Task<string> CallFlow(string name, string query, string caller, CancellationToken cancel)
    {
        var flow = Flows[name];
        foreach (var agent in flow.CallsAgents)
        {
            if (Agents[agent].IsCoder && !WriteMode)
            {
                WriteMode = true;
                query = await CallCoderAgent(agent, query, caller, flow.Name, cancel);
                WriteMode = false;
            }
            else
            {
                query = await CallAgentWithMeta(agent, query, caller, string.Empty, flow.Name, cancel);
            }
        }
        return query;
    }

    const string writeModeText = "\nYou are now in write mode. You cannot call any other {agent}_write tools or write file tools in this mode. The file modifications need to be included in your response.";

    // Extended CallCoderAgent to accept flowName and store correct message meta-info
    public async Task<string> CallCoderAgent(string name, string query, string caller, string flowName = "", CancellationToken cancel = default)
    {
        var agent = Agents[name];
        var id = Guid.NewGuid().ToString();
        agent.Tools.SendMessage(id, name, query);
        Guid messageId = Guid.NewGuid();
        var message = new DevGPTChatMessage
        {
            MessageId = messageId,
            Role = DevGPTMessageRole.Assistant,
            Text = $"{caller}: {query}",
            AgentName = name,
            FunctionName = "CodeModify",
            FlowName = flowName,
            Response = string.Empty
        };
        Messages.Add(message);
        string response = await agent.Generator.UpdateStore(query + writeModeText + "\nALL YOUR MODIFICATIONS MUST ALWAYS SUPPLY THE WHOLE FILE. NEVER leave antyhing out and NEVER replace it with something like /* the rest of the code goes here */ or /* the rest of the code stays the same */", null, true, true, agent.Tools, null, cancel);
        var storedMsg = Messages.FirstOrDefault(m => m.MessageId == messageId);
        if(storedMsg != null) storedMsg.Response = response;
        var replyMsg = new DevGPTChatMessage
        {
            MessageId = Guid.NewGuid(),
            Role = DevGPTMessageRole.Assistant,
            Text = $"{name}: {response}",
            AgentName = name,
            FunctionName = "CodeModify",
            FlowName = flowName,
            Response = response
        };
        Messages.Add(replyMsg);
        agent.Tools.SendMessage(id, name, response);
        return response;
    }

    // Helper for agent call with custom meta-info (used in flows)
    public async Task<string> CallAgentWithMeta(string name, string query, string caller, string functionName, string flowName, CancellationToken cancel)
    {
        var agent = Agents[name];
        var id = Guid.NewGuid().ToString();
        agent.Tools.SendMessage(id, name, query);
        Guid messageId = Guid.NewGuid();
        var message = new DevGPTChatMessage
        {
            MessageId = messageId,
            Role = DevGPTMessageRole.Assistant,
            Text = $"{caller}: {query}",
            AgentName = name,
            FunctionName = functionName ?? string.Empty,
            FlowName = flowName ?? string.Empty,
            Response = string.Empty
        };
        Messages.Add(message);
        string response = await agent.Generator.GetResponse(query + (WriteMode ? writeModeText : ""), null, true, true, agent.Tools, null, cancel);
        var storedMsg = Messages.FirstOrDefault(m => m.MessageId == messageId);
        if(storedMsg != null) storedMsg.Response = response;
        var replyMsg = new DevGPTChatMessage
        {
            MessageId = Guid.NewGuid(),
            Role = DevGPTMessageRole.Assistant,
            Text = $"{name}: {response}",
            AgentName = name,
            FunctionName = functionName ?? string.Empty,
            FlowName = flowName ?? string.Empty,
            Response = response
        };
        Messages.Add(replyMsg);
        agent.Tools.SendMessage(id, name, response);
        return response;
    }

    public async Task<DevGPTAgent> CreateAgent(string name, string systemPrompt, IEnumerable<(IDocumentStore Store, bool Write)> stores, IEnumerable<string> function, IEnumerable<string> agents, IEnumerable<string> flows, bool isCoder = false)
    {
        var config = new OpenAIConfig(OpenAiApiKey);
        var llmClient = new OpenAIClientWrapper(config);
        var tools = new ToolsContextBase();
        AddStoreTools(stores, tools, function, agents, flows, name);
        var tempStores = stores.Skip(1).Select(s => s.Store as IDocumentStore).ToList();
        var generator = new DocumentGenerator(stores.First().Store, new List<DevGPTChatMessage>() { new DevGPTChatMessage { Role = DevGPTMessageRole.System, Text = systemPrompt } }, llmClient, OpenAiApiKey, LogFilePath, tempStores);
        var agent = new DevGPTAgent(name, generator, tools, isCoder);
        Agents[name] = agent;
        return agent;
    }

    public DevGPTFlow CreateFlow(string name, List<string> callsAgents)
    {
        var flow = new DevGPTFlow(name, callsAgents);
        Flows[name] = flow;
        return flow;
    }

    private void AddStoreTools(IEnumerable<(IDocumentStore Store, bool Write)> stores, ToolsContextBase tools, IEnumerable<string> functions, IEnumerable<string> agents, IEnumerable<string> flows, string caller)
    {
        AddAgentTools(tools, agents, caller);
        AddFlowTools(tools, flows, caller);
        var i = 0;
        foreach (var storeItem in stores)
        {
            var store = storeItem.Store;
            AddReadTools(tools, store);
            if (storeItem.Write)
            {
                AddWriteTools(tools, store);
            }
            if (i == 0)
            {
                AddBuildTools(tools, functions, store);
            }
            ++i;
        }
    }

    private void AddWriteTools(ToolsContextBase tools, IDocumentStore store)
    {
        var config = storesConfig.First(x => x.Name == store.Name);
        var writeFile = new DevGPTChatTool($"{store.Name}_write", $"Store a file in store {store.Name}. {config.Description}", [keyParameter, contentParameter], async (messages, toolCall, cancel) =>
        {
            if (WriteMode) return "Cannot give write instructions when in write mode";
            if (keyParameter.TryGetValue(toolCall, out string key))
                if (contentParameter.TryGetValue(toolCall, out string content))
                    return await store.Store(key, content, false) ? "success" : "content provided was the same as the file";
                else
                    return "No content given";
            return "No key given";
        });
        tools.Add(writeFile);
        var deleteFile = new DevGPTChatTool($"{store.Name}_delete", $"Removes a file from store {store.Name}. {config.Description}", [keyParameter], async (messages, toolCall, cancel) =>
        {
            if (keyParameter.TryGetValue(toolCall, out string key))
                return await store.Remove(key) ? "success" : "the file was already deleted"; ;
            return "No key given";
        });
        tools.Add(deleteFile);
    }

    private void AddBuildTools(ToolsContextBase tools, IEnumerable<string> functions, IDocumentStore store)
    {
        if (functions.Contains("git"))
        {
            var git = new DevGPTChatTool($"git", $"Calls git and returns the output.", [argumentsParameter], async (messages, toolCall, cancel) =>
            {
                cancel.ThrowIfCancellationRequested();

                if (argumentsParameter.TryGetValue(toolCall, out string args))
                {
                    var output = GitOutput.GetGitOutput(store.TextStore.RootFolder, args);
                    return output.Item1 + "\n" + output.Item2;
                }
                return "arguments not provided";
            });
            tools.Add(git);
        }
        if (functions.Contains("build"))
        {
            var build = new DevGPTChatTool($"build", $"Builds the solution and returns the output.", [], async (messages, toolCall, cancel) => BuildOutput.GetBuildOutput(store.TextStore.RootFolder, "build.bat", "build_errors.log"));
            tools.Add(build);
        }
        if (functions.Contains("build_dotnet"))
        {
            var build = new DevGPTChatTool($"build_dotnet", $"Builds the .NET backend solution and returns the output.", [], async (messages, toolCall, cancel) => BuildOutput.GetBuildOutput(store.TextStore.RootFolder, "build_dotnet.bat", "build_errors.log"));
            tools.Add(build);
        }
        if (functions.Contains("build_quasar"))
        {
            var build = new DevGPTChatTool($"build_quasar", $"Builds the Quasar frontend project and returns the output.", [], async (messages, toolCall, cancel) => BuildOutput.GetBuildOutput(store.TextStore.RootFolder, "build_quasar.bat", "build_errors.log"));
            tools.Add(build);
        }
        if (functions.Contains("test_quasar"))
        {
            var build = new DevGPTChatTool($"test_quasar", $"Tests the Quasar frontend project and returns the output.", [], async (messages, toolCall, cancel) => BuildOutput.GetBuildOutput(store.TextStore.RootFolder, "test_quasar.bat", "build_errors.log"));
            tools.Add(build);
        }
        if (functions.Contains("npm"))
        {
            var npm = new DevGPTChatTool($"npm", $"Runs the npm command.", [argumentsParameter], async (messages, toolCall, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (argumentsParameter.TryGetValue(toolCall, out string args))
                {
                    var output = NpmOutput.GetNpmOutput(store.TextStore.RootFolder + "\\frontend", args);
                    return output.Item1 + "\n" + output.Item2;
                }
                return "arguments not provided";
            });
            tools.Add(npm);
        }
        if (functions.Contains("dotnet"))
        {
            var git = new DevGPTChatTool($"dotnet", $"Runs the dotnet command.", [argumentsParameter], async (messages, toolCall, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (argumentsParameter.TryGetValue(toolCall, out string args))
                {
                    var output = DotNetOutput.GetDotNetOutput(store.TextStore.RootFolder, args);
                    return output.Item1 + "\n" + output.Item2;
                }
                return "arguments not provided";
            });
            tools.Add(git);
        }
        if (functions.Contains("bigquery"))
        {
            var bigQueryCollectionsTool = new DevGPTChatTool(
                "bigquery_datasets",
                "Retrieves the available datasets in Google BigQuery.",
                [],
                async (messages, toolCall, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    try
                    {
                        var sql = "SELECT schema_name FROM `region-eu`.INFORMATION_SCHEMA.SCHEMATA;";
                        BigQueryClient client = BigQuery_GetClient();

                        var result = await client.ExecuteQueryAsync(sql, parameters: null, null, new GetQueryResultsOptions { PageSize = 10000 });

                        return BigQuery_ExtractAsString(result);
                    }
                    catch (Exception ex)
                    {
                        return $"BigQuery error: {ex.Message}";
                    }
                }
            );            
            tools.Add(bigQueryCollectionsTool);
            var bigQueryTablesTool = new DevGPTChatTool(
                "query_tables",
                "Returns the tables in a Google BigQuery dataset.",
                [bigQueryDataSetParameter],
                async (messages, toolCall, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    if (!bigQueryDataSetParameter.TryGetValue(toolCall, out string collection))
                        return "No query provided.";

                    try
                    {
                        BigQueryClient client = BigQuery_GetClient();
                        var sql = $"SELECT table_name FROM `social-media-hulp.{collection}.INFORMATION_SCHEMA.TABLES`;";

                        var result = await client.ExecuteQueryAsync(sql, parameters: null, null, new GetQueryResultsOptions { PageSize = 10000 });

                        return BigQuery_ExtractAsString(result);
                    }
                    catch (Exception ex)
                    {
                        return $"BigQuery error: {ex.Message}";
                    }
                }
            );
            tools.Add(bigQueryTablesTool);
            var bigQueryTool = new DevGPTChatTool(
                "query_bigquery",
                "Runs a read-only SQL query on Google BigQuery and returns the results as a list of rows.",
                [bigQueryParameter],
                async (messages, toolCall, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    if (!bigQueryParameter.TryGetValue(toolCall, out string sql))
                        return "No query provided.";

                    try
                    {
                        BigQueryClient client = BigQuery_GetClient();

                        var result = await client.ExecuteQueryAsync(sql, parameters: null, null, new GetQueryResultsOptions { PageSize = 10000 });

                        return BigQuery_ExtractAsString(result);
                    }
                    catch (Exception ex)
                    {
                        return $"BigQuery error: {ex.Message}";
                    }
                }
            );
            tools.Add(bigQueryTool);
        }
    }

    private static string BigQuery_ExtractAsString(BigQueryResults result)
    {
        var output = new List<string>();
        foreach (var row in result)
        {
            var rowValues = new List<string>();

            foreach (var field in row.Schema.Fields)
            {
                var value = row[field.Name];
                rowValues.Add($"{field.Name}: {value}");
            }

            output.Add(string.Join(", ", rowValues));
        }

        return output.Count > 0
            ? string.Join("\n", output)  // Limit output for GPT
            : "Query executed, but no results found.";
    }

    private static BigQueryClient BigQuery_GetClient()
    {
        var client = new BigQueryClientBuilder
        {
            ProjectId = "social-media-hulp",
            //ProjectId = "wide-lattice-389014",
            JsonCredentials = File.ReadAllText("C:/Projects/devgpt/Windows/googleaccount.json")
        }.Build();
        return client;
    }

    private void AddReadTools(ToolsContextBase tools, IDocumentStore store)
    {
        var config = storesConfig.First(x => x.Name == store.Name);
        var getFiles = new DevGPTChatTool($"{store.Name}_list", $"Retrieve a list of the files in store {store.Name}. {config.Description}", [], async (messages, toolCall, cancel) =>
        {
            cancel.ThrowIfCancellationRequested();
            return string.Join("\n", await store.List());
        });
        tools.Add(getFiles);
        var getRelevancy = new DevGPTChatTool($"{store.Name}_relevancy", $"Retrieve a list of relevant files in store {store.Name}. {config.Description}", [relevancyParameter], async (messages, toolCall, cancel) =>
        {
            cancel.ThrowIfCancellationRequested();
            if (relevancyParameter.TryGetValue(toolCall, out string key))
                return string.Join("\n", await store.RelevantItems(key));
            return "No key given";
        });
        tools.Add(getRelevancy);
        DevGPTChatTool getFile = new DevGPTChatTool($"{store.Name}_read", $"Retrieve a file from store {store.Name}. {config.Description}", [keyParameter], async (messages, toolCall, cancel) =>
        {
            cancel.ThrowIfCancellationRequested();
            if (keyParameter.TryGetValue(toolCall, out string key))
                return await store.Get(key) ?? "File not found";
            return "No key given";
        });
        tools.Add(getFile);
    }

    private void AddAgentTools(ToolsContextBase tools, IEnumerable<string> agents, string caller)
    {
        foreach (var agent in agents)
        {
            var config = agentsConfig.FirstOrDefault(x => x.Name == agent);
            if (config == null)
                throw new Exception($"Agent {agent} is not defined. Request by {caller}.");
            if (config.ExplicitModify)
            {
                var callCoderAgent = new DevGPTChatTool($"{agent}", $"Calls {agent} to modify the codebase. {config.Description} Be aware of the token limit of 8000 so only let the agents make small modifications at a time.", [instructionParameter], async (messages, toolCall, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    if (WriteMode) return "Cannot give write instructions when in write mode";
                    if (instructionParameter.TryGetValue(toolCall, out string key))
                    {
                        WriteMode = true;
                        var result = await CallCoderAgent(agent, key, caller);
                        WriteMode = false;
                        return result;
                    }
                    return "No key given";
                });
                tools.Add(callCoderAgent);
            }
            else
            {
                var callAgent = new DevGPTChatTool($"{agent}", $"Calls {agent} to execute a tasks and return a message. {config.Description}", [instructionParameter], async (messages, toolCall, cancel) =>
                {
                    cancel.ThrowIfCancellationRequested();
                    if (instructionParameter.TryGetValue(toolCall, out string key))
                        return await CallAgent(agent, key, caller);
                    return "No key given";
                });
                tools.Add(callAgent);
            }
        }
    }

    private void AddFlowTools(ToolsContextBase tools, IEnumerable<string> flows, string caller)
    {
        foreach (var flow in flows)
        {
            var config = flowsConfig.FirstOrDefault(x => x.Name == flow);
            if (config == null)
                throw new Exception($"Flow {flow} not found");
            var callFlow = new DevGPTChatTool($"{flow}", $"Calls {flow} agent workflow to execute a tasks and return a message. {config.Description}", [instructionParameter], async (messages, toolCall, cancel) =>
            {
                cancel.ThrowIfCancellationRequested();
                if (instructionParameter.TryGetValue(toolCall, out string key))
                {
                    var id = Guid.NewGuid().ToString();
                    tools.SendMessage(id, flow, key);
                    var response = await CallFlow(flow, key, caller, cancel);
                    tools.SendMessage(id, flow, response);
                    return response;
                }
                return "No key given";
            });
            tools.Add(callFlow);
        }
    }
}
