using System.Text.Json;

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
    private string _storesJson;
    private string _agentsJson;
    private bool _isContent;
    private readonly string _storesJsonPath;
    private readonly string _agentsJsonPath;
    public readonly QuickAgentCreator _quickAgentCreator;

    // The interaction history with agents
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
    public AgentManager(string storesJsonPath, string agentsJsonPath, string openAIApiKey, string logFilePath, string googleProjectId = "")
    {
        _storesJsonPath = storesJsonPath ?? throw new ArgumentNullException(nameof(storesJsonPath));
        _agentsJsonPath = agentsJsonPath ?? throw new ArgumentNullException(nameof(agentsJsonPath));

        var openAIConfig = new OpenAIConfig(openAIApiKey);
        var llmClient = new OpenAIClientWrapper(openAIConfig);
        var agentFactory = new AgentFactory(openAIApiKey, logFilePath, googleProjectId);
        agentFactory.Messages = History; // History now lives in AgentManager
        _quickAgentCreator = new QuickAgentCreator(agentFactory, llmClient);
    }

    public AgentManager(string storesJson, string agentsJson, string openAIApiKey, string logFilePath, bool isContent, string googleProjectId = "")
    {
        _storesJson = storesJson;
        _agentsJson = agentsJson;
        _isContent = isContent;

        var openAIConfig = new OpenAIConfig(openAIApiKey);
        var llmClient = new OpenAIClientWrapper(openAIConfig);
        var agentFactory = new AgentFactory(openAIApiKey, logFilePath, googleProjectId);
        agentFactory.Messages = History; // History now lives in AgentManager
        _quickAgentCreator = new QuickAgentCreator(agentFactory, llmClient);
    }

    /// <summary>
    /// Loads and initializes all document stores and agents from provided configuration files.
    /// </summary>
    public async Task LoadStoresAndAgents()
    {
        string storesContent;
        string agentsjson;
        if (!_isContent) { 
            if (!File.Exists(_storesJsonPath))
                throw new FileNotFoundException("Could not find stores configuration.", _storesJsonPath);
            if (!File.Exists(_agentsJsonPath))
                throw new FileNotFoundException("Could not find agents configuration.", _agentsJsonPath);
            storesContent = File.ReadAllText(_storesJsonPath);
            agentsjson = File.ReadAllText(_agentsJsonPath);
        }
        else
        {
            storesContent = _storesJson;
            agentsjson = _agentsJson;
        }

        // ---- FORMAT AUTO-DETECTION (support both JSON/.devgpt) ----
        var storesConfig = StoreConfigFormatHelper.AutoDetectAndParse(storesContent) ?? new List<StoreConfig>();
        _quickAgentCreator.AgentFactory.storesConfig = storesConfig;
        _quickAgentCreator.AgentFactory.agentsConfig = JsonSerializer.Deserialize<List<AgentConfig>>(agentsjson) ?? new List<AgentConfig>();
        var agentsConfig = _quickAgentCreator.AgentFactory.agentsConfig;
        // ----------------------------------------------------------

        // Create all document stores
        _stores = new List<IDocumentStore>();
        foreach (var sc in storesConfig)
        {
            var store = _quickAgentCreator.CreateStore(new StorePaths(sc.Path), sc.Name) as IDocumentStore;
            await AddFiles(store, sc.Path, sc.FileFilters, sc.SubDirectory, sc.ExcludePattern);
            _stores.Add(store);
        }

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

    public async Task AddFiles(IDocumentStore store, string path, string[] fileFilters, string subDirectory = "", string[] excludePattern = null)
    {
        var dir = subDirectory == "" ? new DirectoryInfo(path) : new DirectoryInfo(Path.Combine(path, subDirectory));

        var filesParts = new List<FileInfo[]>();
        foreach (var item in fileFilters)
        {
            filesParts.Add(dir.GetFiles(item, SearchOption.AllDirectories));
        }
        var files = filesParts.SelectMany(f => f).ToList();
        files = files.Where(file =>
        {
            // Use Path.GetRelativePath for platform-correct relative file paths
            var relPath = Path.GetRelativePath(path, file.FullName);
            return excludePattern == null || !excludePattern.Any(dir => MatchPattern(relPath, dir));
        }).ToList();

        foreach (var file in files)
        {
            var relPath = Path.GetRelativePath(path, file.FullName);
            if (excludePattern == null || !excludePattern.Any(dir => MatchPattern(relPath, dir)))
            {
                await store.Embed(relPath);
            }
        }
    }

    public bool MatchPattern(string text, string pattern)
    {
        if (pattern.StartsWith("*"))
            return text.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase);
        return text.Contains(pattern, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Get an agent by name.
    /// </summary>
    public DevGPTAgent GetAgent(string name)
    {
        return _agents.FirstOrDefault(a => a.Name == name);
    }

    /// <summary>
    /// Interactively communicates with a specified agent through the console,
    /// using the agent's context and configuration.
    /// </summary>
    public async Task InteractiveUserLoop(string agentName = null)
    {
        DevGPTAgent agent;
        if (string.IsNullOrEmpty(agentName))
        {
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
            while (!response.IsTheUserRequestProperlyHandledAndFinished)
            {
                response = await agent.Generator.GetResponse<IsReadyResult>("Continue handling the user request: " + input, History, true, true, agent.Tools, null);
                Console.WriteLine(response.Message);
            }
            History.Add(new DevGPTChatMessage { Role = DevGPTMessageRole.Assistant, Text = response.Message });
        }
    }

    public async Task<string> SendMessage(string input, string agentName = null)
    {
        DevGPTAgent agent;
        if (string.IsNullOrEmpty(agentName))
        {
            agent = _agents.FirstOrDefault();
            if (agent == null) throw new InvalidOperationException("No agents loaded.");
        }
        else
        {
            agent = GetAgent(agentName);
            if (agent == null) throw new InvalidOperationException($"Agent not found: {agentName}");
        }

        var response = await agent.Generator.GetResponse<IsReadyResult>(input, History, true, true, agent.Tools, null);
        return response.Message;
    }
}
