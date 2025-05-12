public class QuickAgentCreator
{
    public QuickAgentCreator(AgentFactory f, ILLMClient client)
    {
        AgentFactory = f;
        Client = client;
    }

    public AgentFactory AgentFactory { get; set; }
    public ILLMClient Client{ get; set; }

    /// <summary>
    /// Centralized agent creation method.
    /// </summary>
    public async Task<DevGPTAgent> Create(
        string name,
        string systemPrompt,
        IEnumerable<(IDocumentStore Store, bool Write)> stores,
        IEnumerable<string> functions = null,
        IEnumerable<string> agents = null, bool isCoder = false)
    {
        if (agents == null) agents = [];
        if (functions == null) functions = [];
        return await AgentFactory.CreateAgent(name, systemPrompt, stores, functions, agents, isCoder);
    }

    /// <summary>
    /// Creates a document store for code and agent memory.
    /// </summary>
    public DocumentStore CreateStore(StorePaths paths, string name)
    {
        var embeddingStore = new EmbeddingFileStore(paths.EmbeddingsFile, Client);
        var textStore = new TextFileStore(paths.RootFolder);
        var partStore = new DocumentPartFileStore(paths.PartsFile);
        var store = new DocumentStore(embeddingStore, textStore, partStore, Client);
        store.UpdateEmbeddings();
        store.Name = name;
        return store;
    }
}


