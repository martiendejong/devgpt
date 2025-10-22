public class PgVectorTextEmbeddingStore : AbstractTextEmbeddingStore, ITextEmbeddingStore
{
    private readonly string _connectionString;
    private readonly List<EmbeddingInfo> _embeddings = new();

    public PgVectorTextEmbeddingStore(string connectionString, ILLMClient embeddingProvider) : base(embeddingProvider)
    {
        _connectionString = connectionString;
    }

    public override EmbeddingInfo[] Embeddings => _embeddings.ToArray();

    public override async Task<EmbeddingInfo?> GetEmbedding(string key)
    {
        // Placeholder; implement PG query by key using pgvector
        return _embeddings.FirstOrDefault(e => e.Key == key);
    }

    public override async Task<bool> RemoveEmbedding(string key)
    {
        // Placeholder; implement PG delete
        var e = _embeddings.FirstOrDefault(x => x.Key == key);
        if (e == null) return false;
        _embeddings.Remove(e);
        return true;
    }

    protected override async Task UpdateEmbedding(EmbeddingInfo embedding)
    {
        // Placeholder; implement PG upsert
        var idx = _embeddings.FindIndex(e => e.Key == embedding.Key);
        if (idx >= 0) _embeddings[idx] = embedding; else _embeddings.Add(embedding);
    }

    protected override async Task AddEmbedding(EmbeddingInfo embeddingInfo)
    {
        // Placeholder; implement PG insert
        _embeddings.Add(embeddingInfo);
    }
}

