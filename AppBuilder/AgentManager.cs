using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// AgentManager encapsulates all logic for agent and store initialization, configuration,
/// and provides interfaces for agent interaction. It loads configuration from paths provided
/// in the constructor and is the single point of ownership for DevGPT agents and stores,
/// so the rest of the application can treat it entirely as a black box for agent management.
/// </summary>
public class AgentManager
{
    private List<IDocumentStore> _stores;
    private List<DevGPTAgent> _agents;
    private readonly string _storesJsonPath;
    private readonly string _agentsJsonPath;
    private readonly QuickAgentCreator _quickAgentCreator;

    // New: AgentManager owns the interaction history with agents
    public List<DevGPTChatMessage> History { get; } = new List<DevGPTChatMessage>();

    /// <summary>
    /// All available stores, as loaded and constructed from stores.json
    /// </summary>
    public IReadOnlyList<IDocumentStore> Stores => _stores;
    /// <summary>
    /// All available agents, as loaded and constructed from agents.json
    /// </summary>
    public IReadOnlyList<DevGPTAgent> Agents => _agents;

    /// <summary>
    /// Instantiates the AgentManager, loads configuration, and initializes all stores and agents.
    /// </summary>
    /// <param name="storesJsonPath">Absolute or relative path to stores.json</param>
    /// <param name="agentsJsonPath">Absolute or relative path to agents.json</param>
    /// <param name="codeBuilder">CodeBuilder2 instance providing output handling</param>
    /// <param name="openAIApiKey">OpenAI API Key for agent operation</param>
    /// <param name="logFilePath">Optional log file path for agent operation</param>
    public AgentManager(string storesJsonPath, string agentsJsonPath, string openAIApiKey, string logFilePath)
    {
        _storesJsonPath = storesJsonPath ?? throw new ArgumentNullException(nameof(storesJsonPath));
        _agentsJsonPath = agentsJsonPath ?? throw new ArgumentNullException(nameof(agentsJsonPath));

        var openAIConfig = new OpenAIConfig(openAIApiKey);
        var llmClient = new OpenAIClientWrapper(openAIConfig);
        var agentFactory = new AgentFactory(openAIApiKey, logFilePath);
        agentFactory.Messages = History; // History now lives in AgentManager
        _quickAgentCreator = new QuickAgentCreator(agentFactory, llmClient);

        LoadStoresAndAgents();
    }

    /// <summary>
    /// Loads and initializes all document stores and agents from provided configuration files.
    /// </summary>
    private void LoadStoresAndAgents()
    {
        if (!File.Exists(_storesJsonPath))
            throw new FileNotFoundException("Could not find stores configuration.", _storesJsonPath);
        if (!File.Exists(_agentsJsonPath))
            throw new FileNotFoundException("Could not find agents configuration.", _agentsJsonPath);
        string storesjson = File.ReadAllText(_storesJsonPath);
        string agentsjson = File.ReadAllText(_agentsJsonPath);
        var storesConfig = JsonSerializer.Deserialize<List<StoreConfig>>(storesjson) ?? new List<StoreConfig>();
        var agentsConfig = JsonSerializer.Deserialize<List<AgentConfig>>(agentsjson) ?? new List<AgentConfig>();

        // Create all document stores
        _stores = storesConfig
            .Select(sc => _quickAgentCreator.CreateStore(new StorePaths(sc.Path), sc.Name) as IDocumentStore)
            .ToList();

        // Create all agents and set up their communication/roles
        _agents = new List<DevGPTAgent>();
        foreach (var ac in agentsConfig)
        {
            var agent = _quickAgentCreator.Create(
                ac.Name,
                $"Jouw naam: {ac.Name}\nJouw Rol: {ac.Description}\nInstructie: {ac.Prompt}",
                ac.Stores.Select(acs => (_stores.First(s => s.Name == acs.Name), true)).ToList(),
                ac.Functions,
                ac.CallsAgents,
                ac.ExplicitModify
            ).Result;
            _agents.Add(agent);
        }
    }

    /// <summary>
    /// Get an agent by name.
    /// </summary>
    /// <param name="name">Name of the agent.</param>
    /// <returns>DevGPTAgent with specified name or null.</returns>
    public DevGPTAgent GetAgent(string name)
    {
        return _agents.FirstOrDefault(a => a.Name == name);
    }

    /// <summary>
    /// Interactively communicates with a specified agent through the console,
    /// using the agent's context and configuration.
    /// </summary>
    /// <param name="agentName">The agent's name to interact with</param>
    public async Task InteractiveUserLoop(string agentName = null)
    {
        DevGPTAgent agent;
        if (string.IsNullOrEmpty(agentName))
        {
            // If agent name not specified, select first agent.
            agent = _agents.FirstOrDefault();
            if (agent == null) throw new InvalidOperationException("No agents loaded.");
        }
        else
        {
            agent = GetAgent(agentName);
            if (agent == null) throw new InvalidOperationException($"Agent not found: {agentName}");
        }
        while (true)
        {
            Console.WriteLine("Geef een instructie (of 'exit' om te stoppen):");
            var input = Console.ReadLine();
            if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
                break;
            var response = await agent.Generator.GetResponse<IsReadyResult>(input, History, true, true, agent.Tools, null);
            while (!response.IsRequestImplemented)
            {
                response = await agent.Generator.GetResponse<IsReadyResult>("Continue implementing the requested features", History, true, true, agent.Tools, null);
                Console.WriteLine(response.Message);
            }
            History.Add(new DevGPTChatMessage { Role = DevGPTMessageRole.Assistant, Text = response.Message });
        }
    }
}
