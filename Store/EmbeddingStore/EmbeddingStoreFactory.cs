public static class EmbeddingStoreFactory
{
    private static readonly Dictionary<string, Func<string, ILLMClient, ITextEmbeddingStore>> _registry
        = new(StringComparer.InvariantCultureIgnoreCase)
        {
            { "file", (spec, provider) => new EmbeddingFileStore(spec, provider) },
            { "memory", (spec, provider) => new TextEmbeddingMemoryStore(provider) },
            { "sqlite", (spec, provider) => new SqliteTextEmbeddingStore(spec, provider) },
            { "pgvector", (spec, provider) => new PgVectorTextEmbeddingStore(spec, provider) },
            { "faiss", (spec, provider) => new FaissTextEmbeddingStore(spec, provider) },
        };

    public static void Register(string scheme, Func<string, ILLMClient, ITextEmbeddingStore> factory)
    {
        _registry[scheme] = factory;
    }

    public static ITextEmbeddingStore CreateFromSpec(string embeddingsSpec, ILLMClient provider)
    {
        if (string.IsNullOrWhiteSpace(embeddingsSpec))
            throw new ArgumentException("embeddingsSpec must be provided", nameof(embeddingsSpec));

        // Detect explicit scheme prefixes. Defaults to file store for normal paths (e.g., C:\...)
        if (embeddingsSpec.StartsWith("sqlite:", StringComparison.InvariantCultureIgnoreCase))
        {
            var spec = embeddingsSpec.Substring("sqlite:".Length);
            return _registry["sqlite"].Invoke(spec, provider);
        }
        if (embeddingsSpec.StartsWith("pgvector:", StringComparison.InvariantCultureIgnoreCase))
        {
            var spec = embeddingsSpec.Substring("pgvector:".Length);
            return _registry["pgvector"].Invoke(spec, provider);
        }
        if (embeddingsSpec.StartsWith("faiss:", StringComparison.InvariantCultureIgnoreCase))
        {
            var spec = embeddingsSpec.Substring("faiss:".Length);
            return _registry["faiss"].Invoke(spec, provider);
        }

        // Default to file-backed store
        return _registry["file"].Invoke(embeddingsSpec, provider);
    }
}

